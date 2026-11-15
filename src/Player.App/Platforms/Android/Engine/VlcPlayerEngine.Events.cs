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
    private const int FormatChecksAfterStart = 12;   // about three seconds of position reports
    private int _formatChecks;
    private const long FirstReportedTime = -1;
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
        ReportSurfaceSize(widthPx, heightPx);
        _queue.Post(OnSurfaceChanged);
    }

    public void SurfaceSizeChanged(int widthPx, int heightPx)
    {
        EnsureMainThread();
        if (!_surfaceAttached)
            return;

        _window.SetWindowSize(widthPx, heightPx);
        ReportSurfaceSize(widthPx, heightPx);
    }

    private long _surfaceSize;   // width in the high half, height in the low half; written on the UI thread

    private void ReportSurfaceSize(int widthPx, int heightPx)
    {
        Interlocked.Exchange(ref _surfaceSize, ((long)widthPx << 32) | (uint)heightPx);
        _queue.Post(ApplySurfaceAspect);
    }

    private string? _appliedAspect;   // the last aspect ratio given to the current input; null = LibVLC's own

    /// <summary>
    /// The surface view decides the shape of the picture (aspect mode, zoom). LibVLC would keep the
    /// video's own proportions inside the surface and add bars, so it is told to fill the surface exactly.
    /// While the format of a new file is not known the surface still has the shape of the previous one,
    /// so LibVLC keeps the proportions itself until the format arrives.
    /// </summary>
    private void ApplySurfaceAspect()
    {
        var size = Interlocked.Read(ref _surfaceSize);
        var width = (int)(size >> 32);
        var height = (int)(size & 0xFFFFFFFF);
        if (!Live || _status == PlayerStatus.Opening || width <= 0 || height <= 0)
            return;

        var aspect = _format is null
            ? null
            : string.Create(System.Globalization.CultureInfo.InvariantCulture, $"{width}:{height}");

        // The surface is resized on every frame of a pinch: only a new value reaches LibVLC.
        if (aspect == _appliedAspect)
            return;

        _appliedAspect = aspect;
        _player!.AspectRatio = aspect;
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
                BeginFrameReveal(player, _positionMs);
        }
    }

    /// <summary>
    /// Shows a picture for a paused input whose video track was just selected again, or that started paused.
    /// LibVLC 3 has no reliable way to draw one frame in that state: the new decoder is not paused
    /// although the input is, so the first picture races the stopped clock (black about one time in
    /// eight), stepping a frame aborts on the assertion "p_owner->paused", and a restart with
    /// :start-paused draws nothing. So the input runs muted until its clock has moved, then pauses again.
    /// The reported status stays Paused the whole time.
    /// </summary>
    /// <param name="fromMs">The position the clock must pass, or <see cref="FirstReportedTime"/>.</param>
    private void BeginFrameReveal(MediaPlayer player, long fromMs)
    {
        Trace($"frame reveal begins from {fromMs}");
        _revealFromMs = fromMs;
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
        Trace($"frame reveal ends, pause={pause}");
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
                if (!_clockStarted)
                {
                    _clockStarted = true;
                    _formatChecks = FormatChecksAfterStart;
                }

                // LibVLC raises no event when the decoder corrects the size of the video track
                // (for example 1920x1088 becoming 1920x1080), so the format is read again while the
                // first pictures are shown. Without this the surface kept the first size until the
                // next pause or play.
                if (_formatChecks > 0)
                {
                    _formatChecks--;
                    if (_item?.Kind == MediaKind.Video)
                        RefreshFormat(_media?.Tracks ?? []);
                }

                if (_revealingFrame)
                {
                    // The clock has moved past the resync point, so that picture is on screen.
                    if (_revealFromMs == FirstReportedTime)
                        _revealFromMs = time;
                    else if (time - _revealFromMs >= RevealAdvanceMs)
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

        // An input that has not run yet reports the time 0, whatever its start position is.
        if (status == PlayerStatus.Paused && _clockStarted)
            _positionMs = Math.Max(0, player.Time);

        if (Diagnostics)
            Trace($"running: status={status} first={_settingsPending} clock={_clockStarted} time={player.Time} position={_positionMs}");
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
            if (_externalSubtitle is { } external
                && !(File.Exists(external.Path) && player.AddSlave(MediaSlaveType.Subtitle, external.Uri, _subtitleTrackId is null)))
            {
                // The copy is gone: stop reporting it, so the subtitle policy can choose again.
                global::Android.Util.Log.Warn(Tag, "The external subtitle file could not be loaded again.");
                _externalSubtitle = null;
            }
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

        // A surface that came back can restart the input here: this event then belongs to the old one.
        var generation = _generation;
        UpdateVideoOutput();
        if (generation != _generation)
            return;

        ApplySurfaceAspect();

        // An input that starts paused draws nothing (docs/SPIKE.md finding 12): a restart of a paused
        // file (decoder switch, subtitle style) would keep the old picture, or none, until Play.
        // LibVLC reports such an input as Playing and then Paused, so the clock tells the case apart.
        if (status == PlayerStatus.Paused && _pauseRequested && !_clockStarted && !_revealTried && !_revealingFrame
            && !_startedWithoutVideo && _videoEnabled && _surfaceAttached && _item!.Kind == MediaKind.Video && Live)
        {
            _revealTried = true;
            BeginFrameReveal(player, FirstReportedTime);
        }

        if (Live)
            RefreshTracks();
    }

    private void RefreshTracks()
    {
        var player = _player!;
        var tracks = _media?.Tracks ?? [];

        var audioTracks = Describe(player.AudioTrackDescription, tracks, null, out _);
        var subtitleTracks = Describe(player.SpuDescription, tracks, _externalSubtitle, out var externalId);
        _externalTrackId = externalId;

        var audio = player.AudioTrack;

        // Before the tracks are known LibVLC reports -1 for the subtitle, which would read as "off".
        int? subtitle = _status == PlayerStatus.Opening && subtitleTracks.Count == 0 ? null : Math.Max(-1, player.Spu);
        SetTracks(new TrackState(audioTracks, subtitleTracks, audio >= 0 ? audio : null, subtitle));
        RefreshFormat(tracks);
        Publish();
    }

    /// <summary>Reads the size of the video track. Raises FormatChanged only when it differs.</summary>
    private void RefreshFormat(MediaTrack[] tracks)
    {
        VideoFormat? format = null;
        var hasVideo = false;
        foreach (var track in tracks)
        {
            if (track.TrackType != TrackType.Video)
                continue;

            hasVideo = true;
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

        _hasVideo = hasVideo;
        if (format is not null)
        {
            if (Diagnostics && format != _format)
                Trace($"video format {format.Width}x{format.Height} sar {format.SarNumerator}:{format.SarDenominator}");
            SetFormat(format);
        }
    }

    /// <param name="external">The external subtitle file, when the list may contain its track.</param>
    /// <param name="externalId">The id of that track, or null when it is not in the list.</param>
    private static IReadOnlyList<TrackInfo> Describe(
        TrackDescription[] descriptions, MediaTrack[] tracks, ExternalSubtitle? external, out int? externalId)
    {
        externalId = null;
        var result = new List<TrackInfo>(descriptions.Length);
        foreach (var description in descriptions)
        {
            if (description.Id < 0)
                continue;

            var name = description.Name ?? $"Track {description.Id}";

            // LibVLC names the file's track after its path ("Track 1 - [app/files/subtitles/6627ac05]").
            // The path ends with the file's own name, which is how the track is recognized.
            if (external is not null && externalId is null && name.EndsWith(external.NameKey, StringComparison.Ordinal))
            {
                externalId = description.Id;
                result.Add(new TrackInfo(description.Id, external.DisplayName, null));
                continue;
            }

            var language = Array.Find(tracks, t => t.Id == description.Id).Language;
            result.Add(new TrackInfo(description.Id, name, language));
        }

        return result;
    }

    private void SetTracks(TrackState state)
    {
        var current = _tracks;
        var changed = state.SelectedAudio != current.SelectedAudio
            || state.SelectedSubtitle != current.SelectedSubtitle
            || !state.Audio.SequenceEqual(current.Audio)
            || !state.Subtitles.SequenceEqual(current.Subtitles);
        if (!changed)
            return;

        _tracks = state;
        Raise(() => TracksChanged?.Invoke(this, EventArgs.Empty));
    }

    /// <summary>
    /// One immutable picture of the tracks, so a reader never sees a new list with an old selection.
    /// A null selection means not known. A subtitle id of -1 means subtitles are off.
    /// </summary>
    private sealed record TrackState(
        IReadOnlyList<TrackInfo> Audio, IReadOnlyList<TrackInfo> Subtitles, int? SelectedAudio, int? SelectedSubtitle)
    {
        public static TrackState Empty { get; } = new([], [], null, null);
    }

    /// <summary>An external subtitle file of the open item.</summary>
    private sealed record ExternalSubtitle(string Path, string Uri, string DisplayName)
    {
        /// <summary>
        /// The end of the name LibVLC gives the file's track: the last part of the file name before its
        /// extension (LibVLC cuts the name at dots), followed by the closing bracket.
        /// </summary>
        public string NameKey { get; } =
            System.IO.Path.GetFileNameWithoutExtension(Path).Split('.')[^1] + "]";
    }

    private void SetFormat(VideoFormat? format)
    {
        if (_format == format)
            return;

        _format = format;
        Raise(() => FormatChanged?.Invoke(this, format));
        ApplySurfaceAspect();
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
