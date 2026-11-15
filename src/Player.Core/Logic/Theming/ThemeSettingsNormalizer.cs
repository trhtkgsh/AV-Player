using Player.Core.Models;

namespace Player.Core.Logic.Theming;

/// <summary>Brings any <see cref="ThemeSettings"/> into the valid range. Pure.</summary>
public static class ThemeSettingsNormalizer
{
    /// <summary>The HUD opacity of a fresh install; it also replaces NaN. Equals the default of <see cref="ThemeSettings"/>.</summary>
    public const double DefaultHudOpacity = 0.6;

    /// <summary>A defined theme (otherwise Dark OLED), an opaque accent, and an opacity clamped to 0..1 (NaN gives the default).</summary>
    public static ThemeSettings Normalize(ThemeSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        return new ThemeSettings
        {
            Theme = Enum.IsDefined(settings.Theme) ? settings.Theme : ThemeId.DarkOled,
            AccentColorArgb = settings.AccentColorArgb | 0xFF000000u,
            HudOpacity = NormalizeOpacity(settings.HudOpacity),
        };
    }

    public static double NormalizeOpacity(double opacity) =>
        double.IsNaN(opacity) ? DefaultHudOpacity : Math.Clamp(opacity, 0, 1);
}
