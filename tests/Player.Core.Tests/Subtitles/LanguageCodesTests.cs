using Player.Core.Logic.Subtitles;

namespace Player.Core.Tests.Subtitles;

public sealed class LanguageCodesTests
{
    public static TheoryData<string, string, string> Languages => new()
    {
        { "he", "heb", "Hebrew" },
        { "en", "eng", "English" },
        { "ar", "ara", "Arabic" },
        { "ru", "rus", "Russian" },
        { "fr", "fre", "French" },
        { "de", "ger", "German" },
        { "es", "spa", "Spanish" },
        { "it", "ita", "Italian" },
        { "pt", "por", "Portuguese" },
        { "nl", "dut", "Dutch" },
        { "pl", "pol", "Polish" },
        { "tr", "tur", "Turkish" },
        { "el", "gre", "Greek" },
        { "uk", "ukr", "Ukrainian" },
        { "ro", "rum", "Romanian" },
        { "hu", "hun", "Hungarian" },
        { "cs", "cze", "Czech" },
        { "sv", "swe", "Swedish" },
        { "no", "nor", "Norwegian" },
        { "da", "dan", "Danish" },
        { "fi", "fin", "Finnish" },
        { "ja", "jpn", "Japanese" },
        { "ko", "kor", "Korean" },
        { "zh", "chi", "Chinese" },
        { "hi", "hin", "Hindi" },
        { "fa", "per", "Persian" },
        { "yi", "yid", "Yiddish" },
        { "am", "amh", "Amharic" },
    };

    [Theory]
    [MemberData(nameof(Languages))]
    public void Matches_TwoLetterThreeLetterAndName_AllMatchEachOther(string two, string three, string name)
    {
        Assert.True(LanguageCodes.Matches(two, three));
        Assert.True(LanguageCodes.Matches(three, two));
        Assert.True(LanguageCodes.Matches(two, name));
        Assert.True(LanguageCodes.Matches(name, two));
        Assert.True(LanguageCodes.Matches(three, name));
        Assert.True(LanguageCodes.Matches(name, three));
    }

    [Theory]
    [InlineData("fre", "fra")]
    [InlineData("ger", "deu")]
    [InlineData("dut", "nld")]
    [InlineData("gre", "ell")]
    [InlineData("rum", "ron")]
    [InlineData("cze", "ces")]
    [InlineData("chi", "zho")]
    [InlineData("per", "fas")]
    public void Matches_BothIso6392Forms_ReturnsTrue(string a, string b)
    {
        Assert.True(LanguageCodes.Matches(a, b));
    }

    [Theory]
    [InlineData("iw", "he")]
    [InlineData("iw", "heb")]
    [InlineData("iw", "Hebrew")]
    public void Matches_OldHebrewCode_MatchesHebrew(string a, string b)
    {
        Assert.True(LanguageCodes.Matches(a, b));
        Assert.True(LanguageCodes.Matches(b, a));
    }

    [Theory]
    [InlineData("HE", "he")]
    [InlineData("HEBREW", "heb")]
    [InlineData("  he  ", "heb")]
    [InlineData("\tEnglish ", "EN")]
    public void Matches_CaseAndWhitespaceDiffer_ReturnsTrue(string a, string b)
    {
        Assert.True(LanguageCodes.Matches(a, b));
    }

    [Theory]
    [InlineData("en-US", "en")]
    [InlineData("pt_BR", "por")]
    [InlineData("zh-Hans", "Chinese")]
    [InlineData("en-US", "en-GB")]
    public void Matches_RegionOrScriptSuffix_IsIgnored(string a, string b)
    {
        Assert.True(LanguageCodes.Matches(a, b));
        Assert.True(LanguageCodes.Matches(b, a));
    }

    [Theory]
    [InlineData("he", "en")]
    [InlineData("heb", "eng")]
    [InlineData("Hebrew", "English")]
    [InlineData("fr", "de")]
    public void Matches_DifferentLanguages_ReturnsFalse(string a, string b)
    {
        Assert.False(LanguageCodes.Matches(a, b));
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData(null, "he")]
    [InlineData("he", null)]
    [InlineData("", "")]
    [InlineData("", "he")]
    [InlineData("he", "")]
    [InlineData("   ", "he")]
    public void Matches_NullOrEmpty_ReturnsFalse(string? a, string? b)
    {
        Assert.False(LanguageCodes.Matches(a, b));
    }

    [Fact]
    public void Matches_UnknownAndKnown_ReturnsFalse()
    {
        Assert.False(LanguageCodes.Matches("xx", "he"));
    }

    [Fact]
    public void Matches_TwoDifferentUnknownCodes_ReturnsFalse()
    {
        Assert.False(LanguageCodes.Matches("xx", "yy"));
    }

    [Fact]
    public void Matches_TwoEqualUnknownCodesDifferentCase_ReturnsTrue()
    {
        Assert.True(LanguageCodes.Matches("xx", "XX"));
    }

    [Theory]
    [InlineData("heb", "he")]
    [InlineData("iw", "he")]
    [InlineData("Hebrew", "he")]
    [InlineData("FRA", "fr")]
    [InlineData("deu", "de")]
    [InlineData("nob", "no")]
    [InlineData("pt_BR", "pt")]
    [InlineData("zh-Hans", "zh")]
    [InlineData("  ENG ", "en")]
    public void Normalize_KnownLanguage_ReturnsIso6391Code(string input, string expected)
    {
        Assert.Equal(expected, LanguageCodes.Normalize(input));
    }

    [Theory]
    [InlineData("XX", "xx")]
    [InlineData("  Klingon ", "klingon")]
    [InlineData("Xx-Latn", "xx")]
    public void Normalize_UnknownLanguage_ReturnsTrimmedLowerCasePrimaryPart(string input, string expected)
    {
        Assert.Equal(expected, LanguageCodes.Normalize(input));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Normalize_NullOrEmpty_ReturnsNull(string? input)
    {
        Assert.Null(LanguageCodes.Normalize(input));
    }
}
