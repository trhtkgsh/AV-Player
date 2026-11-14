using System.Globalization;
using Player.Core.Models;

namespace Player.Core.Logic.Explorer;

/// <summary>Formats the text shown on an explorer row. All output is culture invariant.</summary>
public static class MediaInfoFormatter
{
    private const string Separator = " · ";
    private const long Kilo = 1024;
    private const long Mega = Kilo * 1024;
    private const long Giga = Mega * 1024;

    /// <summary>"m:ss" under one hour, otherwise "h:mm:ss". Empty for zero or negative durations.</summary>
    public static string Duration(long milliseconds)
    {
        if (milliseconds <= 0)
        {
            return string.Empty;
        }

        var totalSeconds = milliseconds / 1000;
        var seconds = totalSeconds % 60;

        if (totalSeconds < 3600)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0}:{1:00}", totalSeconds / 60, seconds);
        }

        var minutes = totalSeconds % 3600 / 60;
        return string.Format(CultureInfo.InvariantCulture, "{0}:{1:00}:{2:00}", totalSeconds / 3600, minutes, seconds);
    }

    /// <summary>
    /// Bytes, KB and MB with one decimal, GB with two. Empty for negative sizes.
    /// The value is rounded before the unit is chosen, so 1_048_575 bytes is "1.0 MB", not "1024.0 KB".
    /// </summary>
    public static string Size(long bytes)
    {
        if (bytes < 0)
        {
            return string.Empty;
        }

        if (bytes < Kilo)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0} B", bytes);
        }

        var kilo = Math.Round(bytes / (double)Kilo, 1, MidpointRounding.AwayFromZero);
        if (kilo < 1024)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0:0.0} KB", kilo);
        }

        var mega = Math.Round(bytes / (double)Mega, 1, MidpointRounding.AwayFromZero);
        if (mega < 1024)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0:0.0} MB", mega);
        }

        var giga = Math.Round(bytes / (double)Giga, 2, MidpointRounding.AwayFromZero);
        return string.Format(CultureInfo.InvariantCulture, "{0:0.00} GB", giga);
    }

    /// <summary>"1920x1080". Empty when the width or the height is not positive.</summary>
    public static string Resolution(int width, int height)
    {
        if (width <= 0 || height <= 0)
        {
            return string.Empty;
        }

        return string.Format(CultureInfo.InvariantCulture, "{0}x{1}", width, height);
    }

    /// <summary>The instant converted to <paramref name="zone"/>, formatted as "yyyy-MM-dd".</summary>
    public static string Date(long unixSeconds, TimeZoneInfo zone)
    {
        ArgumentNullException.ThrowIfNull(zone);

        var local = TimeZoneInfo.ConvertTime(DateTimeOffset.FromUnixTimeSeconds(unixSeconds), zone);
        return local.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// The enabled parts in the fixed order duration, resolution, size, date, path, joined with " · ".
    /// Parts that are empty are skipped. Duration and resolution need metadata and are skipped when it is null.
    /// </summary>
    public static string InfoLine(MediaItem item, MediaMetadata? metadata, ExplorerInfoFields fields, TimeZoneInfo zone)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(zone);

        var parts = new List<string>(5);

        if (metadata is not null)
        {
            if (Enabled(fields, ExplorerInfoFields.Duration))
            {
                AddIfNotEmpty(parts, Duration(metadata.DurationMs));
            }

            if (Enabled(fields, ExplorerInfoFields.Resolution))
            {
                AddIfNotEmpty(parts, Resolution(metadata.Width, metadata.Height));
            }
        }

        if (Enabled(fields, ExplorerInfoFields.Size))
        {
            AddIfNotEmpty(parts, Size(item.Id.SizeBytes));
        }

        if (Enabled(fields, ExplorerInfoFields.Date))
        {
            AddIfNotEmpty(parts, Date(item.DateAddedSeconds, zone));
        }

        if (Enabled(fields, ExplorerInfoFields.Path))
        {
            AddIfNotEmpty(parts, item.RelativePath);
        }

        return string.Join(Separator, parts);
    }

    private static bool Enabled(ExplorerInfoFields fields, ExplorerInfoFields field) =>
        (fields & field) != 0;

    private static void AddIfNotEmpty(List<string> parts, string text)
    {
        if (text.Length > 0)
        {
            parts.Add(text);
        }
    }
}
