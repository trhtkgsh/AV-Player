using Player.Core.Models;

namespace Player.Core.Logic.Library;

/// <summary>Groups items by MediaStore bucket and counts items and NEW items per folder.</summary>
public static class FolderGrouper
{
    public static IReadOnlyList<MediaFolder> Group(
        IReadOnlyList<MediaItem> items,
        IReadOnlyDictionary<MediaFileId, PlaybackState> states,
        int newTagDays,
        long nowUnixSeconds)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(states);

        var buckets = new Dictionary<long, (string Name, string RelativePath, int Count, int NewCount, bool HasVideo)>();
        foreach (var item in items)
        {
            states.TryGetValue(item.Id, out var state);
            var isNew = NewTagRule.IsNew(item, state, newTagDays, nowUnixSeconds) ? 1 : 0;
            var isVideo = item.Kind == MediaKind.Video;

            buckets[item.BucketId] = buckets.TryGetValue(item.BucketId, out var current)
                ? (current.Name, current.RelativePath, current.Count + 1, current.NewCount + isNew, current.HasVideo || isVideo)
                : (item.FolderName, item.RelativePath, 1, isNew, isVideo);
        }

        return buckets
            .Select(b => new MediaFolder(b.Key, b.Value.Name, b.Value.RelativePath, b.Value.Count, b.Value.NewCount)
            {
                AudioOnly = !b.Value.HasVideo,
            })
            .OrderBy(f => f.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(f => f.BucketId)
            .ToList();
    }
}
