namespace Player.Core.Logic.Subtitles;

/// <summary>A subtitle file next to a video. <paramref name="Language"/> is the language suffix, or null.</summary>
public sealed record SidecarMatch(string FileName, string? Language);

/// <summary>
/// Finds and ranks subtitle files (.srt, .ass, .ssa) that belong to a video by file name.
/// </summary>
public static class SidecarMatcher
{
    private static readonly string[] Extensions = { ".srt", ".ass", ".ssa" };

    // Two or three letters would otherwise read as a language code.
    private static readonly string[] NotLanguages = { "cc", "sdh" };

    private sealed record Candidate(SidecarMatch Match, int Group, int PartCount, int ExtensionRank);

    /// <summary>Returns the matching candidates, best first: preferred language, then no suffix, then the rest.</summary>
    public static IReadOnlyList<SidecarMatch> Match(
        string videoDisplayName,
        IEnumerable<string> candidateFileNames,
        string? preferredLanguage)
    {
        var result = new List<SidecarMatch>();
        if (string.IsNullOrEmpty(videoDisplayName) || candidateFileNames is null)
        {
            return result;
        }

        var dot = videoDisplayName.LastIndexOf('.');
        var baseName = dot > 0 ? videoDisplayName.Substring(0, dot) : videoDisplayName;
        var hasPreferred = !string.IsNullOrWhiteSpace(preferredLanguage);

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var found = new List<Candidate>();
        foreach (var name in candidateFileNames)
        {
            if (string.IsNullOrEmpty(name)
                || string.Equals(name, videoDisplayName, StringComparison.OrdinalIgnoreCase)
                || !TryParse(name, baseName, out var parts, out var extensionRank)
                || !seen.Add(name))
            {
                continue;
            }

            var language = parts.Length > 0 ? LanguageOf(parts[0]) : null;
            var group = hasPreferred && language is not null && LanguageCodes.Matches(language, preferredLanguage)
                ? 0
                : parts.Length == 0 ? 1 : 2;
            found.Add(new Candidate(new SidecarMatch(name, language), group, parts.Length, extensionRank));
        }

        found.Sort(static (x, y) =>
        {
            var c = x.Group.CompareTo(y.Group);
            if (c == 0)
            {
                c = x.PartCount.CompareTo(y.PartCount);
            }

            if (c == 0)
            {
                c = x.ExtensionRank.CompareTo(y.ExtensionRank);
            }

            return c != 0 ? c : StringComparer.OrdinalIgnoreCase.Compare(x.Match.FileName, y.Match.FileName);
        });

        foreach (var item in found)
        {
            result.Add(item.Match);
        }

        return result;
    }

    private static bool TryParse(string name, string baseName, out string[] parts, out int extensionRank)
    {
        parts = Array.Empty<string>();
        extensionRank = -1;

        var dot = name.LastIndexOf('.');
        if (dot < 0)
        {
            return false;
        }

        var extension = name.Substring(dot);
        for (var i = 0; i < Extensions.Length; i++)
        {
            if (string.Equals(extension, Extensions[i], StringComparison.OrdinalIgnoreCase))
            {
                extensionRank = i;
            }
        }

        if (extensionRank < 0)
        {
            return false;
        }

        var rest = name.Substring(0, dot);
        if (string.Equals(rest, baseName, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (rest.Length <= baseName.Length + 1
            || !rest.StartsWith(baseName, StringComparison.OrdinalIgnoreCase)
            || rest[baseName.Length] != '.')
        {
            return false;
        }

        parts = rest.Substring(baseName.Length + 1).Split('.');
        return Array.TrueForAll(parts, static p => p.Length > 0);
    }

    private static string? LanguageOf(string part)
    {
        if (LanguageCodes.IsKnown(part))
        {
            return part;
        }

        var looksLikeCode = (part.Length == 2 || part.Length == 3) && part.All(char.IsLetter);
        return looksLikeCode && Array.IndexOf(NotLanguages, part.ToLowerInvariant()) < 0 ? part : null;
    }
}
