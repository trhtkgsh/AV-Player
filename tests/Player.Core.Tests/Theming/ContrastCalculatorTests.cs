using Player.Core.Logic.Theming;

namespace Player.Core.Tests.Theming;

public sealed class ContrastCalculatorTests
{
    private const uint Black = 0xFF000000;
    private const uint White = 0xFFFFFFFF;

    [Fact]
    public void ContrastRatio_BlackAgainstWhite_Is21()
    {
        Assert.Equal(21.0, ContrastCalculator.ContrastRatio(Black, White), 2);
    }

    [Theory]
    [InlineData(0xFF000000u)]
    [InlineData(0xFFFFFFFFu)]
    [InlineData(0xFF4C9AFFu)]
    public void ContrastRatio_ColorAgainstItself_Is1(uint color)
    {
        Assert.Equal(1.0, ContrastCalculator.ContrastRatio(color, color), 6);
    }

    [Fact]
    public void ContrastRatio_ArgumentsSwapped_GivesSameRatio()
    {
        const uint first = 0xFF102040;
        const uint second = 0xFFFFE080;

        Assert.Equal(
            ContrastCalculator.ContrastRatio(first, second),
            ContrastCalculator.ContrastRatio(second, first),
            10);
    }

    [Fact]
    public void ContrastRatio_AlphaDiffers_AlphaIsIgnored()
    {
        Assert.Equal(1.0, ContrastCalculator.ContrastRatio(0x80FFFFFF, White), 6);
    }

    [Fact]
    public void RelativeLuminance_Black_IsZero()
    {
        Assert.Equal(0.0, ContrastCalculator.RelativeLuminance(Black), 6);
    }

    [Fact]
    public void RelativeLuminance_White_IsOne()
    {
        Assert.Equal(1.0, ContrastCalculator.RelativeLuminance(White), 6);
    }

    [Fact]
    public void ReadableOn_DarkBackground_ReturnsWhite()
    {
        Assert.Equal(White, ContrastCalculator.ReadableOn(0xFF102040));
    }

    [Fact]
    public void ReadableOn_LightBackground_ReturnsBlack()
    {
        Assert.Equal(Black, ContrastCalculator.ReadableOn(0xFFFFE080));
    }

    [Theory]
    [InlineData(0xFF4C9AFFu)]
    [InlineData(0xFFFFD400u)]
    [InlineData(0xFFE53935u)]
    [InlineData(0xFF43A047u)]
    [InlineData(0xFF8E24AAu)]
    [InlineData(0xFF808080u)]
    public void ReadableOn_AccentColor_ResultHasContrastAtLeast4Point5(uint accent)
    {
        var readable = ContrastCalculator.ReadableOn(accent);

        var ratio = ContrastCalculator.ContrastRatio(readable, accent);

        Assert.True(ratio >= 4.5, $"{accent:X8}: {ratio}");
    }
}
