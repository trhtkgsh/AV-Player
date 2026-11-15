using Player.Core.Models;

namespace Player.Core.Contracts;

public interface IMediaLibrary
{
    /// <summary>The device's media collection changed.</summary>
    event EventHandler? Changed;

    Task<bool> EnsureReadPermissionAsync(CancellationToken ct);
    Task<IReadOnlyList<MediaFolder>> GetFoldersAsync(int newTagDays, CancellationToken ct);
    Task<IReadOnlyList<MediaItem>> GetItemsAsync(MediaQuery query, CancellationToken ct);
}

public interface IMediaMetadataService
{
    /// <summary>Cached rows only. Never decodes.</summary>
    Task<IReadOnlyDictionary<MediaFileId, MediaMetadata>> GetCachedAsync(
        IReadOnlyCollection<MediaFileId> ids, CancellationToken ct);

    /// <summary>Returns the cached row or extracts it, with bounded concurrency. Null when extraction failed.</summary>
    Task<MediaMetadata?> GetOrCreateAsync(MediaItem item, CancellationToken ct);

    /// <summary>Removes cached rows and thumbnails for files that are not in <paramref name="existing"/>.</summary>
    Task PruneAsync(IReadOnlyCollection<MediaFileId> existing, CancellationToken ct);
}

/// <summary>File changes through the system media store. Shows the system consent dialog when one is required.</summary>
public interface IFileOperations
{
    Task<FileOperationResult> RenameAsync(MediaItem item, string newDisplayName, CancellationToken ct);
    Task<FileOperationResult> TrashAsync(IReadOnlyList<MediaItem> items, CancellationToken ct);
}

/// <summary>Subtitle loading and the audio/subtitle track policy on top of the engine primitives.</summary>
public interface ISubtitleService
{
    /// <summary>Subtitle files that belong to the item, best match first.</summary>
    Task<IReadOnlyList<SubtitleFile>> FindSidecarsAsync(MediaItem item, CancellationToken ct);

    /// <param name="contentUri">A content URI, for example <see cref="SubtitleFile.ContentUri"/>.</param>
    Task<bool> LoadExternalAsync(string contentUri, CancellationToken ct);

    /// <summary>Opens the system file picker. False when the user cancelled.</summary>
    Task<bool> PickAndLoadAsync(CancellationToken ct);

    Task ApplyPreferredTracksAsync(GlobalDefaults defaults, CancellationToken ct);
}
