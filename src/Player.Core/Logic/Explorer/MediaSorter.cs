using Player.Core.Models;

namespace Player.Core.Logic.Explorer;

/// <summary>
/// Sorts media items by one field. Stable. Ties are always broken by display name (natural,
/// ascending) and then by MediaStore id (ascending), in both directions.
/// </summary>
public static class MediaSorter
{
    public static IReadOnlyList<MediaItem> Sort(
        IReadOnlyList<MediaItem> items,
        SortField field,
        SortDirection direction,
        IReadOnlyDictionary<MediaFileId, MediaMetadata> metadata)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(metadata);

        var descending = direction == SortDirection.Descending;

        IOrderedEnumerable<MediaItem> ordered = field switch
        {
            SortField.Name => descending
                ? items.OrderByDescending(item => item.DisplayName, NaturalNameComparer.Instance)
                : items.OrderBy(item => item.DisplayName, NaturalNameComparer.Instance),
            SortField.DateAdded => descending
                ? items.OrderByDescending(item => item.DateAddedSeconds)
                : items.OrderBy(item => item.DateAddedSeconds),
            SortField.Size => descending
                ? items.OrderByDescending(item => item.Id.SizeBytes)
                : items.OrderBy(item => item.Id.SizeBytes),
            SortField.Duration => OrderByDuration(items, descending, metadata),
            _ => throw new ArgumentOutOfRangeException(nameof(field), field, null),
        };

        return ordered
            .ThenBy(item => item.DisplayName, NaturalNameComparer.Instance)
            .ThenBy(item => item.Id.MediaStoreId)
            .ToList();
    }

    private static IOrderedEnumerable<MediaItem> OrderByDuration(
        IReadOnlyList<MediaItem> items,
        bool descending,
        IReadOnlyDictionary<MediaFileId, MediaMetadata> metadata)
    {
        // Items without a known duration form a group after all others, in both directions.
        var byKnown = items.OrderBy(item => HasDuration(item, metadata) ? 0 : 1);

        return descending
            ? byKnown.ThenByDescending(item => DurationOf(item, metadata))
            : byKnown.ThenBy(item => DurationOf(item, metadata));
    }

    private static bool HasDuration(MediaItem item, IReadOnlyDictionary<MediaFileId, MediaMetadata> metadata) =>
        metadata.TryGetValue(item.Id, out var entry) && entry.DurationMs > 0;

    private static long DurationOf(MediaItem item, IReadOnlyDictionary<MediaFileId, MediaMetadata> metadata) =>
        HasDuration(item, metadata) ? metadata[item.Id].DurationMs : 0;
}
