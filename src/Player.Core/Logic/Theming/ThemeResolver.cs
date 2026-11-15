using Player.Core.Models;

namespace Player.Core.Logic.Theming;

/// <summary>Turns the saved settings into the final token-to-ARGB map.</summary>
public static class ThemeResolver
{
    public static IReadOnlyDictionary<string, uint> Resolve(ThemeSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var palette = ThemePalettes.Get(settings.Theme);
        var accent = settings.AccentColorArgb | 0xFF000000u;   // any ARGB is accepted, always opaque
        var colors = new Dictionary<string, uint>(palette.Colors)
        {
            [ThemeTokens.ColorAccent] = accent,
            [ThemeTokens.ColorOnAccent] = ContrastCalculator.ReadableOn(accent),
        };

        // A palette whose progress color equals its own accent follows the chosen accent.
        if (palette[ThemeTokens.ColorProgress] == palette[ThemeTokens.ColorAccent])
        {
            colors[ThemeTokens.ColorProgress] = accent;
        }

        var opacity = ThemeSettingsNormalizer.NormalizeOpacity(settings.HudOpacity);
        var alpha = (uint)Math.Round(opacity * 255);
        colors[ThemeTokens.ColorHudBackground] =
            (alpha << 24) | (colors[ThemeTokens.ColorHudBackground] & 0x00FFFFFF);

        return colors.AsReadOnly();
    }
}
