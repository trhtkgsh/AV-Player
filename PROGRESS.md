# PROGRESS

Tracking file required by docs/MASTER.md section 8. Updated by the orchestrator only.

## Current phase

Phase 3, revision 2: built on 2026-11-15, waits for the owner's device test. Phase 4 does not start before it.
The owner tested `ArielPlayer-phase3.apk` on the phone and wrote "so far I have found no faults", with eleven
new requests and the instruction to sort them by the order of the phases. He did not say that the whole
checklist was run, so the Phase 3 gate is recorded as "no faults reported", not as a full checklist pass. The
requests are sorted in `docs/specs/11-owner-requests-r3.md` (Phase 1 scope: explorer; Phase 2 scope: stores and
settings; Phase 3 scope: player and gestures; Phase 5: file options; none for Phase 4) and were built in that
order. The table is in "Owner feedback on 2026-11-15 (Phase 3 APK)" below.

Revision APK: `artifacts/ArielPlayer-phase3-r2.apk` (Release, built from main at 253c918 on 2026-11-15,
108,258,080 bytes, arm64-v8a, armeabi-v7a and x86_64,
SHA-256 86574F14AC9E629558D9DC3CCE88CA9FBCA35D42BCBE5AA5C1FDF433F41707E7, same signing certificate and the same
permissions as the Phase 3 APK, no network permission; installs over the Phase 3 app and keeps its data).
Checklist: `docs/qa/phase-3-r2-checklist.md` (43 items for what changed; the regression blocks of
`docs/qa/phase-3-checklist.md` still apply). What was run and what was not is in "Phase 3 revision verification"
below.

Phase 3 APK (the build the owner tested): `artifacts/ArielPlayer-phase3.apk` (Release, built from main at 78f6efb on 2026-11-15, 107.6 MB,
arm64-v8a, armeabi-v7a and x86_64, SHA-256 E105DF2C7F03F6A219B47186FE55CE543BB4221FA215264A523558E3F2A5D028,
same signing certificate as the earlier APKs; installs over the Phase 2 app and keeps its data). Permissions:
the media read permissions and, new, `BATTERY_STATS` (declared because MAUI's battery API requires the
declaration; Android does not grant it to an ordinary app and shows no dialog for it). No network permission.
Checklist: `docs/qa/phase-3-checklist.md`. What was run and what was not is in "Phase 3 verification" below.

The Phase 2 gate is passed: on 2026-11-15 the owner ran
`docs/qa/phase-2-checklist.md` with `ArielPlayer-phase2.apk` on a Samsung Galaxy A55 (Android version not given):
0 failed items, nothing skipped, ARM-1 and ARM-2 without any problem. So the hang of the arm64 build on the
emulator's ARM translation (SPIKE finding 20) does not occur on that phone. The owner sent three items with the
report; they are in "Owner feedback on 2026-11-15 (Phase 2 APK)" below and are handled before the Phase 3 work.
The Phase 1 gate is passed: on 2026-11-15 the owner reported that
`ArielPlayer-phase1-r2.apk` installs and works on the phone and that every item of `docs/qa/phase-1-checklist.md`
was run and passed. The owner did not name the phone model or the Android version.
The roster was approved on 2026-11-14 and the HARD GATE was passed then.

Phase 2 APK: `artifacts/ArielPlayer-phase2.apk` (Release, built from main at aaa5f15 on 2026-11-15, 107 MB,
arm64-v8a, armeabi-v7a and x86_64, SHA-256 28ABA7B8FBAD1EDCA1E333586E791E5FDC5C9AE8FF153DC772E74F2D84B0B1F3,
same signing certificate as the Phase 1 APK, no network permission). Checklist: `docs/qa/phase-2-checklist.md`.
What was run and what was not is in "Phase 2 verification" below. One open risk: the arm64 half stops
responding on the emulator's ARM translation when the video is paused; the cause points to the translation
layer, and only a real phone can settle it (checklist items ARM-1 and ARM-2).

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

## Owner feedback on 2026-11-15 (Phase 2 APK, Samsung Galaxy A55)

| # | Feedback | What was found | What was done | State |
|---|---|---|---|---|
| 5 | Add a Hebrew interface as an option (an addition, not a replacement). | Every text of the app was an English literal in XAML or in a ViewModel. Spec 06 listed language selection as out of scope: the owner's request changes that. | Spec 10, the localization module and the L10N tasks: every text in English and Hebrew, a "Language" choice on the Interface page (System default, English, עברית), right-to-left layout in Hebrew, change at once. Orchestrator's decision: the default is "System default", so on the owner's Hebrew phone the app opens in Hebrew; English is one tap away and stays complete. | Done, checked on API 34 and API 36 in the Release build. Waiting for the owner (checklist LANG-1 to LANG-8). |
| 6 | In "fit to screen" the video shows at about 99% of the screen and becomes 100% after pause or play. On leaving the player the picture is stretched over the whole screen for about a second. | The engine read the size of the video track only on track events and on pause or play, and LibVLC raises no event when the decoder corrects the size. On leaving, the engine's "no format" report made the surface fill its container while the last picture was still on it. | P3-01, P3-05: the format is read again while the first pictures are shown; the surface keeps its shape when the engine stops. | Fixed in code. The 99% picture could not be reproduced on the emulators, so only the owner's phone can confirm it (checklist FIX-2, FIX-3). |
| 7 | The play, next and previous buttons are reversed: the play triangle points left and next/previous swapped places. | The phone's language is Hebrew, so Android lays the app out right to left and MAUI mirrors the transport row and its vector symbols. Media transport controls must not be mirrored. | P3-02: the bottom bar of the player is left to right in every language; symbols are not mirrored, except the back arrow. | Done, checked on the emulator with a Hebrew app locale. Waiting for the owner (checklist FIX-1). |

## Owner feedback on 2026-11-15 (Phase 3 APK): eleven requests, sorted by phase

The owner's order is kept in the first column. "Phase" is the phase whose module the request changes (spec 11).

| # | Request | Phase | Spec 11 | What was done | State |
|---|---|---|---|---|---|
| 7 | A list or grid (icons) view in the explorer. | 1 | R1-2 | "Grid" or "List" in the title bar of the folder list and the file list. Tiles with a large thumbnail (files) or a folder symbol (folders); two columns in portrait, up to four in landscape. | Done, emulator API 34 and 36. |
| 11 | Explorer modes: all media folders, file browser, favorites; a setting to include audio-only folders; favorites are added from folder options, like the file options. | 1 | R1-1, R1-3, R1-4 | "View" opens Media folders, File browser, Favorites, All files. The file browser shows the media folders as a directory tree, one level at a time; Back goes up. Folder options open from a three-dot button on every folder: add to or remove from favorites, default speed. The switch "Show folders with audio only" is on the Interface page. | Done, emulator. File options (rename, delete) stay in Phase 5 and will use the same button. |
| 8 | A default speed per folder. | 2 | R2-1 | New store (`IFolderSettingsStore`, agent `folder-settings-store`). Order: the file's own speed, the folder's, the general default. | Done, emulator (1.5x folder, other folder 1.0x). |
| 9 | Font size for folder and file names. | 2 | R2-2 | "Size of names" on the Interface page, 80% to 160%. | Done, emulator. |
| 10 (setting) | A setting for the long-press speed. | 2 | R2-3 | "Speed while holding the picture" on the Interface page, 0.25x to 4.0x, default 2.0x. | Done, emulator. |
| 1 | The status bar and the navigation bar appear and disappear with the player controls. | 3 | R3-1 | `IVideoSurface.SetSystemBarsVisible`, the HUD keeps clear of the bars with `SystemBarInsets`; the picture does not move; the bars are see-through with white symbols. | Done, measured on API 34 and 36 in landscape with a 4:3 video. |
| 2 | The video name on its own line. | 3 | R3-2 | First line of the top bar. | Done. |
| 3 | Speed and decoder move to the top. | 3 | R3-3, R3-5 | The speed chip is in the top bar; the decoder is an item of the top-bar menu (orchestrator's reading of this request together with request 5). | Done. To confirm with the owner. |
| 4 | No exit button: the system has one. | 3 | R3-4 | The Back button of the player is removed. | Done. |
| 5 | Subtitles, audio track, decoder and repeat in a menu in the top bar, next to Lock. | 3 | R3-5 | A three-dot menu: Subtitles, Audio track, Repeat, "Decoder: HW" or "SW". | Done, emulator, English and Hebrew. |
| 6 | The seek gesture shows a still picture of the target in real time, without playing. | 3 | R3-7 | The drag pauses, seeks while the finger moves (only the newest target is sent), and continues on release. | Done, emulator. Speed on long files with far-apart key frames is not measured: only the phone can tell. |
| 10 (gesture) | Holding a finger on the video plays faster; the earlier speed returns when the finger lifts. | 3 | R3-8 | `GestureKind.LongPress` after half a second; the speed is not saved for the file. | Done, emulator. |

## Gates

| Phase | Build | Tests | Reviewer | Owner device test | Status |
|---|---|---|---|---|---|
| 0 Spike | green (spike) | n/a | n/a | pending | go on emulator, waiting for owner device test (docs/SPIKE.md) |
| 1 | green (Debug and Release) | 349 pass (307 Core, 42 Data) | every merged task approved, except P1-27 (tests only, Sonnet, checked by the orchestrator) | passed on 2026-11-15 (owner report: all checklist items pass) | PASSED |
| 2 | green (Debug and Release) | 900 pass (662 Core, 238 Data) | every merged task approved; engine and subtitle service had a gate review and a second round | passed on 2026-11-15 (owner report: Samsung Galaxy A55, 0 failed items, ARM-1 and ARM-2 pass) | PASSED |
| 3 | green (Debug and Release) | 1340 pass (989 Core, 351 Data) | every merged task approved; engine, surface, gestures, theming, localization and coordinator had a gate review | 2026-11-15: "no faults found so far" (full checklist run not stated); eleven requests, see spec 11 | superseded by revision 2 |
| 3, revision 2 | green (Debug and Release) | 1634 pass (1104 Core, 530 Data) | every merged product task approved; R3-T2 (tests only, Sonnet) checked by the orchestrator | pending (`docs/qa/phase-3-r2-checklist.md`) | built, waiting for the owner |
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
| P2-13c, P2-32, P2-35 | playback-settings-ui | Haiku 5.5 | 3 rounds | merged. Subtitle sheet and subtitle style sheet. Reviewer: changes-required (a style could be applied twice), approve after P2-32 (one apply under a gate, a single navigation to the style sheet, A-B draft fixes, a backdrop that makes all five sheets readable over the picture), approve of P2-35 ("Save as default" waits for a running apply). Open, not blocking: after a failed apply "Save as default" refuses until the next successful apply. |
| P2-16, P2-20 | player-hud-ui | Haiku 5.5 | 1, 1 | merged. HUD entry points (three icons, speed label), per-file decoder choice, the HUD hides under a sheet. Reviewer: approve. |
| P2-17 | explorer-ui | Haiku 5.5 | 2 rounds | merged. Settings entry, folder list follows the new-tag age. Reviewer: changes-required (a null value made every settings event reload), approve after the fix. |
| P2-19 | playback-state-store | Sonnet 5.5 | 1 | merged. A failed save is retried on a timer and by `FlushAsync` (found by the test writer). |
| P2-21 | theming-engine | Sonnet 5.5 | 1 | merged. Android light/dark mode follows the app theme: popups, dialogs and radio buttons are dark. Gate reviewer: approve. |
| P2-26 | core-contracts, subtitles-audio | Fable (orchestrator), Sonnet 5.5 | 1 | merged. Contract: `FindSidecarsAsync` returns `SubtitleFile` records with names. |
| P2-27 | player-engine | Fable 5.2 (orchestrator) | 2 builds | merged. Subtitle style as LibVLC instance options (SPIKE findings 14 to 16): before this no style was applied at all. Text direction automatic. Margin in video pixels. Emulator: all options visible, live style change restarts once and keeps the position. |
| P2-28 | subtitles-audio | Sonnet 5.5 | 1 | merged. Legacy code page from the file name and from the bytes (a Windows-1255 file showed Latin garbage on an English device). Emulator: Hebrew shown. |
| P2-30, P2-31 | core-contracts, player-engine, subtitles-audio | Fable (orchestrator), Sonnet 5.5 | 1, 1 | merged. Gate review of the engine: changes-required (an external subtitle chosen by its id was lost on a restart; a style change while paused left a black picture). Gate review of the subtitle service: changes-required (a slow load could reach the next file; a failed restore stopped the policy; replay added the file twice; one bad byte turned UTF-8 into garbage; I/O on the caller's thread). All fixed. Contract: `AddSubtitleFileAsync` takes a display name, `ExternalSubtitlePath` added. |
| P2-33 | player-engine | Fable 5.2 (orchestrator) | 1 | merged. Found in the integration run: a restart of a paused file lost its position (0 after the second restart) and kept the old picture (SPIKE finding 19). The reveal added in P2-30 never ran, because LibVLC reports such an input as Playing and then Paused. Fixed with a clock flag; verified with engine traces. |
| P2-36 | player-engine | Fable 5.2 (orchestrator) | 1 | merged. Second review round of the branch: approve, no blocking finding. Four of its notes fixed (a stale event after a restart, a file added twice while opening, a missing copy still reported, Stop). Open, not blocking: a subtitle load that crosses an open of the next file can land on the new file (needs the engine to know the item: contract change); the name match of the external track assumes the store's 16-hex names. |
| P2-34, P2-37 | device-qa | Haiku 5.5 | 2 rounds, 1 | merged. `docs/qa/phase-2-checklist.md`: 53 Phase 2 items and the 67 Phase 1 items as a regression block. The agent stopped once, correctly, on two spec conflicts (skip length hidden in Phase 2; who writes global defaults); specs 03 and 06 were amended. |
| P2-T1, T2, T3, T6, T7, T8 | test-writer | Sonnet 5.5 | 1 each | merged. Recorder 59 + 11 retry tests, state repository 42, defaults store 60, text encoding and theme brightness 94, fake engine contract and tolerant UTF-8 17. No product bug. One finding: a failed save was not retried without a new event (fixed in P2-19). Total on main: 900 tests (662 Core, 238 Data). |

Findings on the emulator in Phase 2 that changed the plan: SPIKE findings 14 to 20. Not possible on the platform: an `.ass` or `.ssa` file next to the video is not listed by MediaStore on API 34 for an app with only the media permissions, so it is opened with the file picker.

## Phase 2 verification (2026-11-15)

Run by the orchestrator, on emulators only. No physical device was used.

| Target | Build | Result |
|---|---|---|
| Pixel 5 AVD, API 34, x86_64 | Debug (integration of all Phase 2 branches) | Pass: sidecar `.srt` loads by itself; Hebrew right to left with the punctuation on the left; Windows-1255 decoded; external track shown by its file name; style change while playing and while paused (position kept, new picture drawn); margin from 0% to 30%; decoder switch while paused keeps the external track; repeat "This file" keeps one subtitle track; `.ass` through the system file picker; per-file speed and decoder; resume after force-stop; a watched file starts at 0. |
| Pixel 5 AVD, API 34, x86_64 | Release APK, installed over the Phase 1 r2 APK | Pass: the update installs and keeps the app data; all five sheets; "Save as default"; settings home, playback defaults, interface settings; the clock switch reaches the HUD; default decoder Software applies to a new file; resume after force-stop. |
| Pixel 7 AVD, API 36, x86_64 | Release APK | Pass, two runs: pause, subtitle sheet, style sheet, style change, two decoder switches, back. |
| Pixel 7 AVD, API 36, arm64 through the image's ARM translation | Release APK | Playback, subtitles and three paused style changes worked once. Then it failed in two ways, see below. |
| armeabi-v7a | - | Never run (no image runs it). |

The arm64 failures on the ARM translation (SPIKE finding 20):
- The app stops responding when sheets are opened while the video is paused: 8 of 10 paused runs, also with the Speed sheet alone (4th opening), so it does not depend on the subtitle code. The ANR dump shows the .NET threads stopped for a garbage collection and never resumed.
- Two crashes (SIGSEGV) inside the translator's JNI trampoline (`berberis::ToHostJNIEnv`), on a LibVLC audio thread.
- The same APK's x86_64 half on the same emulator passes the same steps. The Phase 1 APK's arm64 half, paused and sent four rotation requests, did not fail, but it has much less UI to open while paused, so it is a weak control.
- Assessment: the translator does not run a signal handler of a thread that is blocked in a system call, and the .NET runtime stops its threads with signals, so a paused player (LibVLC threads blocked for good) cannot finish a garbage collection there. A real ARM kernel delivers the signal. This is a reasoned explanation, not a proof: it has not been run on a real arm64 device. The checklist starts with ARM-1 and ARM-2 for this.

Not verified at all in Phase 2: pitch at 0.25x and 4.0x and the audio delay by ear (the emulator runs without audio), a file with several audio tracks or embedded subtitle tracks (no such test file), the preferred language defaults on a real file, A-B repeat on the emulator (unit tests only), repeat "Folder" wrap-around on the emulator (unit tests only).

## Phase 3 task log (started 2026-11-15)

Scope decisions by the orchestrator:
- Owner request: a Hebrew interface as an option. Spec 10 (`docs/specs/10-localization.md`), a new agent `localization` (Sonnet 5.5; spec and `.github/agents/localization.agent.md` written; the agent type is not available to the `task` tool until the session restarts, so it runs through `general-purpose` with the roster model set). Texts live with their module: one `IStringSource` class per module, keys `<Module>.<Name>`. `UiSettings.Language`: System (default, follows the device), English, Hebrew. The language changes at once; the direction is set on the window.
- The transport bar of the player is never mirrored (left to right in every language); symbols are not mirrored except the back arrow.
- The window of the player (immersive bars, brightness) belongs to video-surface: `IVideoSurface.SetImmersive`.
- LibVLC is told to fill the surface exactly (aspect ratio = surface size), so the surface view alone decides the shape of the picture. This makes Stretch work without a contract change to the engine.
- The coordinator applies the resolved aspect mode when it opens an item (`IVideoSurface.SetAspect`).
- Top bar of the player: Back, title with a status line (clock and battery), Subtitles, Audio, Repeat, Aspect, Lock.
- The battery level uses MAUI's `Battery` API; the HUD module declares the permission it requires (`BatteryStats`) with an assembly attribute in its own path (ARCHITECTURE section 2).
- The first page is created only after the saved theme and language are applied (no flash of the wrong theme or language).

| Task | Agent | Model | Attempts | Result |
|---|---|---|---|---|
| P3-01, P3-05 | player-engine, video-surface | Fable 5.2 (orchestrator) | 1, 1 | merged. Owner feedback 6. The video format is read again while the first pictures are shown (LibVLC raises no event when the decoder corrects the track size; before, the next read came only with a pause or a play). The surface keeps its shape when the engine stops, so the last picture is not stretched while the page closes. `SetImmersive`. LibVLC fills the surface. Gate review: approve; its notes fixed in P3-05 (no forced aspect while the format of a new file is unknown, the aspect is sent only when it changes, audio files skip the format checks). The 99% picture could not be reproduced on the emulators (1080p file, portrait and landscape, HW and SW): the fix follows from the code path, not from a reproduction. |
| P3-02 | player-hud-ui | Haiku 5.5 | 1 | merged. Owner feedback 7. Bottom bar left to right, `HudIconButton.MirrorInRightToLeft`. Reviewer: approve. Emulator with a Hebrew app locale: transport row in place, back arrow mirrored. |
| P3-03 | core-contracts | Fable 5.2 (orchestrator) | 1 | merged. Contracts: `SetImmersive`, `AppLanguage`, `UiSettings.Language`, `LocalizedText`, `ILocalizer`, `IStringSource`. Spec 10, agent spec. |
| P3-10 | gestures | Sonnet 5.5 | 2 rounds | merged. `GestureClassifier` (pure), overlay view, `GestureSource`. Gate review: approve; follow-ups done (reset on detach, off-axis delta zero, subscriber exceptions caught). |
| P3-T1 | test-writer | Sonnet 5.5 | 2 rounds | merged. 125 classifier tests. No product bug. |
| P3-20a, b, c, P3-21 | theming-engine | Sonnet 5.5 | 2 launches (the first died on a stream error with nothing written), then 4 steps | merged. Accent palette, token resolver, `ThemeSettingsNormalizer`, `ThemeSettingsRepository`, runtime swap, system bars, shared styles, theme page (route `settings-theme`). Gate review: changes-required (accent-colored text on the background fails the contrast rule in Light), fixed in P3-20c, second round: approve. P3-21: on API 36 the strip behind the status bar stayed black in Light (the `AppBarLayout` background is a static theme color); every app bar is now colored with the theme, also for pushed pages. Verified on the API 36 emulator. |
| P3-30, P3-31, P3-32 | player-hud-ui | Haiku 5.5 | 3 rounds | merged. Gesture handling with OSD (tap, double tap, volume, brightness, seek, zoom, pan), immersive window, new top bar, aspect button, screen lock, battery, texts. Reviewer: changes-required (the lock was released when the app went to the background), approve after P3-32. |
| P3-40, P3-41 | playback-coordinator | Sonnet 5.5 | 1, 1 | merged. Aspect mode applied on open; not for a cancelled open. Gate review: approve. |
| P3-50, P3-51, P3-52 | app-skeleton | Haiku 5.5 | 2 rounds, 1, 1 | merged. Gestures and localization wired; the first page is created after the saved theme and language are applied (at most 5 s); the activity applies the system bar colors. Reviewer: approve for each. |
| L10N-01, L10N-02, L10N-03 | localization | Sonnet 5.5 | 3 rounds | merged. `StringCatalog`, `Localizer`, `{loc:T}`, common texts. Gate review: changes-required (a quick sequence of language changes could end on the wrong one), fixed in L10N-02, approve. L10N-03: language names keep their side in either direction (direction marks), subscribers isolated, a missing Hebrew text falls back to English. Approve. |
| L10N-T1, L10N-T2 | test-writer | Sonnet 5.5 | 1, 2 rounds | merged. 69 catalog tests; 22 tests that read the string source files and the XAML and C# of the app (every key used exists, no duplicates, sorted, both languages present, same placeholders, counted texts in pairs). |
| L10N-11 | explorer-ui | Haiku 5.5 | 2 rounds | merged. Explorer texts. Reviewer: approve. |
| L10N-13, L10N-16 | playback-settings-ui | Haiku 5.5 | 3 rounds | merged. Sheet texts. Reviewer: approve; Hebrew corrections applied. |
| L10N-14 | global-defaults-store | Haiku 5.5 | 3 rounds | merged. Defaults pages texts, default aspect row shown. Reviewer: approve; Hebrew corrections applied. |
| L10N-15, L10N-17 | ui-settings | Haiku 5.5 | 2 launches (the first died on a stream error after its first commit), 4 steps, 1 | merged. Language stored and chosen on the Interface page, skip length, battery switch, Theme row, texts. Reviewer: approve. L10N-17 (found by the test writer): a stored field of the wrong JSON kind lost the whole row; each field now falls back alone. Reviewer: approve. |
| P3-T2, T3, T4, T5 | test-writer | Sonnet 5.5 | 1 each | merged. Theming (resolver rules, accent palette, normalizer, repository: 187), coordinator aspect and catalog null texts (36), UI settings language (41, one product bug found: L10N-17), catalog English fallback. Total on main: 1340 tests (989 Core, 351 Data). |
| P3-60 | device-qa | Haiku 5.5 | 2 rounds | merged. `docs/qa/phase-3-checklist.md`: 53 Phase 3 items, 53 Phase 2 and 67 Phase 1 items as regression blocks. |

Emulator findings so far (API 34, x86_64): the Hebrew layout works (right to left, texts, counted texts); immersive player; volume, brightness and seek gestures with their OSD; Stretch, Crop and 100% while playing and while paused, with the hardware and the software decoder; pinch zoom works. Known limit: an aspect change after the file has ended shows a wrong picture until playback starts again (nothing renders a new frame).
Android shows its own "Viewing full screen" hint the first time the player hides the system bars.

## Phase 3 verification (2026-11-15)

Run by the orchestrator, on emulators only (x86_64). No physical device was used.

| Target | Build | Result |
|---|---|---|
| Pixel 5 AVD, API 34 | Debug (integration of the Phase 3 branches) | Pass: Hebrew layout and texts with the device seen as Hebrew; live language change on the Interface page (the page, the settings home and the explorer follow); player top bar mirrored and bottom bar unchanged in Hebrew; immersive player; tap, double tap (10 s forward and back), volume, brightness and seek gestures with their indicators; pinch zoom and pan (the picture stops at its edge); Stretch, Crop and 100% while playing and while paused with the hardware and the software decoder; lock (taps, drags and Back blocked, unlock button, still locked after HOME and return); sheets in Hebrew; theme page and the Light theme on every page. |
| Pixel 5 AVD, API 34 | Release APK, installed over the Phase 2 APK | Pass: the update keeps the data; language switch at once (the `{loc:T}` bindings work in the trimmed build); theme page in Hebrew, Dark Slate applied; player with the volume indicator, Stretch while paused. |
| Pixel 7 AVD, API 36 | Release APK | Pass: Light theme, explorer, immersive player, aspect, a file with two audio tracks and two embedded subtitle tracks (both listed and selected in the sheets: first run with such a file), brightness indicator, double tap. Found: the strip behind the status bar stayed black in Light (fixed in P3-21, verified with a Debug build on the same emulator, then published again). |
| arm64-v8a, armeabi-v7a | - | Not run. The emulator's ARM translation cannot run the paused player (SPIKE finding 20); the Phase 2 build ran on the owner's arm64 phone. |

Not verified at all in Phase 3: the owner's 99% picture on real hardware (not reproducible on the emulators: the fix follows from the code path); the stretch on exit as the owner saw it (the emulator showed a shifted, not a stretched, last picture before the fix and an unchanged one after it); sound above 100% and real brightness (the emulator has no audio and no backlight); sharpness at 400%; gesture recognition by a hand (10 of 10); a real Hebrew phone; landscape with a camera hole; the lock across a real screen-off.

## Phase 3 revision task log (2026-11-15, owner requests, spec 11)

Scope decisions by the orchestrator:
- The requests are sorted by the phase whose module they change and were built in that order: contracts first, then stores and library, then the player, then the explorer.
- "Move the speed and the decoder up" together with "decoder in the menu": the speed is a chip in the top bar, the decoder is an item of the top-bar menu.
- Folder options open from a three-dot button on the folder's row or tile, not from a long press (MAUI has no long-press recognizer for list rows without platform code). File options join the same pattern in Phase 5.
- "File browser" is the tree of the media folders by their path, one level at a time. It is not a browser of every directory of the storage: the app reads MediaStore only.
- A new agent, `folder-settings-store` (Haiku 5.5), owns the per-folder store. Like `localization`, it is registered in `.github/agents` but not available to the `task` tool until the session restarts, so it ran through `general-purpose` with the roster model.
- The device checklist of this round is its own file (`phase-3-r2-checklist.md`), so the owner runs only what changed.

| Task | Agent | Model | Result |
|---|---|---|---|
| R3-01, R3-02 | core-contracts, video-surface | Fable 5.2 (orchestrator) | merged. Contracts (see `docs/CONTRACTS.md` change log): `ExplorerViewMode` Tree and Favorites, `ExplorerLayout`, `GestureKind.LongPress`, `MediaFolder.AudioOnly`, `FolderSettings`, `IFolderSettingsStore`, `EdgeInsets`, four `UiSettings` fields, the resolver's folder argument, `IVideoSurface.SetSystemBarsVisible` and `SystemBarInsets`. Player mode: the window is edge to edge, the bars are shown and hidden inside it. Reviewer: approve; follow-ups in R3-02. |
| R3-10 | gestures | Sonnet 5.5 | merged. Long press after 500 ms without movement. Reviewer: approve. |
| R3-11, R3-15 | ui-settings | Haiku 5.5 | merged. Four stored fields, three rows on the Interface page; every stored field falls back alone; Hebrew texts. Reviewer: approve. |
| R3-12 | folder-settings-store | Haiku 5.5 (through `general-purpose`) | merged. `FolderSettingsStore` on the settings table, `AddFolderSettings()`; a follow-up keeps the loaded map. Reviewed and approved. |
| R3-13 | media-library | Sonnet 5.5 | merged. `MediaFolder.AudioOnly`. Reviewer: approve. |
| R3-14 | playback-coordinator | Sonnet 5.5 | merged. The folder's default speed is read on open. Reviewer: approve. |
| R3-50 | app-skeleton | Haiku 5.5 | merged. `AddFolderSettings()` in `MauiProgram.cs`. Reviewer: approve. |
| R3-20, R3-21 | player-hud-ui | Haiku 5.5 | merged. Title line, second line with status, speed chip, Aspect, menu, Lock; no Back button; bottom bar with seek row and transport; menu; bars follow the controls; seek preview; long-press speed. Reviewed and approved. |
| R3-22 | player-hud-ui | Haiku 5.5 | merged. The player page ignores the safe area (found by the orchestrator on API 36, see below). Reviewer: approve. |
| R3-30, R3-31, R3-32 | explorer-ui | Haiku 5.5 | merged. `FolderTree`, view modes, folder options, favorites, list and grid, name size, audio-only filter. Reviewer: changes-required (the file browser stayed inside a directory that had disappeared), approve after R3-31; R3-32 closes one more note (the audio-only filter uses the newest settings). |
| R3-03, R3-04 | video-surface | Fable 5.2 (orchestrator) | merged. Up to API 34: the bars are see-through with white symbols in player mode and the theme's look returns afterwards; the bar padding MAUI left on the pages under the player is cleared. Gate review: changes-required (the activity puts the theme look back on resume), approve after R3-04 (player mode is applied again when the window gets the focus). |
| R3-T0, R3-T1, R3-T2 | test-writer | Sonnet 5.5 | merged. Resolver with folder settings and fakes; long press, `AudioOnly`, coordinator folder speed, folder store, UI settings fields; `FolderTree` (45 cases). No product bug. Total on main: 1634 tests (1104 Core, 530 Data). |
| R3-60 | device-qa | Haiku 5.5 | merged. `docs/qa/phase-3-r2-checklist.md`: 15 explorer, 7 settings and 21 player items. |

Faults found by the orchestrator's own emulator runs in this round, all fixed before the APK:
- The picture shrank whenever the controls and the bars appeared (API 36, landscape, 4:3 video: 1080 px to 954 px). MAUI pads the children of the page's root layout by the safe area. The first check, in portrait with a 16:9 video, could not show it. Fixed in R3-22. SPIKE finding 27.
- Up to API 34 the bars were painted in the theme color over the top and bottom of the picture, and in the Light theme their symbols were dark on the black player. Fixed in R3-03.
- Up to API 34 the file list kept an empty band above the navigation bar after the player was closed (list height 1945 px before, 1879 px after). Introduced in this round by the edge-to-edge switch of R3-01; never in an APK the owner has. Fixed in R3-03. SPIKE finding 28.
- The folder symbol of the grid tile was not drawn and the options button had a border. Fixed in R3-31.

## Phase 3 revision verification (2026-11-15)

Run by the orchestrator, on emulators only (x86_64). No physical device was used.

| Target | Build | Result |
|---|---|---|
| Pixel 5 AVD, API 34 | Debug (integration builds) | Pass: view sheet with the four modes; file browser (levels, Back goes up, a directory that disappears sends the browser to its parent); folder options, favorite star, Favorites mode; folder speed; list and grid for folders and files, four columns in landscape; audio-only switch in every mode; Interface page rows; explorer and player in Hebrew (menu, folder options, top bar mirrored, bottom bar unchanged); the lock hides the controls and the bars. Light theme: white symbols in the player, also after Home and return; the light bars with dark symbols are back after Back. |
| Pixel 5 AVD, API 34 | Release APK, installed over the Phase 3 APK | Pass, on the final build: the update keeps the data (a played file keeps its state). Folder speed 1.5x on a new file of that folder; file browser; long press shows "Speed 2.0x"; the seek drag shows the target frame; menu with the four items. Landscape, 4:3 video: the picture is 1080 px high with the controls hidden, shown and hidden again; the bars lie over it, see-through. The file list has its full height after the player, in portrait and landscape, on every repeat. On the first Release build of this round: the "All files" mode of the Phase 3 app is kept, favorites, Favorites mode, decoder switch from the menu, playback continues after a seek drag. |
| Pixel 7 AVD, API 36 | Debug (integration builds) | Pass: the picture keeps its size after R3-22; Hebrew player in landscape; lock and unlock in landscape. |
| Pixel 7 AVD, API 36 | Release APK | Pass, on the final build: the picture keeps its size in landscape with the controls hidden, shown and hidden; each sheet opens from the menu (a file with two audio and two subtitle tracks); a seek drag while paused stays paused on the target; Light theme with white symbols in the player and the light bars back afterwards; file list height unchanged after the player; Interface page in Hebrew with the three new rows. On the build before the last surface fix: folder grid, file grid, player with controls in portrait and landscape, the Speed sheet over the unmoved picture. |
| arm64-v8a, armeabi-v7a | - | Not run (SPIKE finding 20; the Phase 2 and Phase 3 builds ran on the owner's arm64 phone). |

Not verified at all in this round: the seek preview on a long file with far-apart key frames (the test files are 12 to 20 seconds); the long press by a real finger (a still finger moves a little; the slop decides); a real camera hole in landscape (the emulators report no cutout); the bars on Samsung's One UI; grid thumbnails on a folder with hundreds of files; folder options and favorites with a real library; the Hebrew texts on a Hebrew phone; items EXP-13, EXP-15, EXP-18, EXP-21, EXP-23, SET-3 to SET-7, PLR-5, PLR-9, PLR-13, PLR-15 to PLR-18 of the checklist, which were not run as written.

## Known gaps carried forward

- Closed in Phase 3: Stretch (the engine fills the surface), immersive bars and battery (`IVideoSurface.SetImmersive`, MAUI `Battery`), system bar colors following the theme (also the status bar strip on API 35 and later).
- The Release APK is 107 MB because it carries three ABIs. The owner's phone (Samsung Galaxy A55) is arm64, so an arm64-only APK of about 45 MB is possible if the owner wants it.
- The armeabi-v7a part of the APK has never run (no emulator image runs it).
- A subtitle style change or a decoder switch restarts the input (LibVLC 3). While paused the position moves forward by 250 to 500 ms per restart.
- `.ass` and `.ssa` files next to the video are not found automatically (MediaStore hides them); the file picker loads them.
- Phase 2 leftovers, none blocking: the HUD stays hidden until a tap when `Shell.Current` is null as a sheet closes (P2-20); a local margin on the "File list" header (P2-23); A-B range edge cases in the coordinator; the Repeat button does not show the active mode.
- Phase 3 leftovers, none blocking:
  - An aspect change after a file has ended shows a wrong picture until playback starts again (nothing renders a new frame).
  - After a live language change the back arrow of the page that is open keeps its old direction until the next navigation.
  - While a settings sheet is open over the player the system bars are visible; they hide again when the sheet closes.
  - In landscape on a phone with a camera hole the top bar of the player is not kept clear of the hole (not checked on a device).
  - Error texts that come from an exception (`ex.Message`) in the sheets, the explorer and the defaults pages are still English.
  - The explorer saves the UI settings from a copy it read earlier; a save that lands between its read and its write could bring back an older language choice (narrow race, no atomic update in the store).
  - `AppBarTinter`: the once-per-activity mark is set before the registration succeeds; a failed registration is not retried (reviewer note).
  - The sliders and the check box keep the accent color, which is weak on the Light background for the yellow and lime accents (reviewer note).
  - In right-to-left layouts the rows of step buttons are mirrored (`Reset`, `+50 ms`, `-50 ms` from left to right), and sliders run right to left, as Android does by default.
- Phase 3 revision leftovers, none blocking:
  - A rotation after a file has ended can show a wrongly cut picture until playback starts again (the same cause as the aspect change after the end: nothing renders a new frame). Seen once on the API 36 emulator.
  - The unlock button and the HUD bars take their side paddings from the physical left and right insets; in a right-to-left layout on a phone with a camera hole at one side in landscape the unlock button may get the padding of the other side (reviewer note, no device to check).
  - Up to API 34 the HUD can be drawn for one frame without its bar paddings when the player opens (reviewer note).
  - With a transparent navigation bar Android may draw its own translucent strip behind the three-button navigation (not checked; the emulators use gesture navigation).
  - In split screen a resume without a focus change does not apply the player's bar look again (reviewer note, rare).
  - In the folder list (rows) the name of a favorite folder starts further in than the others, because the star stands before it.
  - Explorer: a layout change can still cause one extra in-place update of the rows; a change of the column count cancels thumbnail requests of tiles further down until the next scroll; a failed reload of the folder settings leaves the old stars until the next change (reviewer notes).
  - The long-press speed is a fixed value (default 2.0x), not "one step above the current speed": with a default speed of 2.0x the owner sets it to 3.0x on the Interface page, as in his example.
- The arm64 build cannot be tested on the emulators while paused (SPIKE finding 20). Emulator checks use x86_64.
- Pause when the app goes to the background: Phase 1 uses a placeholder in app-skeleton; media-service owns it in Phase 4.
- Contract doc change after the freeze (orchestrator): `IVideoOutputBinding` surface methods must be called on the UI thread; `PlayerSnapshot.Idle`; `IVideoSurface.WindowBrightness` is -1 for the system default.
- `docs/MASTER.md` has an uncommitted change in the main checkout that the orchestrator did not make (the Hebrew preface and the code fence around the plan were removed; the plan text is the same). It was first seen on 2026-11-15 during Phase 3. It is left as it is, not committed and not reverted: the file belongs to the owner.

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
| localization (added in Phase 3) | Sonnet 5.5 | 3 | - | 0 | - | none. Registered in `.github/agents`; run through `general-purpose` with the roster model until the session restarts. |
| folder-settings-store (added in the Phase 3 revision) | Haiku 5.5 | 2 (scope) | - | 0 | - | none. Registered in `.github/agents`; run through `general-purpose` with the roster model until the session restarts. |

## Open items for the owner

1. Run the device test in docs/SPIKE.md to close Phase 0 (Phase 1 was started in parallel at the orchestrator's discretion).
2. Done on 2026-11-15: `artifacts/ArielPlayer-phase1-r2.apk` installed and `docs/qa/phase-1-checklist.md` passed.
3. Done on 2026-11-15: `artifacts/ArielPlayer-phase2.apk` and `docs/qa/phase-2-checklist.md` passed on a Samsung Galaxy A55.
4. Install `artifacts/ArielPlayer-phase3-r2.apk` over the app on the phone and run `docs/qa/phase-3-r2-checklist.md` (43 items: what changed with the eleven requests). The regression blocks of `docs/qa/phase-3-checklist.md` still apply; say whether they were run. Report the failed item IDs and the Android version of the phone: up to Android 14 and from Android 15 the player window takes different code paths.
5. Decide: is "System default" the right default for the language (the app opens in Hebrew on a Hebrew phone), or should it open in English until Hebrew is chosen?
6. The agents `localization` and `folder-settings-store` were added to the roster without a separate approval (the earlier approval said to act by the orchestrator's judgement). Say so if it should be handled differently.
7. `docs/MASTER.md` has a local change that the orchestrator did not make (see "Known gaps"). Commit it or discard it as you see fit.
8. Confirm or correct three readings of the requests: the decoder is an item of the top-bar menu and only the speed is a chip in the top bar; folder options open from a three-dot button and not from a long press; "File browser" walks the folders that hold media, not every directory of the storage.

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
