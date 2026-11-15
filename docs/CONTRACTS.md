# CONTRACTS (Player.Core)

Status: FROZEN. Approved by the owner on 2026-11-14. Only the orchestrator may change this file,
and `src/Player.Core/Contracts` + `src/Player.Core/Models` must match it exactly.

Changes after the freeze (orchestrator only, each one logged in PROGRESS.md):
- 2026-11-15, Phase 2: `IPlayerEngine` gained `TracksChanged`, `CurrentAudioTrackId`, `CurrentSubtitleTrackId`,
  `AudioDelayMs`, `SubtitleDelayMs` and `SubtitleStyle`. The in-player settings sheets need the values in effect, and
  the engine is the only place that knows them (a track picked by LibVLC itself is in no store). Additions only.
- 2026-11-15, Phase 2: `ISubtitleService.FindSidecarsAsync` returns `SubtitleFile` records (content URI, file name,
  language suffix) instead of bare URI strings. The subtitle sheet lists the files, and a content URI has no name.
  The only implementation and the only caller were written in the same phase.

Namespaces: `Player.Core.Models`, `Player.Core.Contracts`. All types are public. Nullable enabled.

Conventions for every contract:

- Async methods take a `CancellationToken` and never block the caller.
- Events may be raised on any non-UI thread. Subscribers marshal to the UI thread themselves.
- Implementations are DI singletons unless stated otherwise.
- Times are `long` milliseconds. UTC instants are Unix seconds.

## Models

```csharp
public readonly record struct MediaFileId(long MediaStoreId, long SizeBytes, long DateModifiedSeconds)
{
    public string ToKey() => $"{MediaStoreId}:{SizeBytes}:{DateModifiedSeconds}";   // formatted with the invariant culture
}

public enum MediaKind { Video, Audio }
public enum DecoderMode { Hardware, Software }
public enum AspectMode { Fit, Stretch, Crop, Original }      // Original = 100%
public enum RepeatMode { None, File, Folder }
public enum PlayerStatus { Idle, Opening, Playing, Paused, Ended, Error }
public enum SortField { Name, DateAdded, Size, Duration }
public enum SortDirection { Ascending, Descending }
public enum ExplorerViewMode { Folders, Flat }
public enum ThemeId { DarkOled, DarkSlate, Light, HighContrast }
public enum ScreenSide { Left, Right }
public enum GesturePhase { Began, Changed, Ended, Cancelled }
public enum GestureKind { Tap, DoubleTap, VerticalDrag, HorizontalDrag, Pinch, Pan }
public enum FileOperationStatus { Success, Cancelled, Denied, Failed }

[Flags] public enum ExplorerInfoFields { None = 0, Duration = 1, Resolution = 2, Size = 4, Date = 8, Path = 16 }

public sealed record MediaItem(
    MediaFileId Id, string ContentUri, string DisplayName, MediaKind Kind,
    long BucketId, string FolderName, string RelativePath,   // text for display only, never used to open files
    long DateAddedSeconds, string MimeType);

public sealed record MediaFolder(long BucketId, string Name, string RelativePath, int ItemCount, int NewCount);

public sealed record MediaMetadata(MediaFileId Id, long DurationMs, int Width, int Height, string? ThumbnailPath);

public sealed record MediaQuery(long? BucketId, MediaKind? Kind);

public sealed record TrackInfo(int Id, string Name, string? Language);

public sealed record SubtitleFile(string ContentUri, string DisplayName, string? Language);   // a subtitle file next to a media file

public sealed record SubtitleStyle(
    double FontScale, uint TextColorArgb, uint OutlineColorArgb, int OutlineThickness,
    double BackgroundOpacity, bool Bold, double BottomMarginFraction)
{
    public static SubtitleStyle Default { get; } = new(1.0, 0xFFFFFFFF, 0xFF000000, 2, 0.0, false, 0.05);
}

// Per file. A null value means "not set for this file": the global default applies.
public sealed record PlaybackState(MediaFileId Id)
{
    public long PositionMs { get; init; }
    public long DurationMs { get; init; }
    public bool Finished { get; init; }                 // position > 95% of duration
    public long LastPlayedUtc { get; init; }
    public float? Speed { get; init; }
    public DecoderMode? Decoder { get; init; }
    public AspectMode? Aspect { get; init; }
    public int? AudioTrackId { get; init; }
    public long? AudioDelayMs { get; init; }
    public int? SubtitleTrackId { get; init; }          // -1 = subtitles off
    public long? SubtitleDelayMs { get; init; }
    public string? ExternalSubtitleUri { get; init; }
}

public sealed record GlobalDefaults
{
    public float Speed { get; init; } = 1.0f;                       // 0.25 .. 4.0
    public DecoderMode Decoder { get; init; } = DecoderMode.Hardware;
    public AspectMode Aspect { get; init; } = AspectMode.Fit;
    public RepeatMode Repeat { get; init; } = RepeatMode.None;
    public bool ResumeFromLastPosition { get; init; } = true;
    public bool SubtitlesEnabled { get; init; } = true;
    public string? PreferredAudioLanguage { get; init; }
    public string? PreferredSubtitleLanguage { get; init; }
    public long AudioDelayMs { get; init; }
    public long SubtitleDelayMs { get; init; }
    public SubtitleStyle SubtitleStyle { get; init; } = SubtitleStyle.Default;
    public bool BackgroundAudio { get; init; }                      // audio-only play in the background
    public bool PictureInPictureOnLeave { get; init; }
}

public sealed record EffectivePlaybackSettings(
    long StartPositionMs, float Speed, DecoderMode Decoder, AspectMode Aspect,
    int? AudioTrackId, long AudioDelayMs, int? SubtitleTrackId, long SubtitleDelayMs,
    string? ExternalSubtitleUri, SubtitleStyle SubtitleStyle);

public sealed record UiSettings
{
    public int HudAutoHideSeconds { get; init; } = 3;
    public int SkipSeconds { get; init; } = 10;                     // double-tap skip
    public bool ShowClock { get; init; } = true;
    public bool ShowBattery { get; init; } = true;
    public ExplorerInfoFields ExplorerFields { get; init; } =
        ExplorerInfoFields.Duration | ExplorerInfoFields.Resolution | ExplorerInfoFields.Size;
    public ExplorerViewMode ViewMode { get; init; } = ExplorerViewMode.Folders;
    public SortField SortField { get; init; } = SortField.Name;
    public SortDirection SortDirection { get; init; } = SortDirection.Ascending;
    public int NewTagDays { get; init; } = 7;
}

public sealed record ThemeSettings
{
    public ThemeId Theme { get; init; } = ThemeId.DarkOled;
    public uint AccentColorArgb { get; init; } = 0xFF4C9AFF;
    public double HudOpacity { get; init; } = 0.6;                  // 0 .. 1
}

public sealed record PlayerSnapshot(
    PlayerStatus Status, MediaFileId? Current, long PositionMs, long DurationMs, float Speed,
    DecoderMode RequestedDecoder, DecoderMode? ActualDecoder, bool HasVideo, bool VideoEnabled,
    int VolumePercent, string? Error)
{
    public static PlayerSnapshot Idle { get; }   // Status Idle, speed 1.0, Hardware requested, video enabled, volume 100
}

public sealed record VideoFormat(int Width, int Height, int SarNumerator, int SarDenominator);

public sealed record ViewTransform(double Scale, double PanX, double PanY)   // scale 1..4, pan as fraction of view size
{
    public static ViewTransform Identity { get; } = new(1, 0, 0);
}

public sealed record GestureEvent(
    GestureKind Kind, GesturePhase Phase, ScreenSide Side,
    double DeltaX, double DeltaY,          // fraction of view size since Began
    double Scale, double FocusX, double FocusY);

public sealed record AbRepeat(long StartMs, long EndMs);

public sealed record FileOperationResult(FileOperationStatus Status, MediaItem? Updated, string? Error);
```

## Contracts

```csharp
// --- Engine (player-engine). Wraps LibVLC. Serialized command queue; see ARCHITECTURE.md section 3.
public interface IPlayerEngine
{
    PlayerSnapshot Snapshot { get; }
    event EventHandler<PlayerSnapshot>? SnapshotChanged;     // status, decoder, tracks, error, position (max 4 per second)
    IReadOnlyList<TrackInfo> AudioTracks { get; }
    IReadOnlyList<TrackInfo> SubtitleTracks { get; }
    event EventHandler? TracksChanged;                       // a track list or a selected track changed
    int? CurrentAudioTrackId { get; }                        // null while not known
    int? CurrentSubtitleTrackId { get; }                     // -1 = off, null while not known
    long AudioDelayMs { get; }
    long SubtitleDelayMs { get; }
    SubtitleStyle SubtitleStyle { get; }

    Task OpenAsync(MediaItem item, EffectivePlaybackSettings settings, bool startPaused, CancellationToken ct);
    Task PlayAsync(CancellationToken ct);
    Task PauseAsync(CancellationToken ct);
    Task StopAsync(CancellationToken ct);
    Task SeekAsync(long positionMs, CancellationToken ct);
    Task SetSpeedAsync(float speed, CancellationToken ct);              // 0.25 .. 4.0, pitch corrected
    Task SetVolumeAsync(int percent, CancellationToken ct);             // 0 .. 200
    Task SetDecoderAsync(DecoderMode mode, CancellationToken ct);       // restarts at the current position
    Task SetVideoEnabledAsync(bool enabled, CancellationToken ct);      // false = audio-only
    Task SelectAudioTrackAsync(int trackId, CancellationToken ct);
    Task SetAudioDelayAsync(long delayMs, CancellationToken ct);
    Task SelectSubtitleTrackAsync(int trackId, CancellationToken ct);   // -1 = off
    Task SetSubtitleDelayAsync(long delayMs, CancellationToken ct);
    Task AddSubtitleFileAsync(string localFilePath, CancellationToken ct);   // file inside app storage
    Task SetSubtitleStyleAsync(SubtitleStyle style, CancellationToken ct);   // restarts at the current position
}

// --- Engine <-> surface boundary. Implemented by player-engine, called by video-surface on the UI thread.
public interface IVideoOutputBinding
{
    VideoFormat? Format { get; }
    event EventHandler<VideoFormat?>? FormatChanged;
    void SurfaceReady(object nativeSurface, int widthPx, int heightPx);   // Android.Views.Surface
    void SurfaceSizeChanged(int widthPx, int heightPx);
    void SurfaceLost();
}

// --- Surface (video-surface). Aspect, zoom/pan, window brightness. No LibVLC knowledge.
public interface IVideoSurface
{
    AspectMode Aspect { get; }
    ViewTransform Transform { get; }
    event EventHandler? Changed;
    void SetAspect(AspectMode mode);
    void SetTransform(ViewTransform transform);        // clamped to 100% .. 400%, pan kept inside the picture
    void SetWindowBrightness(double? value);           // 0 .. 1, null = system default
    double WindowBrightness { get; }                   // 0 .. 1, or -1 while the system default is in effect
}

// --- Gestures (gestures). Raw native touch turned into semantic events. No side effects.
public interface IGestureSource
{
    bool Locked { get; set; }                          // screen lock: only the unlock tap is reported
    event EventHandler<GestureEvent>? Gesture;
}

// --- Coordinator (playback-coordinator). The only caller of IPlayerEngine.OpenAsync.
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
    Task SetAbRepeatAsync(AbRepeat? range, CancellationToken ct);
    Task CloseAsync(CancellationToken ct);
}

// --- Library (media-library)
public interface IMediaLibrary
{
    event EventHandler? Changed;                                        // MediaStore content changed
    Task<bool> EnsureReadPermissionAsync(CancellationToken ct);
    Task<IReadOnlyList<MediaFolder>> GetFoldersAsync(int newTagDays, CancellationToken ct);
    Task<IReadOnlyList<MediaItem>> GetItemsAsync(MediaQuery query, CancellationToken ct);
}

// --- Metadata and thumbnails (thumbnail-cache)
public interface IMediaMetadataService
{
    Task<IReadOnlyDictionary<MediaFileId, MediaMetadata>> GetCachedAsync(IReadOnlyCollection<MediaFileId> ids, CancellationToken ct);
    Task<MediaMetadata?> GetOrCreateAsync(MediaItem item, CancellationToken ct);   // lazy, bounded concurrency
    Task PruneAsync(IReadOnlyCollection<MediaFileId> existing, CancellationToken ct);
}

// --- File operations (file-ops). Handles the system consent dialog itself.
public interface IFileOperations
{
    Task<FileOperationResult> RenameAsync(MediaItem item, string newDisplayName, CancellationToken ct);
    Task<FileOperationResult> TrashAsync(IReadOnlyList<MediaItem> items, CancellationToken ct);
}

// --- Per-file state (playback-state-store)
public interface IPlaybackStateStore
{
    Task<PlaybackState?> GetAsync(MediaFileId id, CancellationToken ct);
    Task<IReadOnlyDictionary<MediaFileId, PlaybackState>> GetManyAsync(IReadOnlyCollection<MediaFileId> ids, CancellationToken ct);
    Task SaveAsync(PlaybackState state, CancellationToken ct);
    Task DeleteAsync(MediaFileId id, CancellationToken ct);
}

public interface IPlaybackStateRecorder
{
    void Update(Func<PlaybackState, PlaybackState> change);   // per-file overrides chosen by the user
    Task FlushAsync(CancellationToken ct);                    // immediate save
}

// --- Global settings stores. Separate from per-file state.
public interface IGlobalDefaultsStore
{
    event EventHandler<GlobalDefaults>? Changed;
    Task<GlobalDefaults> GetAsync(CancellationToken ct);
    Task SaveAsync(GlobalDefaults value, CancellationToken ct);
}

public interface IUiSettingsStore
{
    event EventHandler<UiSettings>? Changed;
    Task<UiSettings> GetAsync(CancellationToken ct);
    Task SaveAsync(UiSettings value, CancellationToken ct);
}

// --- Theming (theming-engine)
public interface IThemeService
{
    ThemeSettings Current { get; }
    event EventHandler<ThemeSettings>? Changed;
    Task InitializeAsync(CancellationToken ct);                 // load and apply the saved theme
    Task ApplyAsync(ThemeSettings settings, CancellationToken ct);   // swaps resources at runtime and saves
}

// --- Subtitles and audio tracks (subtitles-audio). Policy on top of the engine primitives.
public interface ISubtitleService
{
    Task<IReadOnlyList<SubtitleFile>> FindSidecarsAsync(MediaItem item, CancellationToken ct);   // best match first
    Task<bool> LoadExternalAsync(string contentUri, CancellationToken ct);
    Task<bool> PickAndLoadAsync(CancellationToken ct);                                      // system file picker
    Task ApplyPreferredTracksAsync(GlobalDefaults defaults, CancellationToken ct);
}

// --- Background (media-service, media-session-bt, pip)
public interface IPlaybackServiceController
{
    bool IsRunning { get; }
    Task StartAsync(CancellationToken ct);      // foreground service, audio focus, becoming-noisy
    Task StopAsync(CancellationToken ct);
}

public interface IMediaSessionBridge
{
    object? SessionToken { get; }               // Android MediaSession.Token, for the service notification
    Task ActivateAsync(CancellationToken ct);
    Task DeactivateAsync(CancellationToken ct);
}

public interface IPictureInPictureController
{
    bool IsSupported { get; }
    bool IsActive { get; }
    event EventHandler? Changed;
    Task<bool> EnterAsync(CancellationToken ct);
}

// --- App lifecycle (app-skeleton). Raised from MainActivity.
public interface IAppLifecycle
{
    bool IsForeground { get; }
    event EventHandler? Foregrounded;
    event EventHandler? Backgrounded;
    event EventHandler? Stopping;               // last chance to flush state
}

// --- Database plumbing (app-skeleton runs them; each store ships its own)
public interface IDbMigration
{
    int Order { get; }
    Task ApplyAsync(IDbConnectionProvider db, CancellationToken ct);
}

public interface IDbConnectionProvider
{
    Task<object> GetConnectionAsync(CancellationToken ct);   // SQLiteAsyncConnection; typed as object to keep Core free of SQLite
}
```

## Pure logic in Core owned by core-contracts

```csharp
public static class PlaybackSettingsResolver
{
    // Per-file value if set, otherwise the global default. Start position = state position
    // when defaults.ResumeFromLastPosition and the file is not Finished, otherwise 0.
    // SubtitleTrackId with no per-file value: null (engine/preferred track) when
    // defaults.SubtitlesEnabled, otherwise -1 (off). SubtitleStyle always comes from the defaults.
    public static EffectivePlaybackSettings Resolve(PlaybackState? state, GlobalDefaults defaults);
}
```
