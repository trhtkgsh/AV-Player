using Player.Core.Models;

namespace Player.Core.Logic.Explorer;

/// <summary>Instant name filter for the explorer. Case-insensitive contains on the display name.</summary>
public static class SearchFilter
{
    public static IReadOnlyList<MediaItem> Apply(IReadOnlyList<MediaItem> items, string? text)
    {
        ArgumentNullException.ThrowIfNull(items);

        if (string.IsNullOrWhiteSpace(text))
        {
            return items;
        }

        var needle = text.Trim();

        return items
            .Where(item => item.DisplayName.Contains(needle, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }
}
