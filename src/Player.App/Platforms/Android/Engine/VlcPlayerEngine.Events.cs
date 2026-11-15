using Android.OS;
using Android.Views;
using LibVLCSharp.Shared;
using LibVLCSharp.Shared.Structures;
using Player.Core.Contracts;
using Player.Core.Models;

namespace Player.App.Engine;

// LibVLC events, track and format discovery, and the surface binding.
public sealed partial class VlcPlayerEngine
{
    private volatile bool _surfaceAttached;
    private volatile VideoFormat? _format;

    private const int RevealAdvanceMs = 120;
    private static readonly TimeSpan RevealTimeout = TimeSpan.FromSeconds(2);
    private volatile bool _revealingFrame;
    private long _revealFromMs;
    private int _revealId;

    public VideoFormat? Format => _format;
    public event EventHandler<VideoFormat?>? FormatChanged;

    /// <summary>
    /// Called on the UI thread: LibVLC's AWindow only accepts view changes there.
    /// The window size is reported explicitly (docs/SPIKE.md finding 5).
    /// </summary>
    public void SurfaceReady(object nativeSurface, int widthPx, int heightPx)
    {
        EnsureMainThread();
        if (nativeSurface is not global::Android.Views.Surface surface)
            throw new ArgumentException("An Android.Views.Surface is required.", nameof(nativeSurface));

        if (_surfaceAttached)
            _window.DetachViews();
        _window.SetVideoSurface(surface, null);
        _window.AttachViews();
        _window.SetWindowSize(widthPx, heightPx);
        _surfaceAttached = true;
        Trace($"surface ready {widthPx}x{heightPx}");
        _queue.Post(OnSurfaceChanged);
    }

    public void SurfaceSizeChanged(int widthPx, int heightPx)
    {
        EnsureMainThread();
        if (_surfaceAttached)
            _window.SetWindowSize(widthPx, heightPx);
    }

    public void SurfaceLost()
    {
        EnsureMainThread();
        if (!_surfaceAttached)
            return;

        _surfaceAttached = false;
        _window.DetachViews();
        Trace("surface lost");
        _queue.Post(OnSurfaceChanged);
    }

    private static void EnsureMainThread()
    {
        if (Looper.MainLooper is not { IsCurrentThread: true })
            throw new InvalidOperationException("The video surface must be reported on the UI thread.");
    }

    /// <summary>
    /// Engine tracing and full LibVLC logs. Off by default. Turn on with
    /// <c>adb shell setprop log.tag.ArielEngine DEBUG</c> and restart the app.
    /// </summary>
    private static readonly bool Diagnostics =
        global::Android.Util.Log.IsLoggable(Tag, global::Android.Util.LogPriority.Debug);

    private static void Trace(string message)
    {
        if (Diagnostics)
            global::Android.Util.Log.Debug(Tag, message);
    }

    private void OnSurfaceChanged()
    {
        if (!_waitingForSurface)
            UpdateVideoOutput();
        else if (_surfaceAttached)
            Launch();
    }

    /// <summary>Keeps LibVLC from running a video output without a surface (docs/SPIKE.md finding 7).</summary>
    private void UpdateVideoOutput()
    {
        if (!Live)
            return;

        var player = _player!;
        if (Diagnostics)
            Trace($"video output: enabled={_videoEnabled} surface={_surfaceAttached} noVideoStart={_startedWithoutVideo} track={player.VideoTrack} saved={_savedVideoTrack} status={_status}");
        if (!(_videoEnabled && _surfaceAttached))
        {
            if (!_startedWithoutVideo && player.VideoTrack >= 0)
            {
                _savedVideoTrack = player.VideoTrack;
                player.SetVideoTrack(-1);
            }
        }
        else if (_startedWithoutVideo)
        {
            // An input started with :no-video cannot select a video track later.
            if (_item!.Kind == MediaKind.Video)
                Start(CurrentPositionMs);
        }
        else if (_savedVideoTrack is { } track)
        {
            _savedVideoTrack = null;
            player.SetVideoTrack(track);

            // A track selected in mid-stream starts out of sync with the clock and stays black for
            // seconds. A seek to the current position flushes and resyncs it from the last key frame.
            // The last published position is used: LibVLC can still report the time from before a
            // seek that was issued a moment ago, and this resync must not undo that seek.
            player.Time = _positionMs;

            // The requested state decides, not only the reported one: a pause that was just requested
            // may not have been reported yet, and it would leave the new decoder with a black picture.
            // Never while opening: the first running event still has the start settings to apply.
            if (_status == PlayerStatus.Paused || (_pauseRequested && _status == PlayerStatus.Playing))
                BeginFrameReveal(player);
        }
    }

    /// <summary>
    /// Shows a picture for a paused input whose video track was just selected again.
    /// LibVLC 3 has no reliable way to draw one frame in that state: the new decoder is not paused
    /// although the input is, so the first picture races the stopped clock (black about one time in
    /// eight), stepping a frame aborts on the assertion "p_owner->paused", and a restart with
    /// :start-paused stays black. So the input runs muted until its clock has moved, then pauses again.
    /// The reported status stays Paused the whole time.
    /// </summary>
    private void BeginFrameReveal(MediaPlayer player)
    {
        _revealFromMs = _positionMs;
        _revealingFrame = true;
        player.Mute = true;
        player.SetPause(false);

        // If the clock never moves (end of file, a decoder that fails), do not stay muted and running.
        var reveal = ++_revealId;
        var generation = _generation;
        _ = Task.Delay(RevealTimeout).ContinueWith(
            _ => _queue.Post(() =>
            {
                if (generation == _generation && reveal == _revealId)
                    EndFrameReveal(pause: true);
            }),
            TaskScheduler.Default);
    }

    private void EndFrameReveal(bool pause)
    {
        if (!_revealingFrame)
            return;

        _revealingFrame = false;
        if (_player is not { } player)
            return;

        if (pause)
            player.SetPause(true);
        player.Mute = false;
    }

    private MediaPlayer EnsurePlayer(int subtitleMarginPx)
    {
        if (_player is not null && _instanceStyle == _subtitleStyle && _instanceMarginPx == subtitleMarginPx)
            return _player;

        // A new subtitle style needs a new LibVLC instance (see SubtitleOptions).
        DisposePlayer();
        var style = _subtitleStyle;
        _instanceStyle = style;
        _instanceMarginPx = subtitleMarginPx;

        // The native libraries were loaded in the constructor, on the UI thread.
        // Debug level is required: the decoder line parsed in OnLog is a debug message.
        // Subscribing to Log replaces LibVLC's own logger, so nothing is written to logcat by LibVLC itself.
        try
        {
            _libVlc = new LibVLC(enableDebugLogs: true, SubtitleOptions(style, subtitleMarginPx));
        }
        catch (VLCException ex)
        {
            // An option this LibVLC build does not know must not stop playback.
            global::Android.Util.Log.Warn(Tag, $"Subtitle options rejected, using the defaults: {ex.Message}");
            _libVlc = new LibVLC(enableDebugLogs: true);
        }

        _libVlc.Log += OnLog;

        var player = new MediaPlayer(_libVlc);
        player.SetAndroidContext(_window.Handle);

        // LibVLC raises events while holding its own locks. A handler only copies data and posts.
        player.Playing += (_, _) => PostEvent(() => OnRunning(PlayerStatus.Playing));
        player.Paused += (_, _) => PostEvent(() => OnRunning(PlayerStatus.Paused));
        player.EncounteredError += (_, _) => PostEvent(() => Fail("The file cannot be played."));
        player.ESAdded += (_, _) => PostEvent(RefreshTracks);
        player.ESDeleted += (_, _) => PostEvent(RefreshTracks);
        player.ESSelected += (_, _) => PostEvent(RefreshTracks);
        player.Vout += (_, _) => PostEvent(RefreshTracks);
        player.EndReached += (_, _) => PostEvent(() =>
        {
            EndFrameReveal(pause: false);
            _status = PlayerStatus.Ended;
            _positionMs = _durationMs;
            _startMs = 0;
            Publish();
        });
        player.LengthChanged += (_, e) =>
        {
            var length = e.Length;
            PostEvent(() =>
            {
                _durationMs = length;
                Publish();
            });
        };
        player.TimeChanged += (_, e) =>
        {
            var now = System.Environment.TickCount64;
            if (!_revealingFrame && now - Interlocked.Read(ref _lastPositionPost) < PositionIntervalMs)
                return;

            Interlocked.Exchange(ref _lastPositionPost, now);
            var time = e.Time;
            PostEvent(() =>
            {
                if (_revealingFrame)
                {
                    // The clock has moved past the resync point, so that picture is on screen.
                    if (time - _revealFromMs >= RevealAdvanceMs)
                        EndFrameReveal(pause: true);
                    return;
                }

                _positionMs = time;
                Publish();
            });
        };

        return _player = player;
    }

    /// <summary>Posts to the queue and drops the event if the input it belongs to was replaced meanwhile.</summary>
    private void PostEvent(Action handler)
    {
        var generation = Volatile.Read(ref _generation);
        _queue.Post(() =>
        {
            if (generation == _generation)
                handler();
        });
    }

    private void OnRunning(PlayerStatus status)
    {
        var player = _player!;

        // The resume that reveals a frame is internal: the input is still paused as far as anyone knows.
        if (_revealingFrame && status == PlayerStatus.Playing)
            return;

        _status = status;
        if (status == PlayerStatus.Paused)
            _positionMs = Math.Max(0, player.Time);

        if (_settingsPending)
        {
            _settingsPending = false;
            player.Volume = _volume;
            if (_speed != 1f)
                player.SetRate(_speed);
            if (_audioDelayMs != 0)
                player.SetAudioDelay(_audioDelayMs * 1000);
            if (_subtitleDelayMs != 0)
                player.SetSpuDelay(_subtitleDelayMs * 1000);
            if (_audioTrackId is { } audio)
                player.SetAudioTrack(audio);

            // An external file is selected by LibVLC only while no track was chosen after it was added.
            if (_subtitleFileUri is { } subtitleFile)
                player.AddSlave(MediaSlaveType.Subtitle, subtitleFile, _subtitleTrackId is null);
            if (_subtitleTrackId is { } subtitle)
                player.SetSpu(subtitle);
            if (_seekOnStart is { } seek)
            {
                _seekOnStart = null;
                player.Time = seek;
                _positionMs = seek;
            }
        }

        // Pause and play requests can cross the opening of an input, where LibVLC ignores them.
        // The requested state wins once the input reports its own.
        if (status == PlayerStatus.Playing && _pauseRequested)
            player.SetPause(true);
        else if (status == PlayerStatus.Paused && !_pauseRequested)
            player.SetPause(false);

        UpdateVideoOutput();
        if (Live)
            RefreshTracks();
    }

    private void RefreshTracks()
    {
        var player = _player!;
        var tracks = _media?.Tracks ?? [];

        var audioTracks = Describe(player.AudioTrackDescription, tracks);
        var subtitleTracks = Describe(player.SpuDescription, tracks);
        var audio = player.AudioTrack;
        SetTracks(audioTracks, subtitleTracks, new SelectedTracks(audio >= 0 ? audio : null, Math.Max(-1, player.Spu)));

        VideoFormat? format = null;
        _hasVideo = false;
        foreach (var track in tracks)
        {
            if (track.TrackType != TrackType.Video)
                continue;

            _hasVideo = true;
            var video = track.Data.Video;
            if (video.Width == 0 || video.Height == 0)
                continue;

            var rotated = video.Orientation is VideoOrientation.LeftTop or VideoOrientation.LeftBottom
                or VideoOrientation.RightTop or VideoOrientation.RightBottom;
            var sarNum = (int)Math.Max(1, video.SarNum);
            var sarDen = (int)Math.Max(1, video.SarDen);
            format = rotated
                ? new VideoFormat((int)video.Height, (int)video.Width, sarDen, sarNum)
                : new VideoFormat((int)video.Width, (int)video.Height, sarNum, sarDen);
            break;
        }

        if (format is not null)
            SetFormat(format);
        Publish();
    }

    private static IReadOnlyList<TrackInfo> Describe(TrackDescription[] descriptions, MediaTrack[] tracks)
    {
        var result = new List<TrackInfo>(descriptions.Length);
        foreach (var description in descriptions)
        {
            if (description.Id < 0)
                continue;

            var language = Array.Find(tracks, t => t.Id == description.Id).Language;
            result.Add(new TrackInfo(description.Id, description.Name ?? $"Track {description.Id}", language));
        }

        return result;
    }

    private void SetSelected(SelectedTracks selected) => SetTracks(_audioTracks, _subtitleTracks, selected);

    private void SetTracks(
        IReadOnlyList<TrackInfo> audio, IReadOnlyList<TrackInfo> subtitles, SelectedTracks selected)
    {
        var changed = selected != _selected
            || !audio.SequenceEqual(_audioTracks)
            || !subtitles.SequenceEqual(_subtitleTracks);

        _audioTracks = audio;
        _subtitleTracks = subtitles;
        _selected = selected;
        if (changed)
            Raise(() => TracksChanged?.Invoke(this, EventArgs.Empty));
    }

    /// <summary>Null means not known. A subtitle id of -1 means subtitles are off.</summary>
    private sealed record SelectedTracks(int? Audio, int? Subtitle)
    {
        public static SelectedTracks Unknown { get; } = new(null, null);
    }

    private void SetFormat(VideoFormat? format)
    {
        if (_format == format)
            return;

        _format = format;
        Raise(() => FormatChanged?.Invoke(this, format));
    }

    private void OnLog(object? sender, LogEventArgs e)
    {
        const string marker = "using video decoder module \"";
        var message = e.Message;
        if (message is null)
            return;

        if (e.Level == LogLevel.Error)
            global::Android.Util.Log.Warn(Tag, $"libvlc: {message}");
        else if (Diagnostics)
            global::Android.Util.Log.Debug(Tag, $"libvlc [{e.Level}] {e.Module}: {message}");

        // The only reliable signal of the decoder really in use (docs/SPIKE.md finding 4).
        if (!message.StartsWith(marker, StringComparison.Ordinal))
            return;

        var hardware = message.AsSpan(marker.Length).StartsWith("mediacodec", StringComparison.Ordinal);
        PostEvent(() =>
        {
            _actualDecoder = hardware ? DecoderMode.Hardware : DecoderMode.Software;
            Publish();
        });
    }
}
