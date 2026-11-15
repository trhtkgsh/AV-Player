# Agent: ui-settings

**Mission.** Store the UI settings and provide the page that edits them.

**Model and why.** Haiku 5.5. A JSON row in a given table and a settings page, fully specified here and in spec 06.

**Phase.** 2.

**Owned paths.** `src/Player.Data/UiSettings/**`, `src/Player.App/Features/UiSettings/**`.

**Read-only contracts.** Implements `IUiSettingsStore`. Uses `IDbConnectionProvider`, `UiSettings`, `ExplorerInfoFields`.

**Inputs.** docs/specs/06, docs/ARCHITECTURE.md section 4 (`settings` row `ui_settings`), docs/specs/07 (token keys), docs/CONTRACTS.md.

**Deliverables.** `UiSettingsStore` (Player.Data), `UiSettingsPage` + ViewModel, `SettingsHomePage` (a plain list linking to the settings routes `settings-defaults`, `settings-ui`, and from Phase 3 `settings-theme`), `AddUiSettings()` DI extension (in Player.Data) and `AddUiSettingsUi()` (in the feature folder) that registers the routes `settings` and `settings-ui`.

**Decisions made in Phase 2 (they win over the steps below).** Route names are flat (no `/`). Namespaces: `Player.Data.UiSettingsStorage` and `Player.App.Features.InterfaceSettings`, because a namespace named like the model type hides it. The store reads and writes a private DTO with nullable fields and merges it onto `new UiSettings()`: the JSON source generator would reset missing fields. The page does not show `SkipSeconds` and `ShowBattery` until Phase 3; it keeps their stored values. Steppers are replaced by sliders that save on release.

**Steps.**
1. `GetAsync`: read the row with key `ui_settings`, deserialize with System.Text.Json, return `new UiSettings()` when missing or invalid. Cache the value in memory after the first read.
2. Clamp on load: `HudAutoHideSeconds` 1-10, `SkipSeconds` to the nearest of 5, 10, 15, 30, 60, `NewTagDays` 1-30.
3. `SaveAsync`: clamp, upsert the row with `updated_utc`, update the cache, raise `Changed` once.
4. `UiSettingsPage`: HUD hide time stepper, skip length picker, switches for clock and battery, a checkbox list for the explorer fields (duration, resolution, size, date, path), NEW tag age stepper.
5. Every control saves immediately through `SaveAsync` with the full updated record (`with` expression).
6. Do not show view mode or sort on the page. The explorer writes them.
7. `SettingsHomePage` only navigates. It holds no settings logic.

**Acceptance.**
- Build green. Store covered by test-writer against a real SQLite file. Reviewer approves.
- `[ObservableProperty]` and `[RelayCommand]` only. `DynamicResource` tokens only.
- Device: spec 06 acceptance list.

**Out of scope.** Playback defaults, theming page content, explorer and HUD reactions to the settings.

**Escalation.** Stop and report when a contract is missing or wrong, a file outside the owned paths is needed, an acceptance item cannot be met, or the same step failed twice. Never change a contract.

**Report format.** `STATUS: done | blocked`, `BRANCH`, `FILES`, `BUILD`, `TESTS`, `DEVICE CHECKLIST` (items for the owner), `DEVIATIONS`, `QUESTIONS`.
