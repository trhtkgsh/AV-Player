using System.Globalization;
using Android.OS;
using LibVLCSharp.Shared;
using Org.Videolan.Libvlc;
using Player.Core.Contracts;
using Player.Core.Models;
using AndroidApp = Android.App.Application;
using AndroidUri = Android.Net.Uri;
using VlcMedia = LibVLCSharp.Shared.Media;

namespace Player.App.Engine;

/// <summary>
/// LibVLC behind <see cref="IPlayerEngine"/>. A process-wide singleton.
/// Fields without a volatile marker are touched only on the command queue thread.
/// </summary>
public sealed partial class VlcPlayerEngine : IPlayerEngine, IVideoOutputBinding, IDisposable
{
    internal const string Tag = "ArielEngine";
    private const int PositionIntervalMs = 250;
    private static readonly TimeSpan SurfaceWait = TimeSpan.FromSeconds(2);

    private readonly EngineCommandQueue _queue = new("player-engine");
    private readonly AWindow _window = new(new NoOpSurfaceCallback());

    /// <summary>
    /// Must be constructed on the UI thread. LibVLC's native libraries are loaded through
    /// System.loadLibrary, which finds the app's own libraries only from a thread that has a Java frame
    /// of the app on it. On the engine's own thread the load fails with "library not found".
    /// </summary>
    public VlcPlayerEngine()
    {
        if (Looper.MainLooper is not { IsCurrentThread: true })
            throw new InvalidOperationException("The player engine must be created on the UI thread.");

        LibVLCSharp.Shared.Core.Initialize();
    }

    private LibVLC? _libVlc;
    private MediaPlayer? _player;
    private VlcMedia? _media;
    private ParcelFileDescriptor? _descriptor;
    private Timer? _surfaceTimer;

    private MediaItem? _item;
    private PlayerStatus _status;
    private long _positionMs;
    private long _durationMs;
    private string? _error;
    private DecoderMode _requestedDecoder = DecoderMode.Hardware;
    private DecoderMode? _actualDecoder;
    private bool _hasVideo;
    private bool _videoEnabled = true;
    private float _speed = 1f;
    private int _volume = 100;
    private int? _audioTrackId;
    private int? _subtitleTrackId;
    private long _audioDelayMs;
    private long _subtitleDelayMs;
    private SubtitleStyle _subtitleStyle = SubtitleStyle.Default;

    private long _startMs;
    private bool _pauseRequested;
    private long? _seekOnStart;
    private string? _subtitleFileUri;
    private bool _waitingForSurface;
    private bool _startedWithoutVideo;
    private bool _settingsPending;
    private int? _savedVideoTrack;

    private int _generation;
    private long _lastPositionPost;
    private volatile PlayerSnapshot _snapshot = PlayerSnapshot.Idle;
    private volatile IReadOnlyList<TrackInfo> _audioTracks = [];
    private volatile IReadOnlyList<TrackInfo> _subtitleTracks = [];

    public PlayerSnapshot Snapshot => _snapshot;
    public event EventHandler<PlayerSnapshot>? SnapshotChanged;
    public IReadOnlyList<TrackInfo> AudioTracks => _audioTracks;
    public IReadOnlyList<TrackInfo> SubtitleTracks => _subtitleTracks;

    /// <summary>A LibVLC input exists and accepts commands.</summary>
    private bool Live =>
        !_waitingForSurface && _status is PlayerStatus.Opening or PlayerStatus.Playing or PlayerStatus.Paused;

    /// <summary>LibVLC reports no time until the input plays, so the requested position is used while opening.</summary>
    private long CurrentPositionMs
    {
        get
        {
            var time = _status == PlayerStatus.Opening ? -1 : _player?.Time ?? -1;
            return time >= 0 ? time : _positionMs;
        }
    }

    public Task OpenAsync(MediaItem item, EffectivePlaybackSettings settings, bool startPaused, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(settings);
        return _queue.Enqueue(() =>
        {
            _item = item;
            _requestedDecoder = settings.Decoder;
            _speed = Math.Clamp(settings.Speed, 0.25f, 4f);
            _audioTrackId = settings.AudioTrackId;
            _subtitleTrackId = settings.SubtitleTrackId;
            _audioDelayMs = settings.AudioDelayMs;
            _subtitleDelayMs = settings.SubtitleDelayMs;
            _subtitleStyle = settings.SubtitleStyle;
            _subtitleFileUri = null;
            _pauseRequested = startPaused;
            _durationMs = 0;
            SetFormat(null);
            Start(Math.Max(0, settings.StartPositionMs));
        }, ct);
    }

    public Task PlayAsync(CancellationToken ct) => _queue.Enqueue(() =>
    {
        if (_item is null)
            return;

        _pauseRequested = false;
        if (Live)
            _player!.SetPause(false);
        else if (!_waitingForSurface)
            Start(_startMs);
    }, ct);

    public Task PauseAsync(CancellationToken ct) => _queue.Enqueue(() =>
    {
        // The intent is kept, so a pause that arrives while opening or just before a restart is not lost.
        _pauseRequested = true;
        if (_status == PlayerStatus.Playing)
            _player!.SetPause(true);
    }, ct);

    public Task StopAsync(CancellationToken ct) => _queue.Enqueue(() =>
    {
        Halt();
        _item = null;
        _status = PlayerStatus.Idle;
        _positionMs = _durationMs = 0;
        _actualDecoder = null;
        _error = null;
        SetFormat(null);
        Publish();
    }, ct);

    public Task SeekAsync(long positionMs, CancellationToken ct) => _queue.Enqueue(() =>
    {
        if (_item is null)
            return;

        var target = Math.Max(0, _durationMs > 0 ? Math.Min(positionMs, _durationMs) : positionMs);
        if (Live && _status != PlayerStatus.Opening)
        {
            _player!.Time = target;
        }
        else
        {
            // Not seekable yet: start there, or seek as soon as the input plays.
            _startMs = target;
            if (Live)
                _seekOnStart = target;
        }

        _positionMs = target;
        Publish();
    }, ct);

    public Task SetSpeedAsync(float speed, CancellationToken ct) => _queue.Enqueue(() =>
    {
        _speed = Math.Clamp(speed, 0.25f, 4f);
        if (Live)
            _player!.SetRate(_speed);
        Publish();
    }, ct);

    public Task SetVolumeAsync(int percent, CancellationToken ct) => _queue.Enqueue(() =>
    {
        _volume = Math.Clamp(percent, 0, 200);
        if (_player is not null)
            _player.Volume = _volume;
        Publish();
    }, ct);

    public Task SetDecoderAsync(DecoderMode mode, CancellationToken ct) => _queue.Enqueue(() =>
    {
        _requestedDecoder = mode;
        // LibVLC 3 picks the decoder per media: restart at the current position (docs/SPIKE.md finding 3).
        if (Live)
            Start(CurrentPositionMs);
        else
            Publish();
    }, ct);

    public Task SetVideoEnabledAsync(bool enabled, CancellationToken ct) => _queue.Enqueue(() =>
    {
        _videoEnabled = enabled;
        if (_waitingForSurface && !enabled)
            Launch();
        else
            UpdateVideoOutput();
        Publish();
    }, ct);

    public Task SelectAudioTrackAsync(int trackId, CancellationToken ct) => _queue.Enqueue(() =>
    {
        _audioTrackId = trackId;
        if (Live)
            _player!.SetAudioTrack(trackId);
    }, ct);

    public Task SetAudioDelayAsync(long delayMs, CancellationToken ct) => _queue.Enqueue(() =>
    {
        _audioDelayMs = delayMs;
        if (Live)
            _player!.SetAudioDelay(delayMs * 1000);
    }, ct);

    public Task SelectSubtitleTrackAsync(int trackId, CancellationToken ct) => _queue.Enqueue(() =>
    {
        _subtitleTrackId = trackId;
        if (Live)
            _player!.SetSpu(trackId);
    }, ct);

    public Task SetSubtitleDelayAsync(long delayMs, CancellationToken ct) => _queue.Enqueue(() =>
    {
        _subtitleDelayMs = delayMs;
        if (Live)
            _player!.SetSpuDelay(delayMs * 1000);
    }, ct);

    public Task AddSubtitleFileAsync(string localFilePath, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrEmpty(localFilePath);
        return _queue.Enqueue(() =>
        {
            var uri = new Uri(localFilePath).AbsoluteUri;
            if (!Live || !_player!.AddSlave(MediaSlaveType.Subtitle, uri, true))
                throw new InvalidOperationException("The subtitle file could not be loaded.");

            // Remembered so that a decoder switch or a video restart loads it again.
            _subtitleFileUri = uri;
        }, ct);
    }

    /// <summary>Takes effect when playback next starts. A live update is a Phase 2 task.</summary>
    public Task SetSubtitleStyleAsync(SubtitleStyle style, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(style);
        return _queue.Enqueue(() => _subtitleStyle = style, ct);
    }

    public void Dispose()
    {
        _queue.Post(() =>
        {
            Halt();
            _player?.Dispose();
            _libVlc?.Dispose();
            _queue.Dispose();
        });
    }

    private void Start(long startMs)
    {
        Halt();
        _status = PlayerStatus.Opening;
        _positionMs = _startMs = startMs;
        _error = null;
        _actualDecoder = null;

        // The player page creates the surface right after the open command. Starting without it would
        // fail the video output and lose hardware decoding (docs/SPIKE.md findings 6 and 7).
        if (_item!.Kind == MediaKind.Video && _videoEnabled && !_surfaceAttached)
        {
            _waitingForSurface = true;
            var generation = _generation;
            _surfaceTimer = new Timer(
                _ => _queue.Post(() =>
                {
                    if (generation == _generation && _waitingForSurface)
                        Launch();
                }),
                null, SurfaceWait, Timeout.InfiniteTimeSpan);
        }
        else
        {
            Launch();
        }

        Publish();
    }

    private void Launch()
    {
        _waitingForSurface = false;
        _surfaceTimer?.Dispose();
        _surfaceTimer = null;

        try
        {
            var player = EnsurePlayer();
            var withVideo = _videoEnabled && _surfaceAttached;

            // Media is opened by content URI through a file descriptor, never by path.
            // LibVLC duplicates the descriptor when the input starts, so it stays open until Halt.
            _descriptor = AndroidApp.Context.ContentResolver!.OpenFileDescriptor(AndroidUri.Parse(_item!.ContentUri)!, "r")
                ?? throw new IOException("The media file could not be opened.");
            _media = new VlcMedia(_libVlc!, _descriptor.Fd);
            _media.AddOption(_requestedDecoder == DecoderMode.Hardware ? ":codec=mediacodec_ndk,all" : ":codec=all");
            if (_startMs > 0)
                _media.AddOption(string.Create(CultureInfo.InvariantCulture, $":start-time={_startMs / 1000.0:F3}"));
            if (_pauseRequested)
                _media.AddOption(":start-paused");
            if (!withVideo)
                _media.AddOption(":no-video");
            AddSubtitleStyleOptions(_media, _subtitleStyle);

            _startedWithoutVideo = !withVideo;
            _settingsPending = true;
            if (!player.Play(_media))
                throw new InvalidOperationException("Playback could not be started.");
        }
        catch (Exception ex)
        {
            Fail(ex.Message);
        }
    }

    /// <summary>
    /// Stops the current input. Events it raised are dropped through the generation counter.
    /// The video format is kept, so a restart of the same file does not resize the surface.
    /// </summary>
    private void Halt()
    {
        _surfaceTimer?.Dispose();
        _surfaceTimer = null;
        _waitingForSurface = false;

        // Stopped is raised inside Stop() and captures the old generation, so it is ignored.
        _player?.Stop();
        Interlocked.Increment(ref _generation);

        _media?.Dispose();
        _media = null;
        _descriptor?.Close();
        _descriptor?.Dispose();
        _descriptor = null;
        _seekOnStart = null;
        _savedVideoTrack = null;
        _startedWithoutVideo = false;
        _hasVideo = false;
        _audioTracks = [];
        _subtitleTracks = [];
    }

    private void Fail(string message)
    {
        global::Android.Util.Log.Warn(Tag, $"Playback failed: {message}");
        Halt();
        _status = PlayerStatus.Error;
        _error = message;
        SetFormat(null);
        Publish();
    }

    private void Publish()
    {
        var snapshot = new PlayerSnapshot(
            _status, _item?.Id, _positionMs, _durationMs, _speed,
            _requestedDecoder, _actualDecoder, _hasVideo, _videoEnabled, _volume, _error);
        if (snapshot == _snapshot)
            return;

        _snapshot = snapshot;
        Raise(() => SnapshotChanged?.Invoke(this, snapshot));
    }

    /// <summary>A failing subscriber must not fault the command that changed the state.</summary>
    private static void Raise(Action notify)
    {
        try
        {
            notify();
        }
        catch (Exception ex)
        {
            global::Android.Util.Log.Error(Tag, $"Engine event subscriber failed: {ex}");
        }
    }

    private static void AddSubtitleStyleOptions(VlcMedia media, SubtitleStyle style)
    {
        if (style == SubtitleStyle.Default)
            return;

        var scale = Math.Clamp(style.FontScale, 0.25, 4.0);
        media.AddOption($":freetype-rel-fontsize={(int)Math.Round(16 / scale)}");
        media.AddOption($":freetype-color={style.TextColorArgb & 0xFFFFFF}");
        media.AddOption($":freetype-outline-color={style.OutlineColorArgb & 0xFFFFFF}");
        media.AddOption($":freetype-outline-thickness={Math.Clamp(style.OutlineThickness, 0, 3) * 2}");
        media.AddOption($":freetype-background-opacity={(int)Math.Round(Math.Clamp(style.BackgroundOpacity, 0, 1) * 255)}");
        if (style.Bold)
            media.AddOption(":freetype-bold");
    }

    private sealed class NoOpSurfaceCallback : Java.Lang.Object, AWindow.ISurfaceCallback
    {
        public void OnSurfacesCreated(AWindow? aWindow)
        {
        }

        public void OnSurfacesDestroyed(AWindow? aWindow)
        {
        }
    }
}
