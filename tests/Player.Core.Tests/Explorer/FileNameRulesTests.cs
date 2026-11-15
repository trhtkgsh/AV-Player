using Player.Core.Logic.Explorer;

namespace Player.Core.Tests.Explorer;

public sealed class FileNameRulesTests
{
    [Fact]
    public void Split_PlainName_SplitsBaseAndExtension()
    {
        Assert.Equal(("movie", ".mp4"), FileNameRules.Split("movie.mp4"));
    }

    [Fact]
    public void Split_SeveralDots_SplitsAtLastDot()
    {
        Assert.Equal(("a.b.c", ".mkv"), FileNameRules.Split("a.b.c.mkv"));
    }

    [Fact]
    public void Split_NoExtension_ReturnsWholeNameAndEmptyExtension()
    {
        Assert.Equal(("movie", ""), FileNameRules.Split("movie"));
    }

    [Fact]
    public void Split_LeadingDotOnly_IsNameWithoutExtension()
    {
        Assert.Equal((".hidden", ""), FileNameRules.Split(".hidden"));
    }

    [Fact]
    public void Split_LeadingDotAndSecondDot_SplitsAtSecondDot()
    {
        Assert.Equal((".hidden", ".txt"), FileNameRules.Split(".hidden.txt"));
    }

    [Fact]
    public void Split_Empty_ReturnsEmptyParts()
    {
        Assert.Equal(("", ""), FileNameRules.Split(""));
    }

    [Fact]
    public void Split_Null_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => FileNameRules.Split(null!));
    }

    [Fact]
    public void BuildName_SurroundingSpaces_AreTrimmed()
    {
        Assert.Equal("clip.mp4", FileNameRules.BuildName("  clip  ", ".mp4"));
    }

    [Fact]
    public void BuildName_TrailingDotsAndSpaces_AreStripped()
    {
        Assert.Equal("clip.mp4", FileNameRules.BuildName("clip. . ..", ".mp4"));
    }

    [Fact]
    public void BuildName_LeadingDot_IsKept()
    {
        Assert.Equal(".clip.mp4", FileNameRules.BuildName(".clip", ".mp4"));
    }

    [Fact]
    public void BuildName_EmptyExtension_ReturnsOnlyBaseName()
    {
        Assert.Equal("clip", FileNameRules.BuildName("clip.", ""));
    }

    [Fact]
    public void BuildName_InnerSpaces_AreKept()
    {
        Assert.Equal("my clip.mp4", FileNameRules.BuildName("my clip", ".mp4"));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   \t ")]
    public void Check_EmptyOrWhitespace_IsEmpty(string typed)
    {
        Assert.Equal(FileNameProblem.Empty, FileNameRules.Check(typed, ".mp4", "old.mp4"));
    }

    [Theory]
    [InlineData("...")]
    [InlineData(". . .")]
    public void Check_NameThatStripsToEmpty_IsEmpty(string typed)
    {
        Assert.Equal(FileNameProblem.Empty, FileNameRules.Check(typed, ".mp4", "old.mp4"));
    }

    [Theory]
    [InlineData("/")]
    [InlineData("\\")]
    [InlineData(":")]
    [InlineData("*")]
    [InlineData("?")]
    [InlineData("\"")]
    [InlineData("<")]
    [InlineData(">")]
    [InlineData("|")]
    public void Check_ForbiddenCharacter_IsInvalidCharacter(string character)
    {
        Assert.Equal(FileNameProblem.InvalidCharacter, FileNameRules.Check("a" + character + "b", ".mp4", "old.mp4"));
    }

    [Theory]
    [InlineData(".")]
    [InlineData("..")]
    [InlineData(" . ")]
    [InlineData(" .. ")]
    public void Check_DotOrDoubleDot_IsReserved(string typed)
    {
        Assert.Equal(FileNameProblem.Reserved, FileNameRules.Check(typed, ".mp4", "old.mp4"));
    }

    [Fact]
    public void Check_FullNameOfExactlyMaxLength_IsNone()
    {
        var typed = new string('a', FileNameRules.MaxLength - ".mp4".Length);

        Assert.Equal(FileNameProblem.None, FileNameRules.Check(typed, ".mp4", "old.mp4"));
    }

    [Fact]
    public void Check_FullNameOneOverMaxLength_IsTooLong()
    {
        var typed = new string('a', FileNameRules.MaxLength - ".mp4".Length + 1);

        Assert.Equal(FileNameProblem.TooLong, FileNameRules.Check(typed, ".mp4", "old.mp4"));
    }

    [Fact]
    public void Check_NoExtensionAtMaxLength_IsNone()
    {
        Assert.Equal(FileNameProblem.None, FileNameRules.Check(new string('a', 200), "", "old"));
    }

    [Fact]
    public void Check_NoExtensionOneOverMaxLength_IsTooLong()
    {
        Assert.Equal(FileNameProblem.TooLong, FileNameRules.Check(new string('a', 201), "", "old"));
    }

    [Fact]
    public void Check_TrailingSpacesBeyondLimit_AreNotCounted()
    {
        var typed = new string('a', 196) + "    ";

        Assert.Equal(FileNameProblem.None, FileNameRules.Check(typed, ".mp4", "old.mp4"));
    }

    [Fact]
    public void Check_SameNameAsCurrent_IsUnchanged()
    {
        Assert.Equal(FileNameProblem.Unchanged, FileNameRules.Check("movie", ".mp4", "movie.mp4"));
    }

    [Fact]
    public void Check_SameNameWithExtraSpacesAndDots_IsUnchanged()
    {
        Assert.Equal(FileNameProblem.Unchanged, FileNameRules.Check("  movie. ", ".mp4", "movie.mp4"));
    }

    [Fact]
    public void Check_DifferentName_IsNone()
    {
        Assert.Equal(FileNameProblem.None, FileNameRules.Check("other", ".mp4", "movie.mp4"));
    }

    [Fact]
    public void Check_NameDifferingOnlyInCase_IsNone_BecauseComparisonIsOrdinal()
    {
        Assert.Equal(FileNameProblem.None, FileNameRules.Check("MOVIE", ".mp4", "movie.mp4"));
    }

    [Fact]
    public void Check_EmptyTakesPrecedenceOverUnchanged()
    {
        Assert.Equal(FileNameProblem.Empty, FileNameRules.Check("", "", ""));
    }

    [Fact]
    public void Check_Null_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => FileNameRules.Check(null!, ".mp4", "a.mp4"));
        Assert.Throws<ArgumentNullException>(() => FileNameRules.Check("a", null!, "a.mp4"));
        Assert.Throws<ArgumentNullException>(() => FileNameRules.Check("a", ".mp4", null!));
    }
}
