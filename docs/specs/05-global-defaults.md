# Spec 05: Global defaults

Phase: 2. Agent: global-defaults-store. Contracts: `IGlobalDefaultsStore`, `GlobalDefaults`, `PlaybackSettingsResolver`.

## Requirements

1. **Content.** The fields of `GlobalDefaults`: speed, decoder, aspect, repeat, resume from last position, subtitles enabled, preferred audio and subtitle language, audio and subtitle delay, subtitle style, background audio, Picture-in-Picture on leave.
2. **Storage.** One row in the `settings` table, key `global_defaults`, JSON. Missing fields fall back to the record defaults.
3. **Separation.** Fully separate from per-file state. Saving defaults never touches `playback_state`, and playing a file never changes the defaults.
4. **Resolution order.** Per-file value first, then the global default. Implemented only in `PlaybackSettingsResolver`. No other code decides this.
5. **Settings page.** One page that edits every field with the right control (slider for speed, pickers for enums and languages, switches for booleans, a sub-page for subtitle style). Changes save immediately.
6. **Change event.** `Changed` fires after each save. Defaults apply to the next file that opens, not to the one currently playing.
7. **Reset.** A "reset to defaults" action restores the record defaults after a confirmation.
8. **Validation.** Speed 0.25-4.0, delays -5000 to +5000 ms. Out-of-range stored values are clamped on load.

## Acceptance

- Data tests: round trip, missing-field fallback, clamping, `Changed` raised once per save.
- Unit tests for the resolver: every field with and without a per-file value, finished file starts at 0, resume disabled starts at 0.
- Device: change a default, open a file with no saved state: the default applies. Open a file with its own value: the file's value wins.

## Out of scope

Per-folder defaults, profiles, export/import.
