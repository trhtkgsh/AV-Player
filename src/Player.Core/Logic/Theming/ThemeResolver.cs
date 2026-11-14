using Player.Core.Models;

namespace Player.Core.Logic.Theming;

/// <summary>Turns the saved settings into the final token-to-ARGB map.</summary>
public static class ThemeResolver
{
    public static IReadOnlyDictionary<string, uint> Resolve(ThemeSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var colors = new Dictionary<string, uint>(ThemePalettes.Get(settings.Theme).Colors)
        {
            [ThemeTokens.ColorAccent] = settings.AccentColorArgb,
            [ThemeTokens.ColorOnAccent] = ContrastCalculator.ReadableOn(settings.AccentColorArgb),
        };

        var opacity = double.IsNaN(settings.HudOpacity) ? 0 : Math.Clamp(settings.HudOpacity, 0, 1);
        var alpha = (uint)Math.Round(opacity * 255);
        colors[ThemeTokens.ColorHudBackground] =
            (alpha << 24) | (colors[ThemeTokens.ColorHudBackground] & 0x00FFFFFF);

        return colors.AsReadOnly();
    }
}
