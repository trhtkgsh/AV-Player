using Player.Core.Models;

namespace Player.Core.Logic.Overlay;

/// <summary>Builds the texts of the quiet playback display. Pure and static.</summary>
public static class PlaybackOverlayText
{
    private const string Separator = "  ";

    /// <summary>Under one hour: <c>m:ss</c>. One hour or more: <c>h:mm:ss</c>. Negative values count as 0.</summary>
    public static string FormatTime(long milliseconds)
    {
        var totalSeconds = Math.Max(0, milliseconds) / 1000;
        var hours = totalSeconds / 3600;
        var minutes = totalSeconds % 3600 / 60;
        var seconds = totalSeconds % 60;
        return hours > 0
            ? FormattableString.Invariant($"{hours}:{minutes:00}:{seconds:00}")
            : FormattableString.Invariant($"{minutes}:{seconds:00}");
    }

    /// <summary>
    /// The time that is left, with a minus sign: <c>-12:30</c>. Empty when the duration is not known.
    /// </summary>
    public static string FormatRemaining(long positionMs, long durationMs) =>
        durationMs <= 0 ? "" : "-" + FormatTime(durationMs - Math.Clamp(positionMs, 0, durationMs));

    /// <summary>
    /// The playback times in the chosen combination: <c>12:30</c>, <c>12:30 / 45:00</c>, <c>-32:30</c>,
    /// <c>12:30 / -32:30</c>, <c>12:30 / 45:00 / -32:30</c>. Parts that need the duration are left out
    /// while it is not known. Empty for <see cref="OverlayTimeMode.None"/>.
    /// </summary>
    public static string FormatTimes(OverlayTimeMode mode, long positionMs, long durationMs)
    {
        var known = durationMs > 0;
        var position = known ? Math.Clamp(positionMs, 0, durationMs) : Math.Max(0, positionMs);
        var elapsed = FormatTime(position);
        var total = known ? FormatTime(durationMs) : "";
        var remaining = FormatRemaining(position, durationMs);
        return mode switch
        {
            OverlayTimeMode.Elapsed => elapsed,
            OverlayTimeMode.ElapsedTotal => Join(" / ", elapsed, total),
            OverlayTimeMode.Remaining => remaining,
            OverlayTimeMode.ElapsedRemaining => Join(" / ", elapsed, remaining),
            OverlayTimeMode.ElapsedTotalRemaining => Join(" / ", elapsed, total, remaining),
            _ => "",
        };
    }

    /// <summary>
    /// The one line of the display: the times, the clock and the battery level, in that order, each only
    /// when its switch is on and its value is known. Empty when nothing is to be shown.
    /// </summary>
    /// <param name="clock">The time of day as text, or null when not known.</param>
    /// <param name="batteryPercent">0 .. 100, or null when not known.</param>
    public static string FormatLine(
        PlaybackOverlaySettings settings, long positionMs, long durationMs, string? clock, int? batteryPercent)
    {
        ArgumentNullException.ThrowIfNull(settings);
        var battery = settings.Battery && batteryPercent is { } percent
            ? FormattableString.Invariant($"{Math.Clamp(percent, 0, 100)}%")
            : "";
        return Join(
            Separator,
            FormatTimes(settings.Time, positionMs, durationMs),
            settings.Clock ? clock ?? "" : "",
            battery);
    }

    /// <summary>
    /// The text of one corner: the items placed in that corner (times, clock, battery, in that order), each
    /// only when its switch is on and its value is known. Empty when the corner holds nothing.
    /// </summary>
    public static string FormatCorner(
        PlaybackOverlaySettings settings, OverlayCorner corner, long positionMs, long durationMs,
        string? clock, int? batteryPercent)
    {
        ArgumentNullException.ThrowIfNull(settings);
        var battery = settings.Battery && settings.BatteryCorner == corner && batteryPercent is { } percent
            ? FormattableString.Invariant($"{Math.Clamp(percent, 0, 100)}%")
            : "";
        return Join(
            Separator,
            settings.Corner == corner ? FormatTimes(settings.Time, positionMs, durationMs) : "",
            settings.Clock && settings.ClockCorner == corner ? clock ?? "" : "",
            battery);
    }

    /// <summary>The filled part of the progress line: 0 .. 1. 0 when the duration is not known.</summary>
    public static double Progress(long positionMs, long durationMs) =>
        durationMs <= 0 ? 0 : Math.Clamp((double)positionMs / durationMs, 0, 1);

    private static string Join(string separator, params string[] parts) =>
        string.Join(separator, parts.Where(part => part.Length > 0));
}
