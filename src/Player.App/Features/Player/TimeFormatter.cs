namespace Player.App.Features.PlayerHud;

/// <summary>Formats playback times for the HUD. Pure and static.</summary>
public static class TimeFormatter
{
    /// <summary>
    /// Under one hour: <c>m:ss</c>. One hour or more: <c>h:mm:ss</c>.
    /// Negative values are treated as 0.
    /// </summary>
    public static string Format(long milliseconds)
    {
        var totalSeconds = Math.Max(0, milliseconds) / 1000;
        var hours = totalSeconds / 3600;
        var minutes = totalSeconds % 3600 / 60;
        var seconds = totalSeconds % 60;

        return hours > 0
            ? FormattableString.Invariant($"{hours}:{minutes:00}:{seconds:00}")
            : FormattableString.Invariant($"{minutes}:{seconds:00}");
    }
}
