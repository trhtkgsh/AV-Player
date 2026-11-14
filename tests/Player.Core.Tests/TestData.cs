using Player.Core.Models;

namespace Player.Core.Tests;

/// <summary>Factories for test models with fixed, sensible defaults. Nothing here reads a clock.</summary>
internal static class TestData
{
    public static MediaFileId Id(long mediaStoreId = 1, long sizeBytes = 1_000, long dateModifiedSeconds = 1) =>
        new(mediaStoreId, sizeBytes, dateModifiedSeconds);

    public static MediaItem Item(
        long mediaStoreId = 1,
        string name = "video.mp4",
        long sizeBytes = 1_000,
        long dateAddedSeconds = 1_000_000,
        long bucketId = 10,
        string folderName = "Movies")
    {
        return new MediaItem(
            Id: Id(mediaStoreId, sizeBytes),
            ContentUri: $"content://media/external/video/media/{mediaStoreId}",
            DisplayName: name,
            Kind: MediaKind.Video,
            BucketId: bucketId,
            FolderName: folderName,
            RelativePath: $"{folderName}/",
            DateAddedSeconds: dateAddedSeconds,
            MimeType: "video/mp4");
    }

    public static MediaMetadata Metadata(MediaFileId id, long durationMs) =>
        new(id, durationMs, 1920, 1080, ThumbnailPath: null);
}
