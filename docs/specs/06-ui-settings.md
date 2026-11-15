# Spec 06: UI settings

Phase: 2. Agent: ui-settings. Contracts: `IUiSettingsStore`, `UiSettings`.

## Requirements

1. **Content.** The fields of `UiSettings`: HUD auto-hide time, double-tap skip length, clock in HUD, battery in HUD, explorer info fields, explorer view mode, sort field and direction, NEW tag age.
2. **Storage.** One row in the `settings` table, key `ui_settings`, JSON. Missing fields fall back to the record defaults.
3. **Settings page.** HUD hide time: 1-10 s. Skip length: 5, 10, 15, 30, 60 s. Switches for clock and battery. A checkbox list for the explorer fields. NEW tag age: 1-30 days.
4. **Live effect.** `Changed` fires after each save. The explorer and the HUD react without an app restart.
5. **Ownership of values.** View mode and sort are written by the explorer through this store. They are not shown on the settings page.
6. **Validation.** Out-of-range stored values are clamped on load.
7. **Separation.** This store holds no playback values. Those belong to global defaults (spec 05).

## Acceptance

- Data tests: round trip, missing-field fallback, clamping, `Changed` raised once per save.
- Device: change hide time, return to the player: it applies. Toggle an explorer field: the list rows update.
- Device, Phase 3 (the skip length and the battery switch are hidden on the page until the gestures and the battery indicator exist): change skip length, return to the player: it applies.

## Out of scope

Gesture customization, button layout editing. (Language selection was out of scope until Phase 3; it is now part of `UiSettings`, see docs/specs/10-localization.md.)
