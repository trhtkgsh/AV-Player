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

public sealed record MediaFolder(long BucketId, string Name, string RelativePath, int ItemCount, int NewCount)
{
    /// <summary>True when the folder holds no video file.</summary>
    public bool AudioOnly { get; init; }
}

/// <summary>Per folder. A row exists only while it has a speed or the favorite mark.</summary>
public sealed record FolderSettings(long BucketId)
{
    /// <summary>Default speed of the files in this folder; null means the global default.</summary>
    public float? Speed { get; init; }

    public bool Favorite { get; init; }
}

/// <summary>Space taken by the system bars at the edges of the window, in device-independent units.</summary>
public readonly record struct EdgeInsets(double Left, double Top, double Right, double Bottom);

/// <param name="ThumbnailPath">JPEG file in the app cache directory, or null when none could be produced.</param>
public sealed record MediaMetadata(MediaFileId Id, long DurationMs, int Width, int Height, string? ThumbnailPath);

/// <summary>A null member means "no filter".</summary>
public sealed record MediaQuery(long? BucketId, MediaKind? Kind);

public sealed record TrackInfo(int Id, string Name, string? Language);

/// <summary>A subtitle file found next to a media file.</summary>
/// <param name="DisplayName">The file name, for lists.</param>
/// <param name="Language">The language suffix of the file name, or null.</param>
public sealed record SubtitleFile(string ContentUri, string DisplayName, string? Language);

public sealed record FileOperationResult(FileOperationStatus Status, MediaItem? Updated, string? Error)
{
    /// <summary>The number of files that were changed, also when the status is not Success.</summary>
    public int CompletedCount { get; init; }
}
