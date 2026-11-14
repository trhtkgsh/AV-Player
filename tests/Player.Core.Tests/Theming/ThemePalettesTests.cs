using Player.Core.Logic.Theming;
using Player.Core.Models;

namespace Player.Core.Tests.Theming;

public sealed class ThemePalettesTests
{
    public static TheoryData<ThemeId> AllThemes()
    {
        var data = new TheoryData<ThemeId>();
        foreach (var id in Enum.GetValues<ThemeId>())
        {
            data.Add(id);
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(AllThemes))]
    public void Get_EveryThemeId_ReturnsPaletteForThatTheme(ThemeId id)
    {
        var palette = ThemePalettes.Get(id);

        Assert.NotNull(palette);
        Assert.Equal(id, palette.Id);
    }

    [Theory]
    [MemberData(nameof(AllThemes))]
    public void Get_EveryThemeId_PaletteDefinesEveryToken(ThemeId id)
    {
        var palette = ThemePalettes.Get(id);

        var missing = ThemeTokens.All.Where(token => !palette.Colors.ContainsKey(token)).ToArray();

        Assert.Empty(missing);
    }

    [Fact]
    public void Get_UnknownThemeId_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ThemePalettes.Get((ThemeId)999));
    }

    [Fact]
    public void All_Tokens_HasTwelveDistinctEntries()
    {
        Assert.Equal(12, ThemeTokens.All.Count);
        Assert.Equal(12, ThemeTokens.All.Distinct().Count());
    }

    [Fact]
    public void Get_DarkOled_BackgroundIsPureBlack()
    {
        Assert.Equal(0xFF000000u, ThemePalettes.Get(ThemeId.DarkOled)[ThemeTokens.ColorBackground]);
    }

    [Theory]
    [MemberData(nameof(AllThemes))]
    public void Get_EveryTheme_OnSurfaceOnBackgroundContrastIsAtLeast4Point5(ThemeId id)
    {
        var palette = ThemePalettes.Get(id);

        var ratio = ContrastCalculator.ContrastRatio(
            palette[ThemeTokens.ColorOnSurface], palette[ThemeTokens.ColorBackground]);

        Assert.True(ratio >= 4.5, $"{id}: {ratio}");
    }

    [Theory]
    [MemberData(nameof(AllThemes))]
    public void Get_EveryTheme_OnSurfaceOnSurfaceContrastIsAtLeast4Point5(ThemeId id)
    {
        var palette = ThemePalettes.Get(id);

        var ratio = ContrastCalculator.ContrastRatio(
            palette[ThemeTokens.ColorOnSurface], palette[ThemeTokens.ColorSurface]);

        Assert.True(ratio >= 4.5, $"{id}: {ratio}");
    }

    [Fact]
    public void Get_HighContrast_OnSurfaceOnBackgroundContrastIsAtLeast7()
    {
        var palette = ThemePalettes.Get(ThemeId.HighContrast);

        var ratio = ContrastCalculator.ContrastRatio(
            palette[ThemeTokens.ColorOnSurface], palette[ThemeTokens.ColorBackground]);

        Assert.True(ratio >= 7.0, $"{ratio}");
    }

    [Theory]
    [MemberData(nameof(AllThemes))]
    public void Get_EveryTheme_OnAccentOnAccentContrastIsAtLeast3(ThemeId id)
    {
        var palette = ThemePalettes.Get(id);

        var ratio = ContrastCalculator.ContrastRatio(
            palette[ThemeTokens.ColorOnAccent], palette[ThemeTokens.ColorAccent]);

        Assert.True(ratio >= 3.0, $"{id}: {ratio}");
    }

    [Theory]
    [MemberData(nameof(AllThemes))]
    public void Get_EveryTheme_HudTextOnOpaqueHudBackgroundContrastIsAtLeast4Point5(ThemeId id)
    {
        var palette = ThemePalettes.Get(id);
        var opaqueHud = palette[ThemeTokens.ColorHudBackground] | 0xFF000000u;

        var ratio = ContrastCalculator.ContrastRatio(palette[ThemeTokens.ColorHudOnBackground], opaqueHud);

        Assert.True(ratio >= 4.5, $"{id}: {ratio}");
    }
}
