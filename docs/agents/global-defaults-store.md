# Agent: global-defaults-store

**Mission.** Store the global playback defaults and provide the page that edits them.

**Model and why.** Haiku 5.5. A JSON row in a given table and a settings page, fully specified here and in spec 05.

**Phase.** 2.

**Owned paths.** `src/Player.Data/GlobalDefaults/**`, `src/Player.App/Features/GlobalDefaults/**`.

**Read-only contracts.** Implements `IGlobalDefaultsStore`. Uses `IDbConnectionProvider`, `GlobalDefaults`, `SubtitleStyle`, the enums.

**Inputs.** docs/specs/05, docs/ARCHITECTURE.md section 4 (`settings` row `global_defaults`), docs/specs/07 (token keys), docs/CONTRACTS.md.

**Deliverables.** `GlobalDefaultsStore` (Player.Data), `GlobalDefaultsPage` + ViewModel, `DefaultSubtitleStylePage` + ViewModel, `AddGlobalDefaults()` DI extension (in Player.Data) and `AddGlobalDefaultsUi()` (in the feature folder) that registers the route `settings/defaults`.

**Steps.**
1. `GetAsync`: read the row with key `global_defaults`, deserialize with System.Text.Json, return `new GlobalDefaults()` when missing or invalid. Cache the value in memory after the first read.
2. Clamp on load: speed 0.25-4.0, delays -5000..+5000.
3. `SaveAsync`: clamp, upsert the row with `updated_utc`, update the cache, raise `Changed` once.
4. Page: speed slider, decoder picker, aspect picker, repeat picker, switches for resume, subtitles enabled, background audio and Picture-in-Picture on leave, text entries for preferred audio and subtitle language (ISO 639 code, optional), delay steppers, a row that opens the subtitle style page, and "Reset to defaults" with a confirmation.
5. Every control saves immediately through `SaveAsync` with the full updated record (`with` expression).
6. Never read or write `playback_state`. Never call the engine.

**Acceptance.**
- Build green. Store covered by test-writer against a real SQLite file. Reviewer approves.
- `[ObservableProperty]` and `[RelayCommand]` only. `DynamicResource` tokens only.
- Device: spec 05 acceptance list.

**Out of scope.** The resolver (core-contracts). Per-file state. UI settings. Applying defaults to playback (playback-coordinator).

**Escalation.** Stop and report when a contract is missing or wrong, a file outside the owned paths is needed, an acceptance item cannot be met, or the same step failed twice. Never change a contract.

**Report format.** `STATUS: done | blocked`, `BRANCH`, `FILES`, `BUILD`, `TESTS`, `DEVICE CHECKLIST` (items for the owner), `DEVIATIONS`, `QUESTIONS`.
