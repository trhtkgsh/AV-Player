using System.Globalization;
using Player.Core.Contracts;
using Player.Core.Models;

namespace Player.Core.Logic.Localization;

/// <summary>
/// The merged texts of all string sources, with lookup, formatting and plural rules. Pure: no platform types.
/// </summary>
public sealed class StringCatalog
{
    /// <summary>The language code of English.</summary>
    public const string English = "en";

    /// <summary>The language code of Hebrew.</summary>
    public const string Hebrew = "he";

    private readonly Dictionary<string, LocalizedText> _texts = new(StringComparer.Ordinal);

    /// <summary>Merges the sources. The first definition of a key wins; later ones are listed in <see cref="DuplicateKeys"/>.</summary>
    public StringCatalog(IEnumerable<IStringSource> sources)
    {
        ArgumentNullException.ThrowIfNull(sources);

        var duplicates = new List<string>();
        foreach (var source in sources)
        {
            foreach (var (key, text) in source.Texts)
            {
                if (!_texts.TryAdd(key, text) && !duplicates.Contains(key))
                {
                    duplicates.Add(key);
                }
            }
        }

        DuplicateKeys = duplicates;
    }

    /// <summary>Keys defined by more than one source, in the order they were found.</summary>
    public IReadOnlyList<string> DuplicateKeys { get; }

    /// <summary>True when some source defines the key.</summary>
    public bool Contains(string key) => key is not null && _texts.ContainsKey(key);

    /// <summary>The text of a key, or the key in square brackets when it is unknown.</summary>
    public string Get(string key, string languageCode) =>
        TryGet(key, languageCode, out var text) ? text : $"[{key}]";

    /// <summary>The text of a key in a language. Any code other than "he" gives English.</summary>
    public bool TryGet(string key, string languageCode, out string text)
    {
        if (key is not null && _texts.TryGetValue(key, out var entry))
        {
            text = IsHebrew(languageCode) ? entry.Hebrew : entry.English;
            return true;
        }

        text = string.Empty;
        return false;
    }

    /// <summary>
    /// The text of a key with its {0}, {1}... placeholders filled in, formatted with the invariant culture.
    /// A text that does not fit the arguments is returned unformatted; this never throws.
    /// </summary>
    public string Format(string key, string languageCode, params object?[] args)
    {
        if (!TryGet(key, languageCode, out var text))
        {
            return $"[{key}]";
        }

        if (args is null || args.Length == 0)
        {
            return text;
        }

        try
        {
            return string.Format(CultureInfo.InvariantCulture, text, args);
        }
        catch (FormatException)
        {
            return text;
        }
    }

    /// <summary>The key + ".One" when the count is 1, otherwise the key + ".Many"; both formatted with the count as {0}.</summary>
    public string Plural(string key, string languageCode, int count) =>
        Format(count == 1 ? key + ".One" : key + ".Many", languageCode, count);

    /// <summary>
    /// The language in effect for a choice: "en" or "he". For <see cref="AppLanguage.System"/> a device language
    /// of he or iw (with or without a region, any case) gives Hebrew, anything else English.
    /// </summary>
    public static string ResolveLanguageCode(AppLanguage choice, string? deviceLanguageCode) => choice switch
    {
        AppLanguage.English => English,
        AppLanguage.Hebrew => Hebrew,
        _ => IsHebrewDevice(deviceLanguageCode) ? Hebrew : English,
    };

    /// <summary>True for Hebrew, the only right-to-left language of the app.</summary>
    public static bool IsRightToLeft(string languageCode) => IsHebrew(languageCode);

    private static bool IsHebrew(string? languageCode) =>
        string.Equals(languageCode, Hebrew, StringComparison.OrdinalIgnoreCase);

    private static bool IsHebrewDevice(string? deviceLanguageCode)
    {
        if (string.IsNullOrWhiteSpace(deviceLanguageCode))
        {
            return false;
        }

        var code = deviceLanguageCode.Trim();
        var end = code.IndexOfAny(['-', '_']);
        var language = end < 0 ? code : code[..end];
        return language.Equals("he", StringComparison.OrdinalIgnoreCase)
            || language.Equals("iw", StringComparison.OrdinalIgnoreCase);
    }
}
