using Player.Core.Models;

namespace Player.Core.Logic.Library;

/// <summary>An item is NEW when it was added within the last N days and has no playback state.</summary>
public static class NewTagRule
{
    private const long SecondsPerDay = 86_400;

    public static bool IsNew(MediaItem item, PlaybackState? state, int newTagDays, long nowUnixSeconds)
    {
        ArgumentNullException.ThrowIfNull(item);

        if (state is not null || newTagDays <= 0)
        {
            return false;
        }

        // Items dated in the future (clock skew) count as recent.
        return nowUnixSeconds - item.DateAddedSeconds <= newTagDays * SecondsPerDay;
    }
}
