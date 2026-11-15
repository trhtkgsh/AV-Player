namespace Player.Core.Models;

using System.Globalization;

/// <summary>
/// Identity of a media file. Never derived from a path: a file that is moved keeps its identity,
/// a file that is replaced (new size or modified date) gets a new one.
/// </summary>
public readonly record struct MediaFileId(long MediaStoreId, long SizeBytes, long DateModifiedSeconds)
{
    /// <summary>The database key. Culture-invariant, so the stores can parse it back.</summary>
    public string ToKey() =>
        string.Create(CultureInfo.InvariantCulture, $"{MediaStoreId}:{SizeBytes}:{DateModifiedSeconds}");
}

/// <param name="ContentUri">The only handle used to open the file.</param>
/// <param name="FolderName">Display text only.</param>
/// <param name="RelativePath">Display text only. Never used to open or identify the file.</param>
/// <param name="DateAddedSeconds">Unix seconds.</param>
public sealed record MediaItem(
    MediaFileId Id,
    string ContentUri,
    string DisplayName,
    MediaKind Kind,
    long BucketId,
    string FolderName,
    string RelativePath,
    long DateAddedSeconds,
    string MimeType);

public sealed record MediaFolder(long BucketId, string Name, string RelativePath, int ItemCount, int NewCount);

/// <param name="ThumbnailPath">JPEG file in the app cache directory, or null when none could be produced.</param>
public sealed record MediaMetadata(MediaFileId Id, long DurationMs, int Width, int Height, string? ThumbnailPath);

/// <summary>A null member means "no filter".</summary>
public sealed record MediaQuery(long? BucketId, MediaKind? Kind);

public sealed record TrackInfo(int Id, string Name, string? Language);

public sealed record FileOperationResult(FileOperationStatus Status, MediaItem? Updated, string? Error);
