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

    public VideoFormat? Format => _format;
    public event EventHandler<VideoFormat?>? FormatChanged;

    /// <summary>
    /// Called on the UI thread: LibVLC's AWindow only accepts view changes there.
    /// The window size is reported explicitly (docs/SPIKE.md finding 5).
    /// </summary>
    public void SurfaceReady(object nativeSurface, int widthPx, int heightPx)
    {
        EnsureMainThread();
        if (nativeSurface is not Surface surface)
            throw new ArgumentException("An Android.Views.Surface is required.", nameof(nativeSurface));

        if (_surfaceAttached)
            _window.DetachViews();
        _window.SetVideoSurface(surface, null);
        _window.AttachViews();
        _window.SetWindowSize(widthPx, heightPx);
        _surfaceAttached = true;
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
        _queue.Post(OnSurfaceChanged);
    }

    private static void EnsureMainThread()
    {
        if (Looper.MainLooper is not { IsCurrentThread: true })
            throw new InvalidOperationException("The video surface must be reported on the UI thread.");
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
                Start(Math.Max(0, player.Time), _status == PlayerStatus.Paused);
        }
        else if (_savedVideoTrack is { } track)
        {
            _savedVideoTrack = null;
            player.SetVideoTrack(track);
        }
    }

    private MediaPlayer EnsurePlayer()
    {
        if (_player is not null)
            return _player;

        LibVLCSharp.Shared.Core.Initialize();
        _libVlc = new LibVLC();
        _libVlc.Log += OnLog;

        var player = new MediaPlayer(_libVlc);
        player.SetAndroidContext(_window.Handle);

        // LibVLC raises events while holding its own locks. A handler only copies data and posts.
        player.Playing += (_, _) => PostEvent(() => OnRunning(PlayerStatus.Playing));
        player.Paused += (_, _) => PostEvent(() => OnRunning(PlayerStatus.Paused));
        player.EncounteredError += (_, _) => PostEvent(() => Fail("The file cannot be played."));
        player.ESAdded += (_, _) => PostEvent(RefreshTracks);
        player.ESDeleted += (_, _) => PostEvent(RefreshTracks);
        player.Vout += (_, _) => PostEvent(RefreshTracks);
        player.EndReached += (_, _) => PostEvent(() =>
        {
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
            if (now - Interlocked.Read(ref _lastPositionPost) < PositionIntervalMs)
                return;

            Interlocked.Exchange(ref _lastPositionPost, now);
            var time = e.Time;
            PostEvent(() =>
            {
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
            if (_subtitleTrackId is { } subtitle)
                player.SetSpu(subtitle);
        }

        UpdateVideoOutput();
        if (Live)
            RefreshTracks();
    }

    private void RefreshTracks()
    {
        var player = _player!;
        var tracks = _media?.Tracks ?? [];

        _audioTracks = Describe(player.AudioTrackDescription, tracks);
        _subtitleTracks = Describe(player.SpuDescription, tracks);

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

    private void SetFormat(VideoFormat? format)
    {
        if (_format == format)
            return;

        _format = format;
        FormatChanged?.Invoke(this, format);
    }

    private void OnLog(object? sender, LogEventArgs e)
    {
        const string marker = "using video decoder module \"";
        var message = e.Message;
        if (message is null)
            return;

        if (e.Level == LogLevel.Error)
            global::Android.Util.Log.Warn(Tag, $"libvlc: {message}");

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
