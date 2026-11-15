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

    private readonly EngineCommandQueue _queue;
    private readonly AWindow _window;

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

        // Created last, so a failure above leaves no engine thread behind.
        _window = new AWindow(new NoOpSurfaceCallback());
        _queue = new EngineCommandQueue("player-engine");
    }

    private LibVLC? _libVlc;
    private SubtitleStyle? _instanceStyle;   // the style the LibVLC instance was created with
    private int _instanceMarginPx;           // and its subtitle margin
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
    private long _audioDelayMs;      // read from other threads through Interlocked
    private long _subtitleDelayMs;   // read from other threads through Interlocked
    private volatile SubtitleStyle _subtitleStyle = SubtitleStyle.Default;

    private long _startMs;
    private bool _pauseRequested;
    private long? _seekOnStart;
    private volatile ExternalSubtitle? _externalSubtitle;
    private int? _externalTrackId;           // the track LibVLC created for the external file, once known
    private int? _marginPx;                  // the subtitle margin of the open item, computed once per style
    private bool _waitingForSurface;
    private bool _startedWithoutVideo;
    private bool _settingsPending;
    private bool _clockStarted;              // the current input has reported a time
    private bool _revealTried;               // a first picture was already requested for the current input
    private int? _savedVideoTrack;

    private int _generation;
    private long _lastPositionPost;
    private volatile PlayerSnapshot _snapshot = PlayerSnapshot.Idle;
    private volatile TrackState _tracks = TrackState.Empty;

    public PlayerSnapshot Snapshot => _snapshot;
    public event EventHandler<PlayerSnapshot>? SnapshotChanged;
    public IReadOnlyList<TrackInfo> AudioTracks => _tracks.Audio;
    public IReadOnlyList<TrackInfo> SubtitleTracks => _tracks.Subtitles;
    public event EventHandler? TracksChanged;
    public int? CurrentAudioTrackId => _tracks.SelectedAudio;
    public int? CurrentSubtitleTrackId => _tracks.SelectedSubtitle;
    public long AudioDelayMs => Interlocked.Read(ref _audioDelayMs);
    public long SubtitleDelayMs => Interlocked.Read(ref _subtitleDelayMs);
    public SubtitleStyle SubtitleStyle => _subtitleStyle;
    public string? ExternalSubtitlePath => _externalSubtitle?.Path;

    /// <summary>A LibVLC input exists and accepts commands.</summary>
    private bool Live =>
        !_waitingForSurface && _status is PlayerStatus.Opening or PlayerStatus.Playing or PlayerStatus.Paused;

    /// <summary>
    /// LibVLC reports no time until the clock of an input has run (an input that starts paused reports 0),
    /// so the requested position is used until then.
    /// </summary>
    private long CurrentPositionMs
    {
        get
        {
            var time = _status == PlayerStatus.Opening || !_clockStarted ? -1 : _player?.Time ?? -1;
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
            Interlocked.Exchange(ref _audioDelayMs, settings.AudioDelayMs);
            Interlocked.Exchange(ref _subtitleDelayMs, settings.SubtitleDelayMs);
            _subtitleStyle = settings.SubtitleStyle;
            _externalSubtitle = null;
            _externalTrackId = null;
            _marginPx = null;
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
        SeekPictureShown();   // playback shows the picture anyway: nobody waits for it
        if (_previewTarget is { } exact && Live && _status != PlayerStatus.Opening)
            SetTime(_player!, exact, fast: false);   // play from the position itself, not from the key frame before it
        _previewTarget = null;
        if (_revealingFrame)
        {
            // Already running for the frame reveal: keep it running and say so.
            EndFrameReveal(pause: false);
            _status = PlayerStatus.Playing;
            Publish();
        }
        else if (Live)
        {
            _player!.SetPause(false);
        }
        else if (!_waitingForSurface)
        {
            Start(_startMs);
        }
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
        _externalSubtitle = null;
        _status = PlayerStatus.Idle;
        _positionMs = _durationMs = 0;
        _actualDecoder = null;
        _error = null;
        SetFormat(null);
        Publish();
    }, ct);

    /// <summary>
    /// A seek of a paused video is done in two steps, so that a finger that drags over the picture sees
    /// it follow. First a fast seek: LibVLC shows the key frame at or before the position at once. The
    /// task completes when that picture is on the screen (or after <see cref="SeekPictureTimeout"/>), which
    /// paces a caller that sends one target after another: a newer seek would throw away a picture that
    /// is still being decoded. Then, when no newer seek has come for <see cref="ExactSeekDelay"/>, an
    /// exact seek shows the picture of the position itself; playing does the same before it starts.
    /// Cancelling <paramref name="ct"/> after the seek was sent ends only the wait.
    /// </summary>
    public async Task SeekAsync(long positionMs, CancellationToken ct)
    {
        Task? picture = null;
        await _queue.Enqueue(() =>
        {
            if (_item is null)
                return;

            var target = Math.Max(0, _durationMs > 0 ? Math.Min(positionMs, _durationMs) : positionMs);
            if (Live && _status != PlayerStatus.Opening)
            {
                // A frame reveal in progress must now wait for the clock to pass the new position.
                _revealFromMs = target;
                if (_pauseRequested && !_revealingFrame && _hasVideo && _videoEnabled && _hasSurface)
                {
                    picture = PreviewSeek(target);
                }
                else
                {
                    _previewTarget = null;
                    SetTime(_player!, target, fast: false);
                }
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
        }, ct).ConfigureAwait(false);

        if (picture is not null)
            await picture.WaitAsync(ct).ConfigureAwait(false);
    }

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

        // LibVLC confirms with ESSelected a moment later; an accepted request is reported at once.
        if (Live && _player!.SetAudioTrack(trackId))
            SetTracks(_tracks with { SelectedAudio = trackId });
    }, ct);

    public Task SetAudioDelayAsync(long delayMs, CancellationToken ct) => _queue.Enqueue(() =>
    {
        Interlocked.Exchange(ref _audioDelayMs, delayMs);
        if (Live)
            _player!.SetAudioDelay(delayMs * 1000);
    }, ct);

    public Task SelectSubtitleTrackAsync(int trackId, CancellationToken ct) => _queue.Enqueue(() =>
    {
        // The external file's track gets a new id on every restart, so choosing it is remembered
        // as "the external file", not as an id.
        _subtitleTrackId = trackId == _externalTrackId ? null : trackId;
        if (Live && _player!.SetSpu(trackId))
            SetTracks(_tracks with { SelectedSubtitle = trackId });
    }, ct);

    public Task SetSubtitleDelayAsync(long delayMs, CancellationToken ct) => _queue.Enqueue(() =>
    {
        Interlocked.Exchange(ref _subtitleDelayMs, delayMs);
        if (Live)
            _player!.SetSpuDelay(delayMs * 1000);
    }, ct);

    public Task AddSubtitleFileAsync(string localFilePath, string displayName, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrEmpty(localFilePath);
        ArgumentNullException.ThrowIfNull(displayName);
        return _queue.Enqueue(() =>
        {
            var external = new ExternalSubtitle(
                localFilePath, new Uri(localFilePath).AbsoluteUri, displayName.Length > 0 ? displayName : "Subtitle file");
            if (_item is not null && _status == PlayerStatus.Opening)
            {
                // No input runs yet: its first running event adds the file (adding it here as well
                // would create the track twice).
            }
            else if (!Live || !_player!.AddSlave(MediaSlaveType.Subtitle, external.Uri, true))
            {
                throw new InvalidOperationException("The subtitle file could not be loaded.");
            }

            // Remembered so that a decoder switch or a video restart loads it again.
            // LibVLC selects the new track, so an earlier track choice no longer applies.
            _externalSubtitle = external;
            _externalTrackId = null;
            _subtitleTrackId = null;
        }, ct);
    }

    /// <summary>
    /// The subtitle renderer reads its options from the LibVLC instance (docs/SPIKE.md finding 14),
    /// so a change restarts the input at the current position with a new instance.
    /// </summary>
    public Task SetSubtitleStyleAsync(SubtitleStyle style, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(style);
        return _queue.Enqueue(() =>
        {
            if (_subtitleStyle == style)
                return;

            _subtitleStyle = style;
            _marginPx = null;
            if (Live)
                Start(CurrentPositionMs);
        }, ct);
    }

    public void Dispose()
    {
        _queue.Post(() =>
        {
            try
            {
                Halt();
                DisposePlayer();
            }
            finally
            {
                _queue.Dispose();
            }
        });
    }

    /// <summary>Called only after Halt, so no input is running.</summary>
    private void DisposePlayer()
    {
        // Anything the old player still posts must not reach its successor.
        Interlocked.Increment(ref _generation);
        _player?.Dispose();
        _player = null;
        _appliedAspect = null;
        if (_libVlc is { } libVlc)
        {
            libVlc.Log -= OnLog;
            libVlc.Dispose();
            _libVlc = null;
        }
    }

    private void Start(long startMs)
    {
        Halt();
        _status = PlayerStatus.Opening;
        _positionMs = _startMs = startMs;
        _clockStarted = false;
        _revealTried = false;
        _error = null;
        _actualDecoder = null;

        // The player page creates the surface right after the open command. Starting without it would
        // fail the video output and lose hardware decoding (docs/SPIKE.md findings 6 and 7).
        if (_item!.Kind == MediaKind.Video && _videoEnabled && !_hasSurface)
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
            // Media is opened by content URI through a file descriptor, never by path.
            // LibVLC duplicates the descriptor when the input starts, so it stays open until Halt.
            _descriptor = AndroidApp.Context.ContentResolver!.OpenFileDescriptor(AndroidUri.Parse(_item!.ContentUri)!, "r")
                ?? throw new IOException("The media file could not be opened.");

            var player = EnsurePlayer(SubtitleMarginPx());
            var withVideo = _videoEnabled && _hasSurface;
            _media = new VlcMedia(_libVlc!, _descriptor.Fd);
            _media.AddOption(_requestedDecoder == DecoderMode.Hardware ? ":codec=mediacodec_ndk,all" : ":codec=all");
            if (_startMs > 0)
                _media.AddOption(string.Create(CultureInfo.InvariantCulture, $":start-time={_startMs / 1000.0:F3}"));
            if (_pauseRequested)
                _media.AddOption(":start-paused");
            if (!withVideo)
                _media.AddOption(":no-video");

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
        EndFrameReveal(pause: false);
        SeekPictureShown();
        _previewTarget = null;

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
        _externalTrackId = null;
        SetTracks(TrackState.Empty);
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

    /// <summary>
    /// The subtitle options of a LibVLC instance. They are read from the instance, not from the media:
    /// the video output and its text renderer belong to the player, not to the input.
    /// </summary>
    private static string[] SubtitleOptions(SubtitleStyle style, int marginPx)
    {
        // The paragraph direction comes from the text, so a Hebrew or Arabic line is laid out
        // right to left and its closing punctuation ends up on the left.
        var options = new List<string> { "--freetype-text-direction=2" };
        if (style == SubtitleStyle.Default)
            return [.. options];

        var scale = Math.Clamp(style.FontScale, 0.25, 4.0);
        options.Add($"--sub-text-scale={(int)Math.Round(scale * 100)}");
        options.Add($"--freetype-color={style.TextColorArgb & 0xFFFFFF}");
        options.Add($"--freetype-outline-color={style.OutlineColorArgb & 0xFFFFFF}");

        // LibVLC knows four outline widths: 0, 2, 4 (its default) and 6.
        options.Add($"--freetype-outline-thickness={Math.Clamp(style.OutlineThickness, 0, 3) * 2}");
        options.Add($"--freetype-background-opacity={(int)Math.Round(Math.Clamp(style.BackgroundOpacity, 0, 1) * 255)}");
        if (style.Bold)
            options.Add("--freetype-bold");
        if (marginPx != 0)
            options.Add($"--sub-margin={marginPx}");
        return [.. options];
    }

    /// <summary>
    /// LibVLC takes the subtitle margin in pixels of the video, measured up from its own default position,
    /// so the video height must be known before the instance is created. Zero keeps LibVLC's position.
    /// Computed once per open item and style, so a restart never changes it (and never rebuilds the instance).
    /// </summary>
    private int SubtitleMarginPx()
    {
        if (_marginPx is { } known)
            return known;

        var standard = SubtitleStyle.Default.BottomMarginFraction;
        var fraction = Math.Clamp(_subtitleStyle.BottomMarginFraction, 0, 0.5);
        var margin = 0;
        if (_item!.Kind == MediaKind.Video && Math.Abs(fraction - standard) >= 0.0005)
        {
            var height = _format?.Height ?? ProbeVideoHeight();

            // A little below the default position is possible; further down the text would leave the picture.
            if (height > 0)
                margin = (int)Math.Round(Math.Max(fraction - standard, -0.03) * height);
        }

        _marginPx = margin;
        return margin;
    }

    /// <summary>The displayed height of the video, or 0 when it cannot be read. Uses its own descriptor.</summary>
    private int ProbeVideoHeight()
    {
        try
        {
            using var descriptor = AndroidApp.Context.ContentResolver!
                .OpenFileDescriptor(AndroidUri.Parse(_item!.ContentUri)!, "r");
            if (descriptor is null)
                return 0;

            using var retriever = new global::Android.Media.MediaMetadataRetriever();
            try
            {
                retriever.SetDataSource(descriptor.FileDescriptor);
                var width = Number(retriever.ExtractMetadata(global::Android.Media.MetadataKey.VideoWidth));
                var height = Number(retriever.ExtractMetadata(global::Android.Media.MetadataKey.VideoHeight));
                var rotation = Number(retriever.ExtractMetadata(global::Android.Media.MetadataKey.VideoRotation));
                return rotation is 90 or 270 ? width : height;
            }
            finally
            {
                retriever.Release();
                descriptor.Close();
            }
        }
        catch (Exception ex)
        {
            global::Android.Util.Log.Warn(Tag, $"The video height could not be read: {ex.Message}");
            return 0;
        }

        static int Number(string? text) =>
            int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) ? value : 0;
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
