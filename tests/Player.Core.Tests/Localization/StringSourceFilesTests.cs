using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Player.Core.Tests.LocalizationLogic;

/// <summary>
/// Reads the string source files of Player.App (the app project targets Android only, so it cannot be
/// referenced from here) and checks the texts of the user interface.
/// </summary>
public sealed class StringSourceFilesTests
{
    // Keys whose text is the same in every language: English has no letters at all or is a proper name.
    private static readonly HashSet<string> SameInEveryLanguage = new(StringComparer.Ordinal)
    {
        "Common.Language.English",
        "Player.Aspect.Original",
        "Defaults.Aspect.Original",
    };

    private static readonly string[] RequiredFiles =
    [
        "Localization/CommonStrings.cs",
        "Features/Explorer/ExplorerStrings.cs",
        "Features/GlobalDefaults/DefaultsStrings.cs",
        "Features/PlaybackSettings/SheetStrings.cs",
        "Features/Player/PlayerStrings.cs",
        "Features/UiSettings/InterfaceStrings.cs",
        "Theming/ThemeStrings.cs",
    ];

    private sealed record Entry(string Key, string English, string Hebrew, string File, int Line)
    {
        public string Where => $"{File}:{Line}";
    }

    private sealed record Usage(string Key, bool Counted, string File, int Line)
    {
        public string Where => $"{File}:{Line}";
    }

    private sealed record Parsed(
        IReadOnlyList<string> Files,
        IReadOnlyList<Entry> Entries,
        IReadOnlyList<string> BadLines,
        IReadOnlyList<Usage> XamlUsages,
        IReadOnlyList<Usage> CodeUsages);

    private static readonly Lazy<Parsed> Data = new(Load, LazyThreadSafetyMode.ExecutionAndPublication);

    private static readonly Regex EntryRegex = new(
        "^\\s*\\[\"(?<key>[^\"]*)\"\\]\\s*=\\s*new\\(\\s*\"(?<en>(?:[^\"\\\\]|\\\\.)*)\"\\s*,\\s*\"(?<he>(?:[^\"\\\\]|\\\\.)*)\"\\s*\\)\\s*,\\s*$",
        RegexOptions.CultureInvariant);

    private static readonly Regex XamlRegex = new(
        @"\{loc:T\s+(?:Key=)?(?<key>[A-Za-z0-9_.]+)\s*[},]",
        RegexOptions.CultureInvariant);

    private static readonly Regex[] CodeRegexes =
    [
        new(@"[A-Za-z_]*[Ll]ocalizer\s*\[\s*""(?<key>[A-Za-z0-9_.]+)""\s*\]", RegexOptions.CultureInvariant),
        new(@"\.Format\(\s*""(?<key>[A-Za-z0-9_]+(?:\.[A-Za-z0-9_]+)+)""", RegexOptions.CultureInvariant),
        new(@"\.GetText\(\s*""(?<key>[A-Za-z0-9_.]+)""", RegexOptions.CultureInvariant),
    ];

    private static readonly Regex PluralRegex = new(
        @"\.Plural\(\s*""(?<key>[A-Za-z0-9_.]+)""",
        RegexOptions.CultureInvariant);

    private static readonly Regex KeyFormat = new(
        @"^[A-Z][A-Za-z0-9]*(\.[A-Z][A-Za-z0-9]*)+$",
        RegexOptions.CultureInvariant);

    private static readonly Regex PlaceholderRegex = new(
        @"\{(?<index>\d+)(?:[,:][^}]*)?\}",
        RegexOptions.CultureInvariant);

    // ---- Loading ----

    private static string FindRepositoryRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "ArielPlayer.sln")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("ArielPlayer.sln not found above " + AppContext.BaseDirectory);
    }

    private static bool IsBuildOutput(string path) =>
        path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal) ||
        path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal);

    private static string Unescape(string literal)
    {
        var sb = new StringBuilder(literal.Length);
        for (var i = 0; i < literal.Length; i++)
        {
            var c = literal[i];
            if (c != '\\' || i + 1 >= literal.Length)
            {
                sb.Append(c);
                continue;
            }

            var next = literal[++i];
            switch (next)
            {
                case 'n': sb.Append('\n'); break;
                case 'r': sb.Append('\r'); break;
                case 't': sb.Append('\t'); break;
                case '0': sb.Append('\0'); break;
                case 'u' when i + 4 <= literal.Length - 1:
                    sb.Append((char)int.Parse(literal.AsSpan(i + 1, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture));
                    i += 4;
                    break;
                case 'U' when i + 8 <= literal.Length - 1:
                    sb.Append(char.ConvertFromUtf32(int.Parse(literal.AsSpan(i + 1, 8), NumberStyles.HexNumber, CultureInfo.InvariantCulture)));
                    i += 8;
                    break;
                default: sb.Append(next); break; // \\ \" \' and anything else
            }
        }

        return sb.ToString();
    }

    private static Parsed Load()
    {
        var root = FindRepositoryRoot();
        var appRoot = Path.Combine(root, "src", "Player.App");
        string Rel(string path) => Path.GetRelativePath(appRoot, path).Replace('\\', '/');

        var sourceFiles = Directory
            .EnumerateFiles(appRoot, "*Strings.cs", SearchOption.AllDirectories)
            .Where(p => !IsBuildOutput(p))
            .Where(p => File.ReadAllText(p).Contains("IStringSource", StringComparison.Ordinal))
            .OrderBy(p => Rel(p), StringComparer.Ordinal)
            .ToList();

        var entries = new List<Entry>();
        var bad = new List<string>();
        foreach (var path in sourceFiles)
        {
            var lines = File.ReadAllLines(path);
            for (var i = 0; i < lines.Length; i++)
            {
                if (!lines[i].TrimStart().StartsWith("[\"", StringComparison.Ordinal))
                {
                    continue;
                }

                var m = EntryRegex.Match(lines[i]);
                if (!m.Success)
                {
                    bad.Add($"{Rel(path)}:{i + 1}: {lines[i].Trim()}");
                    continue;
                }

                entries.Add(new Entry(
                    m.Groups["key"].Value,
                    Unescape(m.Groups["en"].Value),
                    Unescape(m.Groups["he"].Value),
                    Rel(path),
                    i + 1));
            }
        }

        var sourceSet = new HashSet<string>(sourceFiles, StringComparer.OrdinalIgnoreCase);
        var xaml = new List<Usage>();
        var code = new List<Usage>();

        foreach (var path in Directory.EnumerateFiles(appRoot, "*.*", SearchOption.AllDirectories))
        {
            if (IsBuildOutput(path))
            {
                continue;
            }

            var isXaml = path.EndsWith(".xaml", StringComparison.OrdinalIgnoreCase);
            var isCode = path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) && !sourceSet.Contains(path);
            if (!isXaml && !isCode)
            {
                continue;
            }

            var lines = File.ReadAllLines(path);
            for (var i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                if (isXaml)
                {
                    foreach (Match m in XamlRegex.Matches(line))
                    {
                        xaml.Add(new Usage(m.Groups["key"].Value, false, Rel(path), i + 1));
                    }

                    continue;
                }

                foreach (var regex in CodeRegexes)
                {
                    foreach (Match m in regex.Matches(line))
                    {
                        code.Add(new Usage(m.Groups["key"].Value, false, Rel(path), i + 1));
                    }
                }

                foreach (Match m in PluralRegex.Matches(line))
                {
                    code.Add(new Usage(m.Groups["key"].Value, true, Rel(path), i + 1));
                }
            }
        }

        return new Parsed(sourceFiles.Select(Rel).ToList(), entries, bad, xaml, code);
    }

    // ---- Helpers ----

    private static void AssertNone(IEnumerable<string> offenders, string what)
    {
        var list = offenders.ToList();
        Assert.True(list.Count == 0, what + Environment.NewLine + string.Join(Environment.NewLine, list));
    }

    private static bool HasHebrewLetter(string text) => text.Any(c => c is >= '\u05D0' and <= '\u05EA');

    private static HashSet<int> Placeholders(string text) =>
        PlaceholderRegex.Matches(text.Replace("{{", "", StringComparison.Ordinal).Replace("}}", "", StringComparison.Ordinal))
            .Select(m => int.Parse(m.Groups["index"].Value, CultureInfo.InvariantCulture))
            .ToHashSet();

    private static HashSet<string> DefinedKeys() => Data.Value.Entries.Select(e => e.Key).ToHashSet(StringComparer.Ordinal);

    // ---- 1. Parsing ----

    [Fact]
    public void Discovery_AppSourceTree_FindsAllSevenKnownStringSourceFiles()
    {
        var missing = RequiredFiles.Where(f => !Data.Value.Files.Contains(f, StringComparer.Ordinal)).ToList();

        AssertNone(missing, "String source files not found under src/Player.App:");
    }

    [Fact]
    public void Parse_EveryStringSourceFile_HasAtLeastOneEntry()
    {
        var empty = Data.Value.Files.Where(f => Data.Value.Entries.All(e => e.File != f));

        AssertNone(empty, "String source files without any entry:");
    }

    [Fact]
    public void Parse_EveryEntryLine_MatchesTheEntryForm()
    {
        AssertNone(Data.Value.BadLines, "Lines that start with [\" but do not match the entry form:");
    }

    // ---- 2. Duplicates ----

    [Fact]
    public void Keys_AllFiles_AreDefinedOnlyOnce()
    {
        var duplicates = Data.Value.Entries
            .GroupBy(e => e.Key, StringComparer.Ordinal)
            .Where(g => g.Count() > 1)
            .Select(g => $"{g.Key} defined at {string.Join(", ", g.Select(e => e.Where))}");

        AssertNone(duplicates, "Keys defined more than once:");
    }

    // ---- 3. Key form ----

    [Fact]
    public void Keys_AllFiles_HaveModuleDotNamePascalCaseForm()
    {
        var bad = Data.Value.Entries.Where(e => !KeyFormat.IsMatch(e.Key)).Select(e => $"{e.Key} ({e.Where})");

        AssertNone(bad, "Keys that are not <Module>.<Name> with PascalCase segments:");
    }

    [Fact]
    public void Keys_StartingWithCommon_AreDefinedOnlyInCommonStrings()
    {
        var bad = Data.Value.Entries
            .Where(e => e.Key.StartsWith("Common.", StringComparison.Ordinal) && e.File != "Localization/CommonStrings.cs")
            .Select(e => $"{e.Key} ({e.Where})");

        AssertNone(bad, "Common.* keys defined outside CommonStrings.cs:");
    }

    // ---- 4. Order ----

    [Fact]
    public void Entries_InsideEachFile_AreSortedByKeyOrdinal()
    {
        var bad = new List<string>();
        foreach (var group in Data.Value.Entries.GroupBy(e => e.File))
        {
            var list = group.ToList();
            for (var i = 1; i < list.Count; i++)
            {
                if (string.CompareOrdinal(list[i - 1].Key, list[i].Key) > 0)
                {
                    bad.Add($"{list[i].Key} ({list[i].Where}) comes after {list[i - 1].Key}");
                }
            }
        }

        AssertNone(bad, "Entries that are out of ordinal key order:");
    }

    // ---- 5. Non-empty ----

    [Fact]
    public void Values_English_AreNotEmptyOrWhiteSpace()
    {
        var bad = Data.Value.Entries.Where(e => string.IsNullOrWhiteSpace(e.English)).Select(e => $"{e.Key} ({e.Where})");

        AssertNone(bad, "Keys with an empty English value:");
    }

    [Fact]
    public void Values_Hebrew_AreNotEmptyOrWhiteSpace()
    {
        var bad = Data.Value.Entries.Where(e => string.IsNullOrWhiteSpace(e.Hebrew)).Select(e => $"{e.Key} ({e.Where})");

        AssertNone(bad, "Keys with an empty Hebrew value:");
    }

    // ---- 6. Hebrew letters ----

    [Fact]
    public void Values_Hebrew_ContainAHebrewLetterUnlessAllowListed()
    {
        var bad = Data.Value.Entries
            .Where(e => !HasHebrewLetter(e.Hebrew) && !SameInEveryLanguage.Contains(e.Key))
            .Select(e => $"{e.Key} ({e.Where}): Hebrew \"{e.Hebrew}\"");

        AssertNone(bad, "Keys whose Hebrew value has no Hebrew letter:");
    }

    [Fact]
    public void AllowList_EveryKey_IsDefined()
    {
        var defined = DefinedKeys();
        var stale = SameInEveryLanguage.Where(k => !defined.Contains(k));

        AssertNone(stale, "Allow-listed keys that are not defined:");
    }

    // ---- 7. Placeholders ----

    [Fact]
    public void Placeholders_EnglishAndHebrew_UseTheSameSet()
    {
        var bad = Data.Value.Entries
            .Where(e => !Placeholders(e.English).SetEquals(Placeholders(e.Hebrew)))
            .Select(e => $"{e.Key} ({e.Where}): English [{string.Join(",", Placeholders(e.English).Order())}] Hebrew [{string.Join(",", Placeholders(e.Hebrew).Order())}]");

        AssertNone(bad, "Keys whose English and Hebrew placeholders differ:");
    }

    // ---- 8. Counted texts ----

    [Fact]
    public void Counted_KeyEndingInOne_HasManySibling()
    {
        var defined = DefinedKeys();
        var bad = Data.Value.Entries
            .Where(e => e.Key.EndsWith(".One", StringComparison.Ordinal) &&
                        !defined.Contains(e.Key[..^".One".Length] + ".Many"))
            .Select(e => $"{e.Key} ({e.Where})");

        AssertNone(bad, "Keys ending in .One without a .Many sibling:");
    }

    [Fact]
    public void Counted_KeyEndingInMany_HasOneSibling()
    {
        var defined = DefinedKeys();
        var bad = Data.Value.Entries
            .Where(e => e.Key.EndsWith(".Many", StringComparison.Ordinal) &&
                        !defined.Contains(e.Key[..^".Many".Length] + ".One"))
            .Select(e => $"{e.Key} ({e.Where})");

        AssertNone(bad, "Keys ending in .Many without a .One sibling:");
    }

    [Fact]
    public void Counted_ManyText_ContainsCountPlaceholderInBothLanguages()
    {
        var bad = Data.Value.Entries
            .Where(e => e.Key.EndsWith(".Many", StringComparison.Ordinal) &&
                        (!e.English.Contains("{0}", StringComparison.Ordinal) ||
                         !e.Hebrew.Contains("{0}", StringComparison.Ordinal)))
            .Select(e => $"{e.Key} ({e.Where})");

        AssertNone(bad, "Keys ending in .Many whose text lacks {0}:");
    }

    // ---- 9 and 10. Used keys are defined ----

    [Fact]
    public void Usage_XamlFiles_FindsAtLeastOneKey()
    {
        Assert.NotEmpty(Data.Value.XamlUsages);
    }

    [Fact]
    public void Usage_CodeFiles_FindsAtLeastOneKey()
    {
        Assert.NotEmpty(Data.Value.CodeUsages);
    }

    [Fact]
    public void Usage_KeysInXaml_AreDefined()
    {
        var defined = DefinedKeys();
        var bad = Data.Value.XamlUsages
            .Where(u => !defined.Contains(u.Key))
            .Select(u => $"{u.Key} ({u.Where})");

        AssertNone(bad, "Keys used in XAML but not defined:");
    }

    [Fact]
    public void Usage_KeysInCode_AreDefined()
    {
        var defined = DefinedKeys();
        var bad = Data.Value.CodeUsages
            .Where(u => !u.Counted && !defined.Contains(u.Key))
            .Select(u => $"{u.Key} ({u.Where})");

        AssertNone(bad, "Keys used in C# but not defined:");
    }

    [Fact]
    public void Usage_PluralKeysInCode_HaveOneAndManyDefined()
    {
        var defined = DefinedKeys();
        var bad = new List<string>();
        foreach (var u in Data.Value.CodeUsages.Where(u => u.Counted))
        {
            foreach (var suffix in new[] { ".One", ".Many" })
            {
                if (!defined.Contains(u.Key + suffix))
                {
                    bad.Add($"{u.Key}{suffix} ({u.Where})");
                }
            }
        }

        AssertNone(bad, "Plural keys used in C# without a defined .One or .Many text:");
    }

    // ---- 11. Language and duplicates of meaning ----

    [Fact]
    public void Values_English_ContainNoHebrewLetters()
    {
        var bad = Data.Value.Entries
            .Where(e => HasHebrewLetter(e.English))
            .Select(e => $"{e.Key} ({e.Where}): English \"{e.English}\"");

        AssertNone(bad, "Keys whose English value contains Hebrew letters:");
    }

    [Fact]
    public void Values_CommonStrings_HaveNoIdenticalEnglishTextUnderTwoKeys()
    {
        var bad = Data.Value.Entries
            .Where(e => e.File == "Localization/CommonStrings.cs")
            .GroupBy(e => e.English, StringComparer.Ordinal)
            .Where(g => g.Count() > 1)
            .Select(g => $"\"{g.Key}\" is the English value of {string.Join(", ", g.Select(e => $"{e.Key} ({e.Where})"))}");

        AssertNone(bad, "Identical English values under different keys in CommonStrings.cs:");
    }
}
