using Player.Core.Models;

namespace Player.Core.Logic.Theming;

/// <summary>The four built-in palettes. Original colors. The accent entries are defaults; the resolver overrides them.</summary>
public static class ThemePalettes
{
    private static readonly ThemePalette DarkOled = Create(ThemeId.DarkOled,
        background: 0xFF000000, surface: 0xFF111419, onSurface: 0xFFE9ECF1, muted: 0xFF9BA3AF,
        accent: 0xFF4C9AFF, onAccent: 0xFF000000, divider: 0xFF262B33, progress: 0xFF4C9AFF,
        watched: 0xFF4CD58A, danger: 0xFFFF6B6B, hudBackground: 0x99000000, hudOn: 0xFFFFFFFF);

    private static readonly ThemePalette DarkSlate = Create(ThemeId.DarkSlate,
        background: 0xFF1B2330, surface: 0xFF263143, onSurface: 0xFFE6EBF3, muted: 0xFFA7B2C5,
        accent: 0xFF4C9AFF, onAccent: 0xFF000000, divider: 0xFF3A4860, progress: 0xFF4C9AFF,
        watched: 0xFF55CF8E, danger: 0xFFFF7070, hudBackground: 0x99141B26, hudOn: 0xFFFFFFFF);

    private static readonly ThemePalette Light = Create(ThemeId.Light,
        background: 0xFFF5F6F8, surface: 0xFFFFFFFF, onSurface: 0xFF181C22, muted: 0xFF586170,
        accent: 0xFF4C9AFF, onAccent: 0xFF000000, divider: 0xFFD3D8E0, progress: 0xFF1F6FD6,
        watched: 0xFF1B7F4A, danger: 0xFFB3261E, hudBackground: 0x99000000, hudOn: 0xFFFFFFFF);

    private static readonly ThemePalette HighContrast = Create(ThemeId.HighContrast,
        background: 0xFF000000, surface: 0xFF0B0B0B, onSurface: 0xFFFFFFFF, muted: 0xFFE2E2E2,
        accent: 0xFFFFD400, onAccent: 0xFF000000, divider: 0xFFFFFFFF, progress: 0xFFFFD400,
        watched: 0xFF7CFFB2, danger: 0xFFFF9A9A, hudBackground: 0xCC000000, hudOn: 0xFFFFFFFF);

    public static ThemePalette Get(ThemeId id) => id switch
    {
        ThemeId.DarkOled => DarkOled,
        ThemeId.DarkSlate => DarkSlate,
        ThemeId.Light => Light,
        ThemeId.HighContrast => HighContrast,
        _ => throw new ArgumentOutOfRangeException(nameof(id), id, "Unknown theme."),
    };

    private static ThemePalette Create(
        ThemeId id, uint background, uint surface, uint onSurface, uint muted, uint accent, uint onAccent,
        uint divider, uint progress, uint watched, uint danger, uint hudBackground, uint hudOn)
    {
        var colors = new Dictionary<string, uint>
        {
            [ThemeTokens.ColorBackground] = background,
            [ThemeTokens.ColorSurface] = surface,
            [ThemeTokens.ColorOnSurface] = onSurface,
            [ThemeTokens.ColorOnSurfaceMuted] = muted,
            [ThemeTokens.ColorAccent] = accent,
            [ThemeTokens.ColorOnAccent] = onAccent,
            [ThemeTokens.ColorDivider] = divider,
            [ThemeTokens.ColorProgress] = progress,
            [ThemeTokens.ColorWatched] = watched,
            [ThemeTokens.ColorDanger] = danger,
            [ThemeTokens.ColorHudBackground] = hudBackground,
            [ThemeTokens.ColorHudOnBackground] = hudOn,
        };
        return new ThemePalette(id, colors.AsReadOnly());
    }
}
