using Player.Core.Models;

namespace Player.Core.Logic.Explorer;

/// <summary>Playback progress and watched state for an explorer row.</summary>
public static class ProgressCalculator
{
    private const double WatchedThreshold = 0.95;

    /// <summary>Position over duration, clamped to 0 .. 1. Zero when there is no state or no duration.</summary>
    public static double Fraction(PlaybackState? state)
    {
        if (state is null || state.DurationMs <= 0)
        {
            return 0;
        }

        return Math.Clamp((double)state.PositionMs / state.DurationMs, 0, 1);
    }

    /// <summary>True when the file is marked finished or its position is above 95% of the duration.</summary>
    public static bool IsWatched(PlaybackState? state)
    {
        if (state is null)
        {
            return false;
        }

        return state.Finished || Fraction(state) > WatchedThreshold;
    }
}
