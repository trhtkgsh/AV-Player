using Player.Core.Logic.Theming;
using Player.Core.Models;

namespace Player.Core.Tests.Theming;

public sealed class ThemeResolverTests
{
    private const uint CustomAccent = 0xFFE53935;

    public static TheoryData<ThemeId> AllThemes() => ThemePalettesTests.AllThemes();

    private static ThemeSettings Settings(
        ThemeId theme = ThemeId.DarkOled, uint accent = CustomAccent, double hudOpacity = 0.6) =>
        new() { Theme = theme, AccentColorArgb = accent, HudOpacity = hudOpacity };

    [Theory]
    [MemberData(nameof(AllThemes))]
    public void Resolve_AnyTheme_ReturnsAllTwelveTokens(ThemeId theme)
    {
        var colors = ThemeResolver.Resolve(Settings(theme));

        Assert.Equal(12, colors.Count);
        Assert.All(ThemeTokens.All, token => Assert.True(colors.ContainsKey(token), token));
    }

    [Theory]
    [MemberData(nameof(AllThemes))]
    public void Resolve_AnyTheme_AccentIsTheSettingsAccent(ThemeId theme)
    {
        var colors = ThemeResolver.Resolve(Settings(theme));

        Assert.Equal(CustomAccent, colors[ThemeTokens.ColorAccent]);
    }

    [Theory]
    [MemberData(nameof(AllThemes))]
    public void Resolve_AnyTheme_OnAccentIsReadableOnAccent(ThemeId theme)
    {
        var colors = ThemeResolver.Resolve(Settings(theme));

        Assert.Equal(ContrastCalculator.ReadableOn(CustomAccent), colors[ThemeTokens.ColorOnAccent]);
    }

    [Theory]
    [MemberData(nameof(AllThemes))]
    public void Resolve_AnyTheme_OtherTokensEqualPaletteValues(ThemeId theme)
    {
        var colors = ThemeResolver.Resolve(Settings(theme));
        var palette = ThemePalettes.Get(theme);

        var overridden = new[]
        {
            ThemeTokens.ColorAccent,
            ThemeTokens.ColorOnAccent,
            ThemeTokens.ColorHudBackground,
        };
        var different = ThemeTokens.All
            .Where(token => !overridden.Contains(token) && colors[token] != palette[token])
            .ToArray();

        Assert.Empty(different);
    }

    [Theory]
    [InlineData(0.5, 0x80u)]
    [InlineData(0.0, 0x00u)]
    [InlineData(1.0, 0xFFu)]
    [InlineData(1.7, 0xFFu)]
    [InlineData(-1.0, 0x00u)]
    [InlineData(double.NaN, 0x00u)]
    public void Resolve_HudOpacity_SetsHudBackgroundAlpha(double opacity, uint expectedAlpha)
    {
        var colors = ThemeResolver.Resolve(Settings(hudOpacity: opacity));

        Assert.Equal(expectedAlpha, colors[ThemeTokens.ColorHudBackground] >> 24);
    }

    [Theory]
    [MemberData(nameof(AllThemes))]
    public void Resolve_HudOpacity_KeepsPaletteRgb(ThemeId theme)
    {
        var colors = ThemeResolver.Resolve(Settings(theme, hudOpacity: 0.5));

        var paletteRgb = ThemePalettes.Get(theme)[ThemeTokens.ColorHudBackground] & 0x00FFFFFFu;
        Assert.Equal(paletteRgb, colors[ThemeTokens.ColorHudBackground] & 0x00FFFFFFu);
    }

    [Fact]
    public void Resolve_NullSettings_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => ThemeResolver.Resolve(null!));
    }
}
