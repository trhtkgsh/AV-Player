# Spec 07: Theming

Phase: 3. Agent: theming-engine. Contracts: `IThemeService`, `ThemeSettings`.

## Requirements

1. **Themes.** Dark OLED (pure black backgrounds), Dark Slate, Light, High Contrast. Original palettes. No assets or colors copied from another product.
2. **Runtime swap.** Choosing a theme replaces the active `ResourceDictionary` immediately. No app restart, no page reload. Open pages update.
3. **Tokens.** Every theme defines the same keys: `ColorBackground`, `ColorSurface`, `ColorOnSurface`, `ColorOnSurfaceMuted`, `ColorAccent`, `ColorOnAccent`, `ColorDivider`, `ColorProgress`, `ColorWatched`, `ColorDanger`, `ColorHudBackground`, `ColorHudOnBackground`. A unit check fails if a theme misses a key.
4. **Usage rule.** Pages use only `DynamicResource` with these keys and the shared styles. No hard-coded colors in feature XAML. The reviewer enforces this.
5. **Accent color.** The user picks from a fixed palette of 12 colors. It overrides `ColorAccent` (and a computed readable `ColorOnAccent`) in every theme.
6. **HUD opacity.** 0-100%. Applied to `ColorHudBackground` alpha.
7. **System bars.** Status and navigation bar colors and icon brightness follow the theme. The player page stays immersive.
8. **Persistence.** One row in the `settings` table, key `theme`. Applied in `InitializeAsync` before the first page is shown, so there is no flash of the wrong theme.
9. **Theme page.** Theme list with a live preview, accent palette, HUD opacity slider.
10. **Contrast.** Text on background reaches at least 4.5:1 in every theme, 7:1 in High Contrast.

## Acceptance

- Unit tests: all themes define all tokens, contrast ratios, readable on-accent color.
- Device: switch all four themes with the explorer and the player open: everything recolors at once, including system bars.

## Out of scope

Following the system dark mode, custom user themes, font selection, icon packs.
