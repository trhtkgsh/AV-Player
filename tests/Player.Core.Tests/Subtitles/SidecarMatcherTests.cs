using Player.Core.Logic.Subtitles;

namespace Player.Core.Tests.Subtitles;

public sealed class SidecarMatcherTests
{
    private static string[] Names(string video, string[] candidates, string? preferred = null) =>
        SidecarMatcher.Match(video, candidates, preferred).Select(m => m.FileName).ToArray();

    private static string? LanguageOf(string suffixFile)
    {
        var matches = SidecarMatcher.Match("Movie.mkv", new[] { suffixFile }, null);
        return Assert.Single(matches).Language;
    }

    // --- matching ---

    [Fact]
    public void Match_ExactBaseName_Matches()
    {
        Assert.Equal(new[] { "Movie.srt" }, Names("Movie.mkv", new[] { "Movie.srt" }));
    }

    [Fact]
    public void Match_BaseNameWithDotsInside_UsesNameWithoutLastExtension()
    {
        Assert.Equal(new[] { "Movie.2019.srt" }, Names("Movie.2019.mkv", new[] { "Movie.2019.srt" }));
    }

    [Fact]
    public void Match_VideoNameWithoutDot_IsItsOwnBaseName()
    {
        Assert.Equal(new[] { "Movie.srt" }, Names("Movie", new[] { "Movie.srt" }));
    }

    [Theory]
    [InlineData("Movie.2019.srt")]
    [InlineData("Movie.2019.ass")]
    [InlineData("Movie.2019.ssa")]
    [InlineData("Movie.2019.SRT")]
    [InlineData("Movie.2019.Ass")]
    [InlineData("Movie.2019.SSA")]
    public void Match_SubtitleExtension_Matches(string candidate)
    {
        Assert.Equal(new[] { candidate }, Names("Movie.2019.mkv", new[] { candidate }));
    }

    [Theory]
    [InlineData("MOVIE.2019.srt")]
    [InlineData("movie.2019.he.srt")]
    public void Match_BaseNameDiffersInCase_Matches(string candidate)
    {
        Assert.Equal(new[] { candidate }, Names("Movie.2019.mkv", new[] { candidate }));
    }

    [Theory]
    [InlineData("Movie.2019.he.srt")]
    [InlineData("Movie.2019.en.forced.srt")]
    [InlineData("Movie.2019.a.b.c.srt")]
    public void Match_BaseNameFollowedBySuffixParts_Matches(string candidate)
    {
        Assert.Equal(new[] { candidate }, Names("Movie.2019.mkv", new[] { candidate }));
    }

    [Theory]
    [InlineData("Movie.2019-extras.srt")]
    [InlineData("Movie.20190.srt")]
    [InlineData("Movie.2019.txt")]
    [InlineData("Movie.2019.srt.txt")]
    [InlineData("Movie.2019")]
    [InlineData("Other.srt")]
    [InlineData("Movie.srt")]
    [InlineData("Movie.2019.he.sub")]
    public void Match_NotASidecarOfTheVideo_IsNotReturned(string candidate)
    {
        Assert.Empty(Names("Movie.2019.mkv", new[] { candidate }));
    }

    [Theory]
    [InlineData("Movie.2019..he.srt")]
    [InlineData("Movie.2019..srt")]
    [InlineData("Movie.2019.he..srt")]
    [InlineData("Movie.2019.he..forced.srt")]
    public void Match_EmptySuffixPart_IsNotReturned(string candidate)
    {
        Assert.Empty(Names("Movie.2019.mkv", new[] { candidate }));
    }

    [Fact]
    public void Match_CandidateEqualToVideoFile_IsNotReturned()
    {
        Assert.Empty(Names("Movie.srt", new[] { "Movie.srt", "MOVIE.SRT" }));
    }

    [Fact]
    public void Match_VideoFileAmongCandidates_IsNotReturned()
    {
        Assert.Equal(new[] { "Movie.srt" }, Names("Movie.mkv", new[] { "Movie.mkv", "Movie.srt" }));
    }

    [Fact]
    public void Match_DuplicatesIgnoringCase_ReturnedOnce()
    {
        var result = Names("Movie.mkv", new[] { "Movie.he.srt", "MOVIE.HE.SRT", "Movie.he.srt" });

        Assert.Equal(new[] { "Movie.he.srt" }, result);
    }

    // --- language ---

    [Fact]
    public void Match_NoSuffix_LanguageIsNull()
    {
        Assert.Null(LanguageOf("Movie.srt"));
    }

    [Theory]
    [InlineData("Movie.he.srt", "he")]
    [InlineData("Movie.eng.srt", "eng")]
    [InlineData("Movie.en.forced.srt", "en")]
    [InlineData("Movie.Hebrew.srt", "Hebrew")]
    [InlineData("Movie.xx.srt", "xx")]
    [InlineData("Movie.xyz.srt", "xyz")]
    public void Match_FirstSuffixIsLanguage_ReturnsLanguageAsWritten(string candidate, string expected)
    {
        Assert.Equal(expected, LanguageOf(candidate));
    }

    [Theory]
    [InlineData("Movie.EN.srt", "EN")]
    [InlineData("Movie.Heb.srt", "Heb")]
    [InlineData("Movie.pt-BR.srt", "pt-BR")]
    public void Match_LanguageSuffixInOtherCase_IsNotNormalized(string candidate, string expected)
    {
        Assert.Equal(expected, LanguageOf(candidate));
    }

    [Theory]
    [InlineData("Movie.forced.srt")]
    [InlineData("Movie.sdh.srt")]
    [InlineData("Movie.cc.srt")]
    [InlineData("Movie.CC.srt")]
    [InlineData("Movie.SDH.srt")]
    [InlineData("Movie.12.srt")]
    [InlineData("Movie.commentary.srt")]
    public void Match_FirstSuffixIsNotALanguage_LanguageIsNull(string candidate)
    {
        Assert.Null(LanguageOf(candidate));
    }

    [Fact]
    public void Match_ForcedBeforeLanguage_LanguageIsNull()
    {
        Assert.Null(LanguageOf("Movie.forced.he.srt"));
    }

    // --- order ---

    [Fact]
    public void Match_PreferredLanguage_ComesFirstThenNoSuffixThenRest()
    {
        var result = Names("Movie.mkv", new[] { "Movie.en.srt", "Movie.srt", "Movie.he.srt" }, "he");

        Assert.Equal(new[] { "Movie.he.srt", "Movie.srt", "Movie.en.srt" }, result);
    }

    [Fact]
    public void Match_NoPreferredLanguage_NoSuffixComesFirst()
    {
        var result = Names("Movie.mkv", new[] { "Movie.he.srt", "Movie.srt" }, null);

        Assert.Equal(new[] { "Movie.srt", "Movie.he.srt" }, result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Match_NullOrEmptyPreferredLanguage_HasNoPreferredGroup(string? preferred)
    {
        var result = Names("Movie.mkv", new[] { "Movie.he.srt", "Movie.en.srt", "Movie.srt" }, preferred);

        Assert.Equal(new[] { "Movie.srt", "Movie.en.srt", "Movie.he.srt" }, result);
    }

    [Theory]
    [InlineData("heb")]
    [InlineData("HE")]
    [InlineData("iw")]
    [InlineData("Hebrew")]
    public void Match_PreferredLanguageInOtherForm_MatchesLanguageSuffix(string preferred)
    {
        var result = Names("Movie.mkv", new[] { "Movie.srt", "Movie.he.srt" }, preferred);

        Assert.Equal(new[] { "Movie.he.srt", "Movie.srt" }, result);
    }

    [Fact]
    public void Match_PreferredLanguageSuffixIsSecondPart_IsNotInPreferredGroup()
    {
        var result = Names("Movie.mkv", new[] { "Movie.forced.he.srt", "Movie.fr.srt" }, "he");

        Assert.Equal(new[] { "Movie.fr.srt", "Movie.forced.he.srt" }, result);
    }

    [Fact]
    public void Match_SameGroupDifferentPartCount_FewerPartsFirst()
    {
        var result = Names("Movie.mkv", new[] { "Movie.he.forced.srt", "Movie.he.srt" }, "he");

        Assert.Equal(new[] { "Movie.he.srt", "Movie.he.forced.srt" }, result);
    }

    [Fact]
    public void Match_RestGroupDifferentPartCount_FewerPartsFirst()
    {
        var result = Names("Movie.mkv", new[] { "Movie.en.forced.srt", "Movie.fr.srt" }, "he");

        Assert.Equal(new[] { "Movie.fr.srt", "Movie.en.forced.srt" }, result);
    }

    [Fact]
    public void Match_SameNameWithDifferentExtensions_SrtThenAssThenSsa()
    {
        var result = Names("Movie.mkv", new[] { "Movie.ssa", "Movie.ass", "Movie.srt" });

        Assert.Equal(new[] { "Movie.srt", "Movie.ass", "Movie.ssa" }, result);
    }

    [Fact]
    public void Match_SamePartCountDifferentExtension_ExtensionDecidesBeforeName()
    {
        var result = Names("Movie.mkv", new[] { "Movie.aa.ass", "Movie.zz.srt" });

        Assert.Equal(new[] { "Movie.zz.srt", "Movie.aa.ass" }, result);
    }

    [Fact]
    public void Match_SamePartCountAndExtension_OrdersByNameIgnoringCase()
    {
        var result = Names("Movie.mkv", new[] { "Movie.fr.srt", "Movie.De.srt", "Movie.en.srt" });

        Assert.Equal(new[] { "Movie.De.srt", "Movie.en.srt", "Movie.fr.srt" }, result);
    }

    [Fact]
    public void Match_SameInputInAnyOrder_GivesSameOrder()
    {
        var names = new[] { "Movie.he.srt", "Movie.srt", "Movie.en.srt", "Movie.he.ass", "Movie.forced.srt" };

        var forward = Names("Movie.mkv", names, "he");
        var backward = Names("Movie.mkv", names.Reverse().ToArray(), "he");

        Assert.Equal(forward, backward);
    }

    // --- odd input ---

    [Fact]
    public void Match_EmptyVideoName_ReturnsEmpty()
    {
        Assert.Empty(Names("", new[] { "Movie.srt", ".srt" }));
    }

    [Fact]
    public void Match_EmptyCandidateList_ReturnsEmpty()
    {
        Assert.Empty(Names("Movie.mkv", Array.Empty<string>()));
    }

    [Fact]
    public void Match_EmptyAndDotOnlyCandidates_AreIgnored()
    {
        var result = Names("Movie.mkv", new[] { "", ".", "..", "...", ".srt", "Movie.srt" });

        Assert.Equal(new[] { "Movie.srt" }, result);
    }

    [Fact]
    public void Match_VideoNameIsOnlyAnExtension_DoesNotThrowAndFindsNothingElse()
    {
        Assert.Empty(Names(".mkv", new[] { "Other.srt", ".srt", "", "." }));
    }

    // --- Hebrew, spaces, brackets ---

    [Fact]
    public void Match_HebrewFileNames_PreferredHebrewSuffixFirst()
    {
        var result = SidecarMatcher.Match("סרט.mkv", new[] { "סרט.srt", "סרט.he.srt" }, "he");

        Assert.Equal(new[] { "סרט.he.srt", "סרט.srt" }, result.Select(m => m.FileName).ToArray());
        Assert.Equal("he", result[0].Language);
        Assert.Null(result[1].Language);
    }

    [Fact]
    public void Match_HebrewFileNamesWithoutPreference_NoSuffixFirst()
    {
        var result = Names("סרט.mkv", new[] { "סרט.he.srt", "סרט.srt" });

        Assert.Equal(new[] { "סרט.srt", "סרט.he.srt" }, result);
    }

    [Fact]
    public void Match_NamesWithSpacesAndBrackets_Match()
    {
        var result = SidecarMatcher.Match(
            "My Movie [1080p] (2019).mkv",
            new[] { "My Movie [1080p] (2019).en.srt", "My Movie [1080p] (2019).srt", "My Movie.srt" },
            "en");

        Assert.Equal(
            new[] { "My Movie [1080p] (2019).en.srt", "My Movie [1080p] (2019).srt" },
            result.Select(m => m.FileName).ToArray());
        Assert.Equal("en", result[0].Language);
    }
}
