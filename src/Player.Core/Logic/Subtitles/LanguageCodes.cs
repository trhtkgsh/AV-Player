namespace Player.Core.Logic.Subtitles;

/// <summary>
/// Language code comparison across ISO 639-1, ISO 639-2 (both forms), the old code "iw" and English names.
/// </summary>
public static class LanguageCodes
{
    // First entry is the ISO 639-1 code, the rest are alternative codes and the English name.
    private static readonly string[][] Table =
    {
        new[] { "he", "heb", "iw", "hebrew" },
        new[] { "en", "eng", "english" },
        new[] { "ar", "ara", "arabic" },
        new[] { "ru", "rus", "russian" },
        new[] { "fr", "fre", "fra", "french" },
        new[] { "de", "ger", "deu", "german" },
        new[] { "es", "spa", "spanish" },
        new[] { "it", "ita", "italian" },
        new[] { "pt", "por", "portuguese" },
        new[] { "nl", "dut", "nld", "dutch" },
        new[] { "pl", "pol", "polish" },
        new[] { "tr", "tur", "turkish" },
        new[] { "el", "gre", "ell", "greek" },
        new[] { "uk", "ukr", "ukrainian" },
        new[] { "ro", "rum", "ron", "romanian" },
        new[] { "hu", "hun", "hungarian" },
        new[] { "cs", "cze", "ces", "czech" },
        new[] { "sv", "swe", "swedish" },
        new[] { "no", "nor", "nob", "nno", "nb", "nn", "norwegian" },
        new[] { "da", "dan", "danish" },
        new[] { "fi", "fin", "finnish" },
        new[] { "ja", "jpn", "japanese" },
        new[] { "ko", "kor", "korean" },
        new[] { "zh", "chi", "zho", "chinese" },
        new[] { "hi", "hin", "hindi" },
        new[] { "fa", "per", "fas", "persian" },
        new[] { "yi", "yid", "yiddish" },
        new[] { "am", "amh", "amharic" },
    };

    private static readonly Dictionary<string, string> Known = BuildKnown();

    /// <summary>True when both texts name the same language; unknown codes match only an equal unknown code.</summary>
    public static bool Matches(string? a, string? b)
    {
        var na = Normalize(a);
        var nb = Normalize(b);
        return na is not null && nb is not null && string.Equals(na, nb, StringComparison.Ordinal);
    }

    /// <summary>The ISO 639-1 code of a known language, the lower-case primary part of an unknown one, or null.</summary>
    public static string? Normalize(string? code)
    {
        var primary = Primary(code);
        if (primary.Length == 0)
        {
            return null;
        }

        return Known.TryGetValue(primary, out var iso1) ? iso1 : primary;
    }

    internal static bool IsKnown(string? code)
    {
        var primary = Primary(code);
        return primary.Length > 0 && Known.ContainsKey(primary);
    }

    private static string Primary(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return string.Empty;
        }

        var text = code.Trim();
        var cut = text.IndexOfAny(new[] { '-', '_' });
        if (cut >= 0)
        {
            text = text.Substring(0, cut).Trim();
        }

        return text.ToLowerInvariant();
    }

    private static Dictionary<string, string> BuildKnown()
    {
        var map = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var row in Table)
        {
            foreach (var key in row)
            {
                map[key] = row[0];
            }
        }

        return map;
    }
}
