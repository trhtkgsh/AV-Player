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
            ThemeTokens.ColorProgress,
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
    [InlineData(double.NaN, 0x99u)]
    [InlineData(double.PositiveInfinity, 0xFFu)]
    [InlineData(double.NegativeInfinity, 0x00u)]
    public void Resolve_HudOpacity_SetsHudBackgroundAlpha(double opacity, uint expectedAlpha)
    {
        var colors = ThemeResolver.Resolve(Settings(hudOpacity: opacity));

        Assert.Equal(expectedAlpha, colors[ThemeTokens.ColorHudBackground] >> 24);
    }

    [Theory]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    [InlineData(double.NaN)]
    public void Resolve_NonFiniteOpacity_DoesNotThrow(double opacity)
    {
        var colors = ThemeResolver.Resolve(Settings(hudOpacity: opacity));

        Assert.Equal(12, colors.Count);
    }

    [Theory]
    [InlineData(-0.001)]
    [InlineData(-100.0)]
    public void Resolve_OpacityBelowZero_GivesAlphaZero(double opacity)
    {
        var colors = ThemeResolver.Resolve(Settings(hudOpacity: opacity));

        Assert.Equal(0x00u, colors[ThemeTokens.ColorHudBackground] >> 24);
    }

    [Theory]
    [InlineData(1.001)]
    [InlineData(100.0)]
    public void Resolve_OpacityAboveOne_GivesAlphaFF(double opacity)
    {
        var colors = ThemeResolver.Resolve(Settings(hudOpacity: opacity));

        Assert.Equal(0xFFu, colors[ThemeTokens.ColorHudBackground] >> 24);
    }

    [Theory]
    [InlineData(ThemeId.DarkOled)]
    [InlineData(ThemeId.DarkSlate)]
    [InlineData(ThemeId.HighContrast)]
    public void Resolve_PaletteProgressEqualsPaletteAccent_ProgressFollowsChosenAccent(ThemeId theme)
    {
        var palette = ThemePalettes.Get(theme);
        Assert.Equal(palette[ThemeTokens.ColorAccent], palette[ThemeTokens.ColorProgress]);

        var colors = ThemeResolver.Resolve(Settings(theme));

        Assert.Equal(CustomAccent, colors[ThemeTokens.ColorProgress]);
    }

    [Fact]
    public void Resolve_LightTheme_ProgressKeepsPaletteValue()
    {
        var colors = ThemeResolver.Resolve(Settings(ThemeId.Light));

        Assert.Equal(ThemePalettes.Get(ThemeId.Light)[ThemeTokens.ColorProgress], colors[ThemeTokens.ColorProgress]);
        Assert.NotEqual(CustomAccent, colors[ThemeTokens.ColorProgress]);
    }

    [Theory]
    [InlineData(0x004C9AFFu)]
    [InlineData(0x804C9AFFu)]
    [InlineData(0xFF4C9AFFu)]
    public void Resolve_AccentWithAnyAlpha_ResolvesOpaque(uint accent)
    {
        var colors = ThemeResolver.Resolve(Settings(accent: accent));

        Assert.Equal(0xFF4C9AFFu, colors[ThemeTokens.ColorAccent]);
        Assert.Equal(0xFF4C9AFFu, colors[ThemeTokens.ColorProgress]);
    }

    public static TheoryData<uint> PaletteAccents()
    {
        var data = new TheoryData<uint>();
        foreach (var color in AccentPalette.Colors)
        {
            data.Add(color);
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(PaletteAccents))]
    public void Resolve_PaletteAccent_OnAccentIsBlackOrWhiteWithContrastAtLeast4Point5(uint accent)
    {
        var colors = ThemeResolver.Resolve(Settings(accent: accent));
        var onAccent = colors[ThemeTokens.ColorOnAccent];

        Assert.Contains(onAccent, new[] { 0xFF000000u, 0xFFFFFFFFu });
        Assert.True(ContrastCalculator.ContrastRatio(onAccent, accent) >= 4.5);
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
