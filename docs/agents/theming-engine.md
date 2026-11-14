# Agent: theming-engine

**Mission.** Provide the color tokens and shared styles, and swap the theme at runtime.

**Model and why.** Sonnet 5.5. Runtime resource replacement in MAUI and Android system-bar handling need framework knowledge.

**Phase.** 1: tokens, shared styles, Dark OLED applied at startup. 3: four themes, runtime swap, accent, HUD opacity, theme page, persistence.

**Owned paths.** `src/Player.App/Theming/**`, `src/Player.App/Resources/Styles/**`, `src/Player.Data/Theme/**`, `src/Player.Core/Logic/Theming/**`.

**Read-only contracts.** Implements `IThemeService`. Uses `ThemeSettings`, `ThemeId`, `IDbConnectionProvider`.

**Inputs.** docs/specs/07, docs/ARCHITECTURE.md section 4 (`settings` row `theme`), docs/CONTRACTS.md.

**Deliverables.** `ThemePalette` + `ThemePalettes` + `ContrastCalculator` (pure, in Core/Logic/Theming), `ThemeSettingsRepository` (Player.Data), `ThemeService`, shared `Styles.xaml`, `ThemePage` + ViewModel, `AddTheming()` DI extension.

**Steps.**
1. Define the twelve token keys from spec 07 as constants. Define one `ThemePalette` (token to ARGB) per `ThemeId` in Core/Logic/Theming. Original colors only.
2. `ContrastCalculator`: WCAG contrast ratio, and a readable on-accent color (black or white) for any accent.
3. `ThemeService.ApplyAsync`: write every token into `Application.Current.Resources` as a `Color` on the UI thread, apply the accent override and the HUD background alpha, then set status and navigation bar colors and icon brightness. Raise `Changed`. Save.
4. `InitializeAsync`: load the saved settings (defaults when missing) and apply before the first page appears.
5. `Styles.xaml`: shared styles for Page, Label, Button, Entry, Slider, Switch, CollectionView rows. `DynamicResource` only. No literal colors.
6. Phase 3: `ThemePage` with theme list, live preview, 12-color accent palette, HUD opacity slider. Changes apply immediately.
7. Repository: read and write the `theme` row as JSON.

**Acceptance.**
- Build green. Palettes and contrast covered by test-writer (all tokens present in all themes, 4.5:1 text contrast, 7:1 in High Contrast).
- No literal color in the owned XAML.
- Device: all four themes recolor open pages and system bars at once. The theme survives a restart with no flash.

**Out of scope.** Feature pages' layout. Following the system dark mode. Fonts and icons.

**Escalation.** Stop and report when a contract is missing or wrong, a file outside the owned paths is needed, an acceptance item cannot be met, or the same step failed twice. Never change a contract.

**Report format.** `STATUS: done | blocked`, `BRANCH`, `FILES`, `BUILD`, `TESTS`, `DEVICE CHECKLIST` (items for the owner), `DEVIATIONS`, `QUESTIONS`.
