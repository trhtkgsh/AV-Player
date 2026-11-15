using Player.Core.Models;

namespace Player.Core.Logic.Theming;

/// <summary>Tells whether a theme is dark, decided from its palette and not from a list of ids.</summary>
public static class ThemeBrightness
{
    private const double DarkLuminanceLimit = 0.5;

    public static bool IsDark(ThemeId id) =>
        ContrastCalculator.RelativeLuminance(ThemePalettes.Get(id).Colors[ThemeTokens.ColorBackground]) < DarkLuminanceLimit;
}
