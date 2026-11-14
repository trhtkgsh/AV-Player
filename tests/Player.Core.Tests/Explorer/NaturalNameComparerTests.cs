using Player.Core.Logic.Explorer;

namespace Player.Core.Tests.Explorer;

public sealed class NaturalNameComparerTests
{
    private static int Compare(string? x, string? y) => NaturalNameComparer.Instance.Compare(x, y);

    [Fact]
    public void Compare_NullAndNonNull_NullSortsFirst()
    {
        Assert.True(Compare(null, "a") < 0);
        Assert.True(Compare("a", null) > 0);
    }

    [Fact]
    public void Compare_BothNull_ReturnsZero()
    {
        Assert.Equal(0, Compare(null, null));
    }

    [Fact]
    public void Compare_EmptyAndNonEmpty_EmptySortsFirst()
    {
        Assert.True(Compare("", "a") < 0);
    }

    [Fact]
    public void Compare_DigitRunsOfDifferentLength_ComparesNumericValue()
    {
        Assert.True(Compare("ep2", "ep10") < 0);
    }

    [Fact]
    public void Compare_FileNamesWithNumbers_ComparesNumericValue()
    {
        Assert.True(Compare("file2.mp4", "file10.mp4") < 0);
    }

    [Fact]
    public void Compare_TrailingNumbers_ComparesNumericValue()
    {
        Assert.True(Compare("a10", "a9") > 0);
    }

    [Fact]
    public void Compare_SameTextDifferentCase_ReturnsZero()
    {
        Assert.Equal(0, Compare("a1", "A1"));
    }

    [Fact]
    public void Compare_PrefixIsShorter_PrefixSortsFirst()
    {
        Assert.True(Compare("a", "a1") < 0);
    }

    [Fact]
    public void Compare_DifferentLetterAtSamePosition_OrdersAlphabetically()
    {
        Assert.True(Compare("abc", "abd") < 0);
    }

    [Fact]
    public void Compare_LeadingZerosOnSameNumber_ReturnsNonZero()
    {
        Assert.True(Compare("a1", "a01") < 0);
    }

    [Fact]
    public void Compare_LeadingZerosReversed_ReturnsOppositeSign()
    {
        Assert.True(Compare("a01", "a1") > 0);
    }

    [Fact]
    public void Compare_NumericRunsTieThenLetterDiffers_LetterDecides()
    {
        Assert.True(Compare("a01b", "a1a") > 0);
    }

    [Fact]
    public void Compare_DigitRunsLongerThan20Digits_OrdersByLastDigit()
    {
        var prefix = "file" + new string('9', 29);

        Assert.True(Compare(prefix + "1", prefix + "2") < 0);
        Assert.True(Compare(prefix + "2", prefix + "1") > 0);
    }

    [Theory]
    [InlineData("ep2", "ep10")]
    [InlineData("file2.mp4", "file10.mp4")]
    [InlineData("a10", "a9")]
    [InlineData("a1", "A1")]
    [InlineData("a", "a1")]
    [InlineData("abc", "abd")]
    [InlineData("a1", "a01")]
    [InlineData("a01b", "a1a")]
    [InlineData("", "a")]
    [InlineData(null, "a")]
    public void Compare_AnyPair_IsAntisymmetric(string? x, string? y)
    {
        Assert.Equal(Math.Sign(Compare(x, y)), -Math.Sign(Compare(y, x)));
    }

    [Fact]
    public void Compare_MixedNames_SortsInNaturalOrder()
    {
        var sorted = new[] { "ep10", "ep2", "Ep1", "ep01" }
            .OrderBy(name => name, NaturalNameComparer.Instance)
            .ToArray();

        Assert.Equal(new[] { "Ep1", "ep01", "ep2", "ep10" }, sorted);
    }
}
