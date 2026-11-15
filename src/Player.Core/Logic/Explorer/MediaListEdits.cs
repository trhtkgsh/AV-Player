using Player.Core.Models;

namespace Player.Core.Logic.Explorer;

/// <summary>Replaces or removes one file in a list. The order of the other items is kept.</summary>
public static class MediaListEdits
{
    /// <summary>Puts <paramref name="replacement"/> where the item with <paramref name="id"/> was. Unchanged when it is not there.</summary>
    public static IReadOnlyList<MediaItem> Replace(IReadOnlyList<MediaItem> items, MediaFileId id, MediaItem replacement)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(replacement);

        return items.Select(item => item.Id == id ? replacement : item).ToList();
    }

    /// <summary>Removes the item with <paramref name="id"/>. Unchanged when it is not there.</summary>
    public static IReadOnlyList<MediaItem> Remove(IReadOnlyList<MediaItem> items, MediaFileId id)
    {
        ArgumentNullException.ThrowIfNull(items);

        return items.Where(item => item.Id != id).ToList();
    }
}
