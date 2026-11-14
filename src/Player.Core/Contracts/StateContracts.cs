using Player.Core.Models;

namespace Player.Core.Contracts;

/// <summary>Per-file playback state. Never reads or writes the global defaults.</summary>
public interface IPlaybackStateStore
{
    Task<PlaybackState?> GetAsync(MediaFileId id, CancellationToken ct);

    Task<IReadOnlyDictionary<MediaFileId, PlaybackState>> GetManyAsync(
        IReadOnlyCollection<MediaFileId> ids, CancellationToken ct);

    Task SaveAsync(PlaybackState state, CancellationToken ct);
    Task DeleteAsync(MediaFileId id, CancellationToken ct);
}

/// <summary>
/// Follows the engine and saves the current file's state on a 5 s debounce and on
/// pause, stop, end, item change and app lifecycle events.
/// </summary>
public interface IPlaybackStateRecorder
{
    /// <summary>Applies a per-file override chosen by the user and saves immediately.</summary>
    void Update(Func<PlaybackState, PlaybackState> change);

    /// <summary>Saves now. Completes when the write finished.</summary>
    Task FlushAsync(CancellationToken ct);
}

/// <summary>Global playback defaults. Never reads or writes per-file state.</summary>
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

public interface IThemeService
{
    ThemeSettings Current { get; }
    event EventHandler<ThemeSettings>? Changed;

    /// <summary>Loads and applies the saved theme. Called before the first page is shown.</summary>
    Task InitializeAsync(CancellationToken ct);

    /// <summary>Swaps the resources at runtime and saves the choice.</summary>
    Task ApplyAsync(ThemeSettings settings, CancellationToken ct);
}
