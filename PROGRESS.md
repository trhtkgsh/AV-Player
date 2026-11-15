# PROGRESS

Tracking file required by docs/MASTER.md section 8. Updated by the orchestrator only.

## Current phase

Phase 2 (started 2026-11-15). The Phase 1 gate is passed: on 2026-11-15 the owner reported that
`ArielPlayer-phase1-r2.apk` installs and works on the phone and that every item of `docs/qa/phase-1-checklist.md`
was run and passed. The owner did not name the phone model or the Android version.
The roster was approved on 2026-11-14 and the HARD GATE was passed then.

Phase 1 APK: `artifacts/ArielPlayer-phase1-r2.apk` (Release, built from main at acf3f1c on 2026-11-15, 105 MB,
arm64-v8a, armeabi-v7a and x86_64, SHA-256 DEDAD755AB8A3AF9C178BFFF4BA01B7C34DA12AA612594600CE7E4242789B838). It replaces
`ArielPlayer-phase1.apk` (2826f17), which the owner could not install. The same file was run on two emulators: API 36 as
x86_64 and as arm64 (through the image's ARM translation), API 34 as x86_64. Checked: install, launcher icon, splash,
status bar color, explorer, thumbnails, playback, HUD symbols, pause and play, previous, next, back, decoder switch
(mediacodec to avcodec on arm64). The armeabi-v7a part has not run anywhere.

## Owner feedback on 2026-11-15 (first APK)

| # | Feedback | What was found | What was done | State |
|---|---|---|---|---|
| 1 | The app cannot be installed on the phone. | The owner gave no error text and no phone model. The first APK is valid: v2 and v3 signatures verify, minSdk 26, zip aligned, installs on API 34 and API 36. It contained only arm64-v8a and x86_64, so a phone that runs a 32-bit Android refuses it as not compatible. | P1-30 adds armeabi-v7a. The arm64 part was run for the first time (on the emulators' ARM translation) and works. | Closed: r2 installs on the owner's phone (reported 2026-11-15). The cause was not established: the added 32-bit ABI fits the result, but the owner sent no error text and no phone model, so it is not proven. |
| 2 | The player buttons look like ordinary buttons. They must be white symbols with no background. | The HUD used text buttons with the accent fill. | P1-29: vector symbols for back, previous, play or pause, next; the decoder is an outlined HW/SW label. | Done, checked on API 34 and API 36 in the Release build. |
| 3 | The app needs its own icon, on the launcher and on the splash screen. | Both were the purple ".NET" template images. | P1-30: a white play triangle on a blue gradient (launcher), the same symbol in a blue circle on black (splash). | Done, checked on API 34 and API 36. |
| 4 | The Android status bar must have the color of the app design. | It was purple: the template colors in `colors.xml`. | P1-30: `colorPrimary` and `colorPrimaryDark` are the Dark OLED surface color, `colorAccent` the accent blue. | Done for the one theme of Phase 1, checked on API 34 and API 36. Following a theme change at runtime stays a Phase 3 task (ARCHITECTURE.md section 5). |

## Gates

| Phase | Build | Tests | Reviewer | Owner device test | Status |
|---|---|---|---|---|---|
| 0 Spike | green (spike) | n/a | n/a | pending | go on emulator, waiting for owner device test (docs/SPIKE.md) |
| 1 | green (Debug and Release) | 349 pass (307 Core, 42 Data) | every merged task approved, except P1-27 (tests only, Sonnet, checked by the orchestrator) | passed on 2026-11-15 (owner report: all checklist items pass) | PASSED |
| 2 | - | - | - | - | in progress |
| 3 | - | - | - | - | not started |
| 4 | - | - | - | - | not started |
| 5 | - | - | - | - | not started |

## Section 9 checklist

| Step | Item | Status |
|---|---|---|
| 1 | Read MASTER.md | done |
| 2 | Repo structure, copilot-instructions, PROGRESS.md | done |
| 3 | Phase 0 spike and docs/SPIKE.md | done on emulator, owner device test pending |
| 4 | docs/ARCHITECTURE.md and frozen contracts | done, contracts frozen 2026-11-14 |
| 5 | docs/specs/01-09 and docs/agents/<id>.md | done: 9 specs, 23 agent specs, 23 `.github/agents/<id>.agent.md` with model set |
| 6 | Roster shown to owner, approval | approved 2026-11-14 |
| 7 | Start Phase 1 | in progress |

## Delegation mechanism

The custom agents in `.github/agents` are available in the `task` tool (they appeared after the session was
resumed on 2026-11-14 evening; P1-03 to P1-07 were first launched through `general-purpose` with the roster
model set explicitly, before that). Tasks are delegated to the custom agent by name, without a model override.
The brief is the agent's spec file plus the contracts and acceptance criteria. Fable-marked agents are done by
the orchestrator.

## Phase 1 task log

| # | Agent | Model | Branch | Attempts | Result |
|---|---|---|---|---|---|
| P1-01 | app-skeleton | Haiku 5.5 | task/p1-01-app-skeleton | 1 | merged. Reviewer: approve. Build and tests green. Brief error by orchestrator: manifest check must use the Release build. |
| P1-02 | core-contracts | Fable 5.2 (orchestrator) | task/p1-02-core-contracts | 1 | merged. Player.Core builds with 0 warnings and no references. Reviewer pass deferred to the phase gate. |
| P1-03 | app-skeleton | Haiku 5.5 | task/p1-03-app-skeleton | 1 (first run interrupted by a session stop, resumed from its partial work) | merged. Reviewer: approve. |
| P1-04 | media-library | Sonnet 5.5 | task/p1-04-media-library | 1 | merged. Follow-up commit declares READ_MEDIA_VISUAL_USER_SELECTED (orchestrator decision). Not reviewed yet (gate). |
| P1-05 | thumbnail-cache | Sonnet 5.5 | task/p1-05-thumbnail-cache | 1 | merged. 477 lines (over the guideline). Not reviewed yet (gate). |
| P1-06 | playback-coordinator | Sonnet 5.5 | task/p1-06-playback-coordinator | 1 | merged. 452 lines (over the guideline). Not reviewed yet (gate). |
| P1-07 | theming-engine | Sonnet 5.5 | task/p1-07-theming-engine | 1 | merged. Not reviewed yet (gate). |
| P1-08 | player-engine | Fable 5.2 (orchestrator) | task/p1-08-player-engine | 1 | merged. 730 lines (far over the guideline: queue, engine and binding do not compile separately). Reviewer: 2 rounds, 3 blocking findings, all fixed; the last fix was not re-reviewed. Never run on a device. |
| P1-09 | video-surface | Fable 5.2 (orchestrator) | task/p1-09-video-surface | 1 | merged. Geometry for all aspect modes and zoom/pan is in Core. Not reviewed, never run on a device. |
| P1-11 | explorer-ui | Haiku 5.5 | task/p1-11-explorer-logic | 1 | merged. Reviewer: approve (logic traced by hand, no tests yet). |
| P1-12 | player-hud-ui | Haiku 5.5 | task/p1-12-player-hud | 2 | merged. Reviewer: changes-required (unguarded close and error paths), approve after the fixes. |
| P1-13 | explorer-ui | Haiku 5.5 | task/p1-13-explorer-vm | 1 | merged. Reviewer: approve with non-blocking findings, fixed in P1-14 and P1-20. 613 lines. |
| P1-14 | explorer-ui | Haiku 5.5 | task/p1-14-explorer-pages | 1 | merged. Reviewer: approve with non-blocking findings (P1-20). |
| P1-15 | test-writer | Haiku 5.5 | task/p1-15-tests-core-a | 1 | merged. 91 tests pass. Reviewer: approve. 926 lines. |
| P1-16 | test-writer | Haiku 5.5, then Sonnet 5.5 | task/p1-16-tests-core-b | 3 on Haiku (all died on a model stream error with nothing written: the full task twice, then the small P1-16a). Escalated to Sonnet 5.5 for P1-16a, see the escalation log. | P1-16a running on Sonnet |
| P1-17 | app-skeleton | Haiku 5.5 | task/p1-17-wiring | 1 | merged. Reviewer: approve. Release manifest has no network permission. |
| P1-18 | app-skeleton | Haiku 5.5 | task/p1-18-shell-route | 1 | merged. One-line fix of an orchestrator design error (root route `explorer` shadowed `explorer/items`). Gate reviewer: approve. |
| P1-19 | player-engine | Fable 5.2 (orchestrator) | task/p1-19-engine-fixes | 1 | merged. Two defects found on the first emulator run (SPIKE.md findings 11 and 12). Gate reviewer: approve with non-blocking findings (P1-24). |
| P1-20 | explorer-ui | Haiku 5.5 | task/p1-20-explorer-polish | 1 | merged. Reviewer: approve. |
| P1-21 | theming-engine | Sonnet 5.5 | task/p1-21-shell-style | 1 | merged. Gate reviewer: approve. Verified on the emulator. |
| P1-22 | device-qa | Haiku 5.5 | task/p1-22-device-qa | 1 | merged. 60 items. The agent had no shell tool (orchestrator's definition error, fixed); the orchestrator created the folder and committed for it. |
| P1-23 | media-library | Sonnet 5.5 | task/p1-23-library-permissions | 1 | merged. Fixes the gate reviewer's changes-required (API 34 partial access re-requested on every load). Reviewer: approve. Not run on a device. |
| P1-24 | player-engine, video-surface | Fable 5.2 (orchestrator) | task/p1-24-engine-surface-fixes | 1 | merged. Gate review fixes, plus a paused black frame after backgrounding found while testing them (SPIKE.md finding 12). Emulator: 16 of 16 cycles. Not re-reviewed. |
| P1-25 | playback-coordinator | Sonnet 5.5 | task/p1-25-coordinator-fixes | 1 | merged. 11 fixes for the gate reviewer's findings. Reviewer: approve. Next, Prev (restart and previous item) checked on the emulator. |
| P1-16a/b | test-writer | Sonnet 5.5 (escalated) | task/p1-16-tests-core-b | 1 each | merged. Surface geometry, theming, info formatter, sorter and resolver gaps. No product bug found. |
| P1-26 | test-writer | Sonnet 5.5 | task/p1-26-tests-coordinator | 1 | merged. 77 coordinator test cases with fakes, 5 runs in a row green. No product bug found. Core total: 307. |
| P1-27 | test-writer | Sonnet 5.5 | task/p1-27-tests-data | 6 launches, 3 died on the model stream error (the first one silently, overnight) | merged. Split into three launches of one test file each, with a commit after each. 42 tests against a real SQLite file: `DbConnectionProvider` 12, `SettingsRepository` 13, `MediaMetadataMigration` and `MediaMetadataRepository` 17. No product bug found. No separate reviewer pass: the orchestrator read the tests and ran the whole suite. |
| P1-28 | player-engine, video-surface | Fable 5.2 (orchestrator) | task/p1-28-reveal-fixes | 1 | merged. Reviewer of P1-24: changes-required (a seek during a frame reveal), approve after the fix. Emulator: 10 of 10 and 6 of 6 paused background cycles, 10 rapid open/close cycles. |
| publish | orchestrator | - | main at 2826f17 | 3 | Release APK. Attempt 1 (`-r android-x64`) built an APK without LibVLC. Attempt 2 (`-p:RuntimeIdentifiers`) failed to build. Attempt 3 (no RID) is correct and verified on the emulator. See ARCHITECTURE.md section 5. |
| P1-29 | player-hud-ui | Haiku 5.5 | task/p1-29-hud-icons | 1 | merged. `HudIconButton` (a `Path` in a tappable grid) and the HUD layout. Reviewer: approve. Non-blocking, open: a null `IconColor` throws; the dim also runs when the command cannot execute; the decoder label's description hides "HW"/"SW" from TalkBack. |
| P1-30 | app-skeleton | Haiku 5.5 | task/p1-30-icon-splash-abi | 2 | merged. Icon and splash SVG, `colors.xml`, Release `RuntimeIdentifiers` with android-arm. Reviewer: changes-required (the comment in `colors.xml` was missing), fixed. |
| P1-31 | theming-engine | - | - | 0 | not started, moved to Phase 3. Runtime system bar colors are not needed while one theme exists; the API 35 finding is in `docs/agents/theming-engine.md` step 3. |
| publish r2 | orchestrator | - | main at acf3f1c | 1 | Release APK with three ABIs, 105 MB. Checked: `libvlc.so` in each ABI, no network permission, v2 and v3 signatures, runs on API 34 and API 36. |
| P1-32 | device-qa | Haiku 5.5 | task/p1-32-qa-update | 1 | merged. Checklist: 67 items (7 new: HUD-14, HUD-15, APP-3 to APP-5, THEME-2, THEME-3), wording matches the symbols, APK install line. The session again had no shell tool, so the orchestrator made the commit. Text only, read by the orchestrator, no reviewer pass. |

Gate review (2026-11-14, three reviewer runs on main at a21d967): media-library changes-required then approve (P1-23); thumbnail-cache approve; playback-coordinator approve; core-contracts approve (matches docs/CONTRACTS.md member by member); video-surface approve; player-engine fixes approve; theming-engine approve; shell route approve.

Parallel tasks run in git worktrees under `D:\Projects\Ariel Video Player.wt\<task>` so that each keeps its own branch.

## First run of the product (emulator, Pixel 5 AVD, API 34, 2026-11-14)

Verified by the orchestrator, evidence in `docs/evidence/phase1/`: start, permission dialogs, folder list, item list
with lazy thumbnails and natural sort, open a video, HUD show/hide/auto-hide, decoder HW -> SW -> HW with the position
kept, pause/play, slider seek, end of file -> next file, end of the last file, 6 background/foreground cycles with the
picture restored and hardware decoding kept, vertical video, audio file, 8 rapid open/close cycles, Back.

Defects found and fixed: shell route (P1-18), native library load thread and late picture after backgrounding (P1-19).
Not verified: anything on a physical device, HEVC, 4K, 500+ files, API levels other than 34, a Release build on a device.

Phase 1 tasks not finished: the owner's device test.

The stream error is not tied to the model: it hit test-writer on Haiku three times and on Sonnet four times (7 of
13 launches), and no other agent. Its cause is unknown. What worked: one test file per launch, launched in sync mode,
with a commit at the end, so a dead launch loses a few minutes and is simply repeated. A background launch that dies
does not always notify the orchestrator.

Lessons recorded for later briefs:
- An agent that builds with a relative path can build in the main checkout and corrupt its `obj` folder (a crash at startup that a clean build removed). Every brief now gives full paths for build commands.
- Reviewers that build at the same time need separate worktrees.
- A namespace must never have the name of a type that other code uses by its simple name. `Player.Core.Logic.PlaybackState`, `Player.Data.GlobalDefaults` and `Player.Data.UiSettingsStore` (same name as its class) each broke or would have broken sibling code with CS0118. Rule: `Player.Data.<Area>Storage`, `Player.Core.Logic.StateRecording`, `Player.App.Features.DefaultsSettings`, `Player.App.Features.InterfaceSettings`.
- The System.Text.Json source generator sets an `init` property, or a record constructor parameter, that is missing in the JSON to `default(T)`, not to the record's initializer. Checked with a scratch program on 2026-11-15: `{}` gave speed 0 and a null style. Stores never serialize the Core records: they use a private DTO with nullable fields and merge it onto `new T()`.
- Shell routes are flat names (`settings-ui`, `player-speed`). A registered route that starts with another route's name was resolved wrongly in Phase 1 (P1-18).

## Phase 2 task log

Phase 2 scope decisions by the orchestrator:
- Settings whose feature does not exist yet are not shown on the settings pages and keep their stored values: aspect (Phase 3), double-tap skip length and battery (Phase 3), background audio and Picture-in-Picture (Phase 4). The theme row of the settings home arrives in Phase 3.
- Contract change after the freeze (docs/CONTRACTS.md): `IPlayerEngine` gained `TracksChanged`, `CurrentAudioTrackId`, `CurrentSubtitleTrackId`, `AudioDelayMs`, `SubtitleDelayMs`, `SubtitleStyle`.
- `SubtitleService` applies its policy by itself when a file opens (it follows the engine); nobody calls it. External subtitle files are copied as UTF-8 into app data, and the per-file state stores the `file://` URI of the copy.

| Task | Agent | Model | Attempts | Result |
|---|---|---|---|---|
| P2-01 | playback-state-store | Sonnet 5.5 | 3 rounds | merged. `PlaybackStateRecorder`. Reviewer: approve; all traced interleavings correct. Fixed on the way: a failed load can no longer overwrite saved overrides, one pending retry per file, no redundant queued saves. |
| P2-02 | playback-state-store | Sonnet 5.5 | 3 rounds | merged. `playback_state` table, repository, DI. Reviewer: approve. Enum overrides are stored as names and read tolerantly. |
| P2-03 | global-defaults-store | Haiku 5.5 | 3 rounds | merged. Store. Reviewer: changes-required (missing JSON fields lost their defaults, a fault of the brief), approve after the DTO fix. |
| P2-04 | ui-settings | Haiku 5.5 | 2 rounds | merged. Store. Reviewer: changes-required (same JSON finding), approve after the fix. |
| P2-06 | core-contracts, player-engine | Fable 5.2 (orchestrator) | 1 | merged. Contract additions above; `SetSubtitleStyleAsync` restarts the input at the current position; an external subtitle no longer overrides a track chosen after it. Not yet run on an emulator. Open: the bottom margin of the subtitle style is not applied, and the freetype options are unverified. |
| P2-07 | test-writer | Sonnet 5.5 | 1 | merged with P2-06. `FakePlayerEngine` implements the new members. |
| P2-08 | subtitles-audio | Sonnet 5.5 | 1 | merged. `LanguageCodes`, `SidecarMatcher`, `PreferredTrackSelector`. |
| P2-09 | subtitles-audio | Sonnet 5.5 | 1 | merged, not yet run. `SubtitleService`, `SubtitleFileStore`, `SubtitleTextEncoding` (Windows-1255 and other legacy code pages to UTF-8). Gate review and emulator check pending. |
| P2-18 | theming-engine | Sonnet 5.5 | 1 | merged. Styles for RadioButton, CheckBox, Picker, Editor and keyed styles for settings rows and HUD panels. |
| core fix | core-contracts | Fable 5.2 (orchestrator) | 1 | `MediaFileId.ToKey` is culture-invariant (reviewer of P2-02). |
| P2-T4 | test-writer | Sonnet 5.5 | 1 | merged. 94 tests for `UiSettingsStore`. No product bug. |
| P2-T5 | test-writer | Sonnet 5.5 | 1 | merged. 174 tests for the subtitle logic. No product bug. |
| P2-05 | app-skeleton | Haiku 5.5 | 1 | merged. Real stores and the recorder replace the Phase 1 placeholders. Reviewer: approve. Emulator: a file played to 6.7 s, force-stopped and reopened resumed near 6 s, with a progress bar in the list and no NEW tag. |
| P2-10 | app-skeleton | Haiku 5.5 | 1 | merged. Phase 2 services and pages registered (8 lines, checked by the orchestrator). |
| P2-11, P2-22 | global-defaults-store | Haiku 5.5 | 4 rounds, 1 | merged. Playback defaults page and default subtitle style page. Reviewer: changes-required twice (quick taps dropped by `[RelayCommand]` on async methods; a reset that lost its error), then approve. |
| P2-12, P2-23 | ui-settings | Haiku 5.5 | 1, 1 | merged. Interface settings page, settings home, review follow-ups. Reviewer: approve. |
| P2-13a, P2-13b | playback-settings-ui | Haiku 5.5 | 2, 1 | merged. Speed, repeat and A-B, audio sheets. Reviewer: approve. The modal sheet is see-through over the video (SPIKE finding 18). |
| P2-13c, P2-32 | playback-settings-ui | Haiku 5.5 | in review | Subtitle sheet and subtitle style sheet. Reviewer: changes-required (a style could be applied twice). Fix round running, with readable panels. |
| P2-16, P2-20 | player-hud-ui | Haiku 5.5 | 1, 1 | merged. HUD entry points (three icons, speed label), per-file decoder choice, the HUD hides under a sheet. Reviewer: approve. |
| P2-17 | explorer-ui | Haiku 5.5 | 2 rounds | merged. Settings entry, folder list follows the new-tag age. Reviewer: changes-required (a null value made every settings event reload), approve after the fix. |
| P2-19 | playback-state-store | Sonnet 5.5 | 1 | merged. A failed save is retried on a timer and by `FlushAsync` (found by the test writer). |
| P2-21 | theming-engine | Sonnet 5.5 | 1 | merged. Android light/dark mode follows the app theme: popups, dialogs and radio buttons are dark. Gate reviewer: approve. |
| P2-26 | core-contracts, subtitles-audio | Fable (orchestrator), Sonnet 5.5 | 1 | merged. Contract: `FindSidecarsAsync` returns `SubtitleFile` records with names. |
| P2-27 | player-engine | Fable 5.2 (orchestrator) | 2 builds | merged. Subtitle style as LibVLC instance options (SPIKE findings 14 to 16): before this no style was applied at all. Text direction automatic. Margin in video pixels. Emulator: all options visible, live style change restarts once and keeps the position. |
| P2-28 | subtitles-audio | Sonnet 5.5 | 1 | merged. Legacy code page from the file name and from the bytes (a Windows-1255 file showed Latin garbage on an English device). Emulator: Hebrew shown. |
| P2-30, P2-31 | core-contracts, player-engine, subtitles-audio | Fable (orchestrator), Sonnet 5.5 | in progress | Gate review of the engine: changes-required (an external subtitle chosen by its id was lost on a restart; a style change while paused left a black picture). Gate review of the subtitle service: changes-required (a slow load could reach the next file; a failed restore stopped the policy; replay added the file twice; one bad byte turned UTF-8 into garbage; I/O on the caller's thread). Contract: `AddSubtitleFileAsync` takes a display name, `ExternalSubtitlePath` added. |
| P2-T1, T2, T3, T6, T7 | test-writer | Sonnet 5.5 | 1 each | merged. Recorder 59 + 11 retry tests, state repository 42, defaults store 60, text encoding and theme brightness 94. No product bug. One finding: a failed save was not retried without a new event (fixed in P2-19). Total: 896 tests. |

Findings on the emulator in Phase 2 that changed the plan: SPIKE findings 14 to 18. Not possible on the platform: an `.ass` or `.ssa` file next to the video is not listed by MediaStore on API 34 for an app with only the media permissions, so it is opened with the file picker.

## Known gaps carried forward

- Stretch aspect mode: LibVLC letterboxes inside the surface, so the engine must force the display aspect. Phase 3, needs a contract decision.
- Immersive system bars on the player page, battery in the HUD: Phase 3 (platform code, owner to be decided).
- System bar colors follow a theme change at runtime: Phase 3, theming-engine. Phase 1 uses fixed colors in `colors.xml` that match Dark OLED.
- The Release APK is 105 MB because it carries three ABIs. A smaller per-ABI APK is possible once the owner's phone is known.
- The armeabi-v7a part of the APK has never run (no emulator image runs it).
- Subtitle style: applied only when playback starts; the freetype options are unverified. Phase 2.
- Pause when the app goes to the background: Phase 1 uses a placeholder in app-skeleton; media-service owns it in Phase 4.
- Contract doc change after the freeze (orchestrator): `IVideoOutputBinding` surface methods must be called on the UI thread; `PlayerSnapshot.Idle`; `IVideoSurface.WindowBrightness` is -1 for the system default.

## Agents

| Agent | Model | Phase | Current task | Attempts | Result | Open escalations |
|---|---|---|---|---|---|---|
| orchestrator | Fable 5.2 | 0 | Phase 0 spike, architecture, specs | 1 | GO for LibVLCSharp on emulator, physical device check open | none |
| core-contracts | Fable 5.2 | 1 | - | 0 | - | none |
| player-engine | Fable 5.2 | 1, 2, 4 | - | 0 | - | none |
| media-service | Fable 5.2 | 4 | - | 0 | - | none |
| video-surface | Fable 5.2 | 1, 3 | - | 0 | - | none |
| gestures | Sonnet 5.5 | 3 | - | 0 | - | none |
| subtitles-audio | Sonnet 5.5 | 2 | - | 0 | - | none |
| media-library | Sonnet 5.5 | 1 | - | 0 | - | none |
| thumbnail-cache | Sonnet 5.5 | 1 | - | 0 | - | none |
| file-ops | Sonnet 5.5 | 5 | - | 0 | - | none |
| playback-state-store | Sonnet 5.5 | 2 | - | 0 | - | none |
| playback-coordinator (added) | Sonnet 5.5 | 1, 2 | - | 0 | - | none |
| theming-engine | Sonnet 5.5 | 1, 3 | - | 0 | - | none |
| pip | Sonnet 5.5 | 4 | - | 0 | - | none |
| media-session-bt | Sonnet 5.5 | 4 | - | 0 | - | none |
| reviewer | Sonnet 5.5 | all | - | 0 | - | none |
| explorer-ui | Haiku 5.5 | 1, 5 | - | 0 | - | none |
| player-hud-ui | Haiku 5.5 | 1, 3 | - | 0 | - | none |
| playback-settings-ui | Haiku 5.5 | 2 | - | 0 | - | none |
| global-defaults-store | Haiku 5.5 | 2 | - | 0 | - | none |
| ui-settings | Haiku 5.5 | 2 | - | 0 | - | none |
| test-writer | Haiku 5.5 | all | - | 0 | - | none |
| device-qa | Haiku 5.5 | all | - | 0 | - | none |
| app-skeleton (added) | Haiku 5.5 | 1, then wiring | - | 0 | - | none |

## Open items for the owner

1. Run the device test in docs/SPIKE.md to close Phase 0 (Phase 1 was started in parallel at the orchestrator's discretion).
2. Install `artifacts/ArielPlayer-phase1-r2.apk`. If the phone refuses it, report the exact message, the phone model and the Android version (checklist item APP-3).
3. Run `docs/qa/phase-1-checklist.md` on the phone for the Phase 1 gate.

## Escalation log

| Date | Task | Agent | From | To | Reason |
|---|---|---|---|---|---|
| 2026-11-14 | P1-16a (surface geometry tests) | test-writer | Haiku 5.5 | Sonnet 5.5 | Three launches (P1-16 twice, then the smaller P1-16a) ended with the same model stream error (`EOF while parsing a value`) before any file was written. Other Haiku agents ran normally in the same period, and test-writer's first task (P1-15) succeeded. The cause is unknown and may not be capability. Escalated one level under MASTER.md section 5. Open: decide whether the remaining test tasks stay on Sonnet. |

## Owner decisions

| Date | Decision |
|---|---|
| 2026-11-14 | HARD GATE: no product code before agents are registered and the roster is approved. |
| 2026-11-14 | Orchestrator codes only for agents marked Fable; everything else is delegated or the owner switches agent manually. |
| 2026-11-14 | Roster (23 agents), ARCHITECTURE.md section 6 refinements and CONTRACTS.md approved. "Do everything as you see fit." |
