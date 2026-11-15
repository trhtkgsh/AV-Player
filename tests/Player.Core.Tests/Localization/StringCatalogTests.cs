using System.Globalization;
using Player.Core.Contracts;
using Player.Core.Logic.Localization;
using Player.Core.Models;

namespace Player.Core.Tests.LocalizationLogic;

public sealed class StringCatalogTests
{
    private sealed class FakeStringSource : IStringSource
    {
        public FakeStringSource(params (string Key, string English, string Hebrew)[] entries)
        {
            var texts = new Dictionary<string, LocalizedText>();
            foreach (var (key, english, hebrew) in entries)
            {
                texts[key] = new LocalizedText(english, hebrew);
            }

            Texts = texts;
        }

        public IReadOnlyDictionary<string, LocalizedText> Texts { get; }
    }

    private static StringCatalog Catalog(params (string Key, string English, string Hebrew)[] entries) =>
        new([new FakeStringSource(entries)]);

    // ---- Get ----

    [Fact]
    public void Get_KnownKeyEnglish_ReturnsEnglishText()
    {
        var catalog = Catalog(("Explorer.Sort", "Sort", "מיון"));

        Assert.Equal("Sort", catalog.Get("Explorer.Sort", "en"));
    }

    [Fact]
    public void Get_KnownKeyHebrew_ReturnsHebrewText()
    {
        var catalog = Catalog(("Explorer.Sort", "Sort", "מיון"));

        Assert.Equal("מיון", catalog.Get("Explorer.Sort", "he"));
    }

    [Fact]
    public void Get_UnknownKey_ReturnsKeyInBrackets()
    {
        var catalog = Catalog(("Explorer.Sort", "Sort", "מיון"));

        Assert.Equal("[Missing.Key]", catalog.Get("Missing.Key", "en"));
        Assert.Equal("[Missing.Key]", catalog.Get("Missing.Key", "he"));
    }

    [Theory]
    [InlineData("fr")]
    [InlineData("ar")]
    [InlineData("")]
    [InlineData("heb")]
    [InlineData("xx")]
    public void Get_OtherLanguageCode_ReturnsEnglish(string code)
    {
        var catalog = Catalog(("Explorer.Sort", "Sort", "מיון"));

        Assert.Equal("Sort", catalog.Get("Explorer.Sort", code));
    }

    [Fact]
    public void Get_KeyIsCaseSensitive_UnknownWhenCaseDiffers()
    {
        var catalog = Catalog(("Explorer.Sort", "Sort", "מיון"));

        Assert.Equal("[explorer.sort]", catalog.Get("explorer.sort", "en"));
    }

    // ---- TryGet and Contains ----

    [Fact]
    public void TryGet_KnownKey_ReturnsTrueAndText()
    {
        var catalog = Catalog(("Explorer.Sort", "Sort", "מיון"));

        Assert.True(catalog.TryGet("Explorer.Sort", "en", out var english));
        Assert.True(catalog.TryGet("Explorer.Sort", "he", out var hebrew));
        Assert.Equal("Sort", english);
        Assert.Equal("מיון", hebrew);
    }

    [Fact]
    public void TryGet_UnknownKey_ReturnsFalseAndEmptyText()
    {
        var catalog = Catalog(("Explorer.Sort", "Sort", "מיון"));

        Assert.False(catalog.TryGet("Missing.Key", "en", out var text));
        Assert.Equal(string.Empty, text);
    }

    [Fact]
    public void Contains_KnownKey_ReturnsTrue()
    {
        var catalog = Catalog(("Explorer.Sort", "Sort", "מיון"));

        Assert.True(catalog.Contains("Explorer.Sort"));
    }

    [Fact]
    public void Contains_UnknownKey_ReturnsFalse()
    {
        var catalog = Catalog(("Explorer.Sort", "Sort", "מיון"));

        Assert.False(catalog.Contains("Missing.Key"));
        Assert.False(catalog.Contains(string.Empty));
    }

    // ---- Format ----

    [Fact]
    public void Format_EnglishPlaceholders_AreFilled()
    {
        var catalog = Catalog(("Player.Position", "{0} of {1}", "{0} מתוך {1}"));

        Assert.Equal("3 of 10", catalog.Format("Player.Position", "en", 3, 10));
    }

    [Fact]
    public void Format_HebrewPlaceholders_AreFilled()
    {
        var catalog = Catalog(("Player.Position", "{0} of {1}", "{0} מתוך {1}"));

        Assert.Equal("3 מתוך 10", catalog.Format("Player.Position", "he", 3, 10));
    }

    [Fact]
    public void Format_NoArgs_ReturnsTextUnformatted()
    {
        var catalog = Catalog(("Player.Position", "{0} of {1}", "{0} מתוך {1}"));

        Assert.Equal("{0} of {1}", catalog.Format("Player.Position", "en"));
    }

    [Fact]
    public void Format_NoArgsAndEscapedBraces_ReturnsTextAsStored()
    {
        var catalog = Catalog(("Key.Braces", "{{x}}", "{{x}}"));

        Assert.Equal("{{x}}", catalog.Format("Key.Braces", "en"));
    }

    [Fact]
    public void Format_PlaceholderIndexOutOfRange_ReturnsTextUnformattedAndDoesNotThrow()
    {
        var catalog = Catalog(("Key.Bad", "Value {5}", "ערך {5}"));

        Assert.Equal("Value {5}", catalog.Format("Key.Bad", "en", 1));
        Assert.Equal("ערך {5}", catalog.Format("Key.Bad", "he", 1));
    }

    [Fact]
    public void Format_LoneOpenBrace_ReturnsTextUnformattedAndDoesNotThrow()
    {
        var catalog = Catalog(("Key.Bad", "Broken {", "שבור {"));

        Assert.Equal("Broken {", catalog.Format("Key.Bad", "en", 1));
        Assert.Equal("שבור {", catalog.Format("Key.Bad", "he", 1));
    }

    [Fact]
    public void Format_UnknownKey_ReturnsKeyInBrackets()
    {
        var catalog = Catalog(("Player.Position", "{0} of {1}", "{0} מתוך {1}"));

        Assert.Equal("[Missing.Key]", catalog.Format("Missing.Key", "en", 1));
        Assert.Equal("[Missing.Key]", catalog.Format("Missing.Key", "he"));
    }

    [Fact]
    public void Format_OtherLanguageCode_UsesEnglish()
    {
        var catalog = Catalog(("Player.Position", "{0} of {1}", "{0} מתוך {1}"));

        Assert.Equal("3 of 10", catalog.Format("Player.Position", "fr", 3, 10));
    }

    [Fact]
    public void Format_NumberUnderGermanThreadCulture_UsesInvariantCulture()
    {
        var catalog = Catalog(("Key.Number", "Value {0:N1}", "ערך {0:N1}"));
        var original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("de-DE");

            Assert.Equal("Value 1,234.5", catalog.Format("Key.Number", "en", 1234.5));
            Assert.Equal("ערך 1,234.5", catalog.Format("Key.Number", "he", 1234.5));
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    [Fact]
    public void Format_NumberUnderInvariantAndGermanCulture_GivesSameResult()
    {
        var catalog = Catalog(("Key.Number", "Value {0}", "ערך {0}"));
        var original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            var invariant = catalog.Format("Key.Number", "en", 0.5);

            CultureInfo.CurrentCulture = new CultureInfo("de-DE");
            var german = catalog.Format("Key.Number", "en", 0.5);

            Assert.Equal("Value 0.5", invariant);
            Assert.Equal(invariant, german);
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    // ---- Plural ----

    private static StringCatalog PluralCatalog() => Catalog(
        ("Files.One", "1 file", "קובץ אחד"),
        ("Files.Many", "{0} files", "{0} קבצים"));

    [Fact]
    public void Plural_CountOne_UsesOneKeyEnglish()
    {
        Assert.Equal("1 file", PluralCatalog().Plural("Files", "en", 1));
    }

    [Fact]
    public void Plural_CountOne_UsesOneKeyHebrew()
    {
        Assert.Equal("קובץ אחד", PluralCatalog().Plural("Files", "he", 1));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    [InlineData(11)]
    [InlineData(-1)]
    public void Plural_CountNotOne_UsesManyKeyWithCountEnglish(int count)
    {
        Assert.Equal($"{count} files", PluralCatalog().Plural("Files", "en", count));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    [InlineData(11)]
    public void Plural_CountNotOne_UsesManyKeyWithCountHebrew(int count)
    {
        Assert.Equal($"{count} קבצים", PluralCatalog().Plural("Files", "he", count));
    }

    [Fact]
    public void Plural_LargeCount_FormatsWithInvariantCultureOnGermanThread()
    {
        var catalog = Catalog(("Files.Many", "{0:N0} files", "{0:N0} קבצים"));
        var original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("de-DE");

            Assert.Equal("1,234 files", catalog.Plural("Files", "en", 1234));
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    [Fact]
    public void Plural_UnknownKeyCountOne_ReturnsBracketedOneKey()
    {
        Assert.Equal("[Missing.One]", PluralCatalog().Plural("Missing", "en", 1));
    }

    [Fact]
    public void Plural_UnknownKeyOtherCount_ReturnsBracketedManyKey()
    {
        Assert.Equal("[Missing.Many]", PluralCatalog().Plural("Missing", "en", 2));
        Assert.Equal("[Missing.Many]", PluralCatalog().Plural("Missing", "he", 0));
    }

    // ---- Merging sources and duplicates ----

    [Fact]
    public void Constructor_SeveralSources_MergesAllKeys()
    {
        var catalog = new StringCatalog(
        [
            new FakeStringSource(("A.One", "one", "אחד")),
            new FakeStringSource(("B.Two", "two", "שניים")),
            new FakeStringSource(("C.Three", "three", "שלושה")),
        ]);

        Assert.Equal("one", catalog.Get("A.One", "en"));
        Assert.Equal("שניים", catalog.Get("B.Two", "he"));
        Assert.Equal("three", catalog.Get("C.Three", "en"));
        Assert.Empty(catalog.DuplicateKeys);
    }

    [Fact]
    public void Constructor_KeyInTwoSources_FirstDefinitionWins()
    {
        var catalog = new StringCatalog(
        [
            new FakeStringSource(("A.Key", "first", "ראשון")),
            new FakeStringSource(("A.Key", "second", "שני")),
        ]);

        Assert.Equal("first", catalog.Get("A.Key", "en"));
        Assert.Equal("ראשון", catalog.Get("A.Key", "he"));
    }

    [Fact]
    public void DuplicateKeys_KeyInTwoSources_ListedOnce()
    {
        var catalog = new StringCatalog(
        [
            new FakeStringSource(("A.Key", "first", "ראשון")),
            new FakeStringSource(("A.Key", "second", "שני")),
        ]);

        Assert.Equal(["A.Key"], catalog.DuplicateKeys);
    }

    [Fact]
    public void DuplicateKeys_KeyInThreeSources_ListedOnce()
    {
        var catalog = new StringCatalog(
        [
            new FakeStringSource(("A.Key", "1", "1")),
            new FakeStringSource(("A.Key", "2", "2")),
            new FakeStringSource(("A.Key", "3", "3")),
        ]);

        Assert.Equal(["A.Key"], catalog.DuplicateKeys);
        Assert.Equal("1", catalog.Get("A.Key", "en"));
    }

    [Fact]
    public void DuplicateKeys_SeveralDuplicates_ListedInOrderFound()
    {
        var catalog = new StringCatalog(
        [
            new FakeStringSource(("A.Key", "1", "1"), ("B.Key", "1", "1")),
            new FakeStringSource(("B.Key", "2", "2")),
            new FakeStringSource(("A.Key", "2", "2")),
        ]);

        Assert.Equal(["B.Key", "A.Key"], catalog.DuplicateKeys);
    }

    [Fact]
    public void DuplicateKeys_NoDuplicates_IsEmpty()
    {
        var catalog = new StringCatalog(
        [
            new FakeStringSource(("A.Key", "1", "1")),
            new FakeStringSource(("B.Key", "2", "2")),
        ]);

        Assert.Empty(catalog.DuplicateKeys);
    }

    // ---- Empty catalog ----

    [Fact]
    public void EmptyCatalog_NoSources_WorksAndKnowsNothing()
    {
        var catalog = new StringCatalog([]);

        Assert.Empty(catalog.DuplicateKeys);
        Assert.False(catalog.Contains("Any.Key"));
        Assert.False(catalog.TryGet("Any.Key", "en", out _));
        Assert.Equal("[Any.Key]", catalog.Get("Any.Key", "he"));
        Assert.Equal("[Any.Key]", catalog.Format("Any.Key", "en", 1));
        Assert.Equal("[Any.Many]", catalog.Plural("Any", "en", 5));
    }

    [Fact]
    public void EmptyCatalog_SourceWithoutTexts_Works()
    {
        var catalog = new StringCatalog([new FakeStringSource()]);

        Assert.Empty(catalog.DuplicateKeys);
        Assert.Equal("[X]", catalog.Get("X", "en"));
    }

    // ---- Empty (null) texts ----

    private sealed class RawStringSource : IStringSource
    {
        public RawStringSource(string key, LocalizedText text)
        {
            Texts = new Dictionary<string, LocalizedText> { [key] = text };
        }

        public IReadOnlyDictionary<string, LocalizedText> Texts { get; }
    }

    private sealed class ThrowingToString
    {
        public override string ToString() => throw new InvalidOperationException("ToString failed");
    }

    private static StringCatalog RawCatalog(string key, LocalizedText text) => new([new RawStringSource(key, text)]);

    [Theory]
    [InlineData("en")]
    [InlineData("he")]
    public void Get_DefaultLocalizedText_ReturnsKeyInBrackets(string code)
    {
        var catalog = RawCatalog("Key.Empty", default);

        Assert.Equal("[Key.Empty]", catalog.Get("Key.Empty", code));
    }

    [Theory]
    [InlineData("en")]
    [InlineData("he")]
    public void TryGet_DefaultLocalizedText_ReturnsFalseAndEmptyText(string code)
    {
        var catalog = RawCatalog("Key.Empty", default);

        Assert.False(catalog.TryGet("Key.Empty", code, out var text));
        Assert.Equal(string.Empty, text);
    }

    [Fact]
    public void Contains_DefaultLocalizedText_ReturnsTrue()
    {
        var catalog = RawCatalog("Key.Empty", default);

        Assert.True(catalog.Contains("Key.Empty"));
    }

    [Theory]
    [InlineData("en")]
    [InlineData("he")]
    public void Format_DefaultLocalizedText_ReturnsKeyInBracketsAndDoesNotThrow(string code)
    {
        var catalog = RawCatalog("Key.Empty", default);

        Assert.Equal("[Key.Empty]", catalog.Format("Key.Empty", code));
        Assert.Equal("[Key.Empty]", catalog.Format("Key.Empty", code, 1, "x"));
    }

    [Theory]
    [InlineData("en")]
    [InlineData("he")]
    public void Plural_DefaultLocalizedText_ReturnsBracketedKeyAndDoesNotThrow(string code)
    {
        var catalog = new StringCatalog(
        [
            new RawStringSource("Files.One", default),
            new RawStringSource("Files.Many", default),
        ]);

        Assert.Equal("[Files.One]", catalog.Plural("Files", code, 1));
        Assert.Equal("[Files.Many]", catalog.Plural("Files", code, 5));
    }

    [Fact]
    public void Get_NullHebrewAndSetEnglish_ReturnsTextInEnglishAndBracketedKeyInHebrew()
    {
        // Current rule: a null text counts as unknown, there is no fallback from Hebrew to English.
        var catalog = RawCatalog("Key.HeMissing", new LocalizedText("Only English", null!));

        Assert.Equal("Only English", catalog.Get("Key.HeMissing", "en"));
        Assert.Equal("[Key.HeMissing]", catalog.Get("Key.HeMissing", "he"));
    }

    [Fact]
    public void TryGet_NullHebrewAndSetEnglish_TrueInEnglishFalseInHebrew()
    {
        var catalog = RawCatalog("Key.HeMissing", new LocalizedText("Only English", null!));

        Assert.True(catalog.TryGet("Key.HeMissing", "en", out var english));
        Assert.Equal("Only English", english);
        Assert.False(catalog.TryGet("Key.HeMissing", "he", out var hebrew));
        Assert.Equal(string.Empty, hebrew);
    }

    [Fact]
    public void Format_NullHebrewAndSetEnglish_FormatsEnglishAndBracketsKeyInHebrew()
    {
        var catalog = RawCatalog("Key.HeMissing", new LocalizedText("Value {0}", null!));

        Assert.Equal("Value 7", catalog.Format("Key.HeMissing", "en", 7));
        Assert.Equal("[Key.HeMissing]", catalog.Format("Key.HeMissing", "he", 7));
    }

    [Fact]
    public void Get_NullEnglishAndSetHebrew_ReturnsBracketedKeyInEnglishAndTextInHebrew()
    {
        var catalog = RawCatalog("Key.EnMissing", new LocalizedText(null!, "רק עברית"));

        Assert.Equal("[Key.EnMissing]", catalog.Get("Key.EnMissing", "en"));
        Assert.Equal("רק עברית", catalog.Get("Key.EnMissing", "he"));
    }

    [Fact]
    public void Format_ArgumentToStringThrows_ReturnsUnformattedTextAndDoesNotThrow()
    {
        var catalog = Catalog(("Key.Arg", "Value {0}", "ערך {0}"));

        Assert.Equal("Value {0}", catalog.Format("Key.Arg", "en", new ThrowingToString()));
        Assert.Equal("ערך {0}", catalog.Format("Key.Arg", "he", new ThrowingToString()));
    }

    [Fact]
    public void Format_ArgumentToStringThrowsButNotUsed_FormatsNormally()
    {
        var catalog = Catalog(("Key.Arg", "Value {0}", "ערך {0}"));

        Assert.Equal("Value 1", catalog.Format("Key.Arg", "en", 1, new ThrowingToString()));
    }

    [Fact]
    public void Format_ArgsNull_ReturnsTextUnformattedAndDoesNotThrow()
    {
        var catalog = Catalog(("Key.Arg", "Value {0}", "ערך {0}"));

        Assert.Equal("Value {0}", catalog.Format("Key.Arg", "en", (object?[])null!));
        Assert.Equal("ערך {0}", catalog.Format("Key.Arg", "he", (object?[])null!));
    }

    [Fact]
    public void Format_ArgsNullAndUnknownKey_ReturnsKeyInBrackets()
    {
        var catalog = Catalog(("Key.Arg", "Value {0}", "ערך {0}"));

        Assert.Equal("[Missing.Key]", catalog.Format("Missing.Key", "en", (object?[])null!));
    }

    [Fact]
    public void Format_SingleNullArgument_FormatsAsEmptyString()
    {
        var catalog = Catalog(("Key.Arg", "Value [{0}]", "ערך [{0}]"));

        Assert.Equal("Value []", catalog.Format("Key.Arg", "en", (object?)null));
    }

    // ---- ResolveLanguageCode ----

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("he")]
    [InlineData("en")]
    [InlineData("fr")]
    public void ResolveLanguageCode_EnglishChoice_IgnoresDevice(string? device)
    {
        Assert.Equal("en", StringCatalog.ResolveLanguageCode(AppLanguage.English, device));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("he")]
    [InlineData("en")]
    [InlineData("fr")]
    public void ResolveLanguageCode_HebrewChoice_IgnoresDevice(string? device)
    {
        Assert.Equal("he", StringCatalog.ResolveLanguageCode(AppLanguage.Hebrew, device));
    }

    [Theory]
    [InlineData("he", "he")]
    [InlineData("iw", "he")]
    [InlineData("he-IL", "he")]
    [InlineData("iw_IL", "he")]
    [InlineData("HE", "he")]
    [InlineData("en", "en")]
    [InlineData("en-US", "en")]
    [InlineData("ar", "en")]
    [InlineData("fr", "en")]
    [InlineData("", "en")]
    [InlineData(null, "en")]
    public void ResolveLanguageCode_SystemChoice_FollowsDevice(string? device, string expected)
    {
        Assert.Equal(expected, StringCatalog.ResolveLanguageCode(AppLanguage.System, device));
    }

    // ---- IsRightToLeft and constants ----

    [Theory]
    [InlineData("he", true)]
    [InlineData("en", false)]
    [InlineData("ar", false)]
    [InlineData("", false)]
    public void IsRightToLeft_LanguageCode_TrueOnlyForHebrew(string code, bool expected)
    {
        Assert.Equal(expected, StringCatalog.IsRightToLeft(code));
    }

    [Fact]
    public void Constants_HaveLanguageCodes()
    {
        Assert.Equal("en", StringCatalog.English);
        Assert.Equal("he", StringCatalog.Hebrew);
    }
}
