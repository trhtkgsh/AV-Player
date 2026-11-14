using Player.Core.Models;

namespace Player.Core.Contracts;

/// <summary>
/// The playback engine. A process-wide singleton that is never owned by a page.
/// Every method enqueues a command on the engine's own thread and completes when the command ran.
/// Events are raised on that thread, never on the UI thread.
/// </summary>
public interface IPlayerEngine
{
    PlayerSnapshot Snapshot { get; }

    /// <summary>Raised on any change. Position updates arrive at most four times per second.</summary>
    event EventHandler<PlayerSnapshot>? SnapshotChanged;

    IReadOnlyList<TrackInfo> AudioTracks { get; }
    IReadOnlyList<TrackInfo> SubtitleTracks { get; }

    Task OpenAsync(MediaItem item, EffectivePlaybackSettings settings, bool startPaused, CancellationToken ct);
    Task PlayAsync(CancellationToken ct);
    Task PauseAsync(CancellationToken ct);
    Task StopAsync(CancellationToken ct);
    Task SeekAsync(long positionMs, CancellationToken ct);

    /// <param name="speed">0.25 .. 4.0, pitch corrected.</param>
    Task SetSpeedAsync(float speed, CancellationToken ct);

    /// <param name="percent">0 .. 200.</param>
    Task SetVolumeAsync(int percent, CancellationToken ct);

    /// <summary>Restarts playback at the current position with the other decoder.</summary>
    Task SetDecoderAsync(DecoderMode mode, CancellationToken ct);

    /// <summary>False switches to audio-only.</summary>
    Task SetVideoEnabledAsync(bool enabled, CancellationToken ct);

    Task SelectAudioTrackAsync(int trackId, CancellationToken ct);
    Task SetAudioDelayAsync(long delayMs, CancellationToken ct);

    /// <param name="trackId">-1 turns subtitles off.</param>
    Task SelectSubtitleTrackAsync(int trackId, CancellationToken ct);

    Task SetSubtitleDelayAsync(long delayMs, CancellationToken ct);

    /// <param name="localFilePath">A file inside the app's own storage.</param>
    Task AddSubtitleFileAsync(string localFilePath, CancellationToken ct);

    Task SetSubtitleStyleAsync(SubtitleStyle style, CancellationToken ct);
}

/// <summary>
/// The boundary between the engine and the native video surface.
/// Implemented by the engine, called by the surface. The surface knows nothing about the engine's internals.
/// </summary>
public interface IVideoOutputBinding
{
    VideoFormat? Format { get; }
    event EventHandler<VideoFormat?>? FormatChanged;

    /// <param name="nativeSurface">The platform surface object (Android.Views.Surface).</param>
    void SurfaceReady(object nativeSurface, int widthPx, int heightPx);

    void SurfaceSizeChanged(int widthPx, int heightPx);
    void SurfaceLost();
}

/// <summary>Aspect mode, zoom/pan and window brightness of the video view.</summary>
public interface IVideoSurface
{
    AspectMode Aspect { get; }
    ViewTransform Transform { get; }

    /// <summary>0 .. 1, or -1 when the system default is in effect.</summary>
    double WindowBrightness { get; }

    event EventHandler? Changed;

    void SetAspect(AspectMode mode);

    /// <summary>The transform is clamped to 100% .. 400% and the pan keeps the picture on screen.</summary>
    void SetTransform(ViewTransform transform);

    /// <param name="value">0 .. 1, or null to restore the system default.</param>
    void SetWindowBrightness(double? value);
}

/// <summary>Native touch input turned into semantic events. Has no side effects of its own.</summary>
public interface IGestureSource
{
    /// <summary>While true only taps are reported.</summary>
    bool Locked { get; set; }

    event EventHandler<GestureEvent>? Gesture;
}

/// <summary>Runs a queue of items on top of the engine. The only caller of <see cref="IPlayerEngine.OpenAsync"/>.</summary>
public interface IPlaybackCoordinator
{
    MediaItem? Current { get; }
    IReadOnlyList<MediaItem> Queue { get; }
    RepeatMode Repeat { get; }
    AbRepeat? AbRepeat { get; }
    event EventHandler? Changed;

    Task OpenAsync(MediaItem item, IReadOnlyList<MediaItem> queue, CancellationToken ct);
    Task NextAsync(CancellationToken ct);
    Task PreviousAsync(CancellationToken ct);
    Task SkipAsync(long deltaMs, CancellationToken ct);
    Task SetRepeatAsync(RepeatMode mode, CancellationToken ct);

    /// <param name="range">Null clears the loop.</param>
    Task SetAbRepeatAsync(AbRepeat? range, CancellationToken ct);

    Task CloseAsync(CancellationToken ct);
}
