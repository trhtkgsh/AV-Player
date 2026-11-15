using Player.Core.Logic.Theming;
using Player.Core.Models;

namespace Player.Core.Tests.Theming;

public sealed class ThemeSettingsNormalizerTests
{
    public static TheoryData<ThemeId> AllThemes() => ThemePalettesTests.AllThemes();

    [Fact]
    public void Colors_Always_HasTwelveEntries()
    {
        Assert.Equal(12, AccentPalette.Colors.Count);
    }

    [Fact]
    public void Colors_Always_AreDistinct()
    {
        Assert.Equal(AccentPalette.Colors.Count, AccentPalette.Colors.Distinct().Count());
    }

    [Fact]
    public void Colors_Always_AreFullyOpaque()
    {
        Assert.All(AccentPalette.Colors, color => Assert.Equal(0xFFu, color >> 24));
    }

    [Fact]
    public void Colors_Always_ContainsDefault()
    {
        Assert.Contains(AccentPalette.Default, AccentPalette.Colors);
    }

    [Fact]
    public void Default_Always_EqualsThemeSettingsDefaultAccent()
    {
        Assert.Equal(0xFF4C9AFFu, AccentPalette.Default);
        Assert.Equal(AccentPalette.Default, new ThemeSettings().AccentColorArgb);
    }

    [Fact]
    public void DefaultHudOpacity_Always_EqualsThemeSettingsDefaultOpacity()
    {
        Assert.Equal(0.6, ThemeSettingsNormalizer.DefaultHudOpacity);
        Assert.Equal(ThemeSettingsNormalizer.DefaultHudOpacity, new ThemeSettings().HudOpacity);
    }

    [Theory]
    [InlineData(4)]
    [InlineData(-1)]
    [InlineData(99)]
    [InlineData(int.MaxValue)]
    [InlineData(int.MinValue)]
    public void Normalize_UndefinedTheme_BecomesDarkOled(int raw)
    {
        var result = ThemeSettingsNormalizer.Normalize(new ThemeSettings { Theme = (ThemeId)raw });

        Assert.Equal(ThemeId.DarkOled, result.Theme);
    }

    [Theory]
    [MemberData(nameof(AllThemes))]
    public void Normalize_DefinedTheme_IsKept(ThemeId theme)
    {
        var result = ThemeSettingsNormalizer.Normalize(new ThemeSettings { Theme = theme });

        Assert.Equal(theme, result.Theme);
    }

    [Theory]
    [InlineData(0x004C9AFFu, 0xFF4C9AFFu)]
    [InlineData(0x804C9AFFu, 0xFF4C9AFFu)]
    [InlineData(0xFF4C9AFFu, 0xFF4C9AFFu)]
    [InlineData(0x00000000u, 0xFF000000u)]
    [InlineData(0x00FFFFFFu, 0xFFFFFFFFu)]
    public void Normalize_Accent_BecomesOpaque(uint accent, uint expected)
    {
        var result = ThemeSettingsNormalizer.Normalize(new ThemeSettings { AccentColorArgb = accent });

        Assert.Equal(expected, result.AccentColorArgb);
    }

    [Theory]
    [InlineData(-0.5, 0.0)]
    [InlineData(-1000.0, 0.0)]
    [InlineData(1.5, 1.0)]
    [InlineData(1000.0, 1.0)]
    [InlineData(double.NegativeInfinity, 0.0)]
    [InlineData(double.PositiveInfinity, 1.0)]
    public void Normalize_OpacityOutOfRange_IsClamped(double opacity, double expected)
    {
        var result = ThemeSettingsNormalizer.Normalize(new ThemeSettings { HudOpacity = opacity });

        Assert.Equal(expected, result.HudOpacity);
    }

    [Fact]
    public void Normalize_NaNOpacity_BecomesDefaultHudOpacity()
    {
        var result = ThemeSettingsNormalizer.Normalize(new ThemeSettings { HudOpacity = double.NaN });

        Assert.Equal(ThemeSettingsNormalizer.DefaultHudOpacity, result.HudOpacity);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(0.25)]
    [InlineData(0.6)]
    [InlineData(1.0)]
    public void Normalize_OpacityInRange_PassesThroughUnchanged(double opacity)
    {
        var result = ThemeSettingsNormalizer.Normalize(new ThemeSettings { HudOpacity = opacity });

        Assert.Equal(opacity, result.HudOpacity);
    }

    [Fact]
    public void Normalize_ValidSettings_ReturnsEqualSettings()
    {
        var settings = new ThemeSettings { Theme = ThemeId.Light, AccentColorArgb = 0xFFB07CFF, HudOpacity = 0.35 };

        Assert.Equal(settings, ThemeSettingsNormalizer.Normalize(settings));
    }

    [Fact]
    public void Normalize_DefaultSettings_ReturnsDefaultSettings()
    {
        Assert.Equal(new ThemeSettings(), ThemeSettingsNormalizer.Normalize(new ThemeSettings()));
    }

    [Theory]
    [InlineData(99, 0x004C9AFFu, 7.0)]
    [InlineData(2, 0x80123456u, -3.0)]
    [InlineData(1, 0xFF000000u, double.NaN)]
    [InlineData(-5, 0u, double.PositiveInfinity)]
    public void Normalize_TwiceOnAnyInput_EqualsNormalizingOnce(int theme, uint accent, double opacity)
    {
        var settings = new ThemeSettings { Theme = (ThemeId)theme, AccentColorArgb = accent, HudOpacity = opacity };

        var once = ThemeSettingsNormalizer.Normalize(settings);
        var twice = ThemeSettingsNormalizer.Normalize(once);

        Assert.Equal(once, twice);
    }

    [Fact]
    public void Normalize_NullSettings_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => ThemeSettingsNormalizer.Normalize(null!));
    }

    [Fact]
    public void Normalize_Input_IsNotMutated()
    {
        var settings = new ThemeSettings { Theme = (ThemeId)99, AccentColorArgb = 0x004C9AFF, HudOpacity = 5 };

        _ = ThemeSettingsNormalizer.Normalize(settings);

        Assert.Equal((ThemeId)99, settings.Theme);
        Assert.Equal(0x004C9AFFu, settings.AccentColorArgb);
        Assert.Equal(5.0, settings.HudOpacity);
    }
}
