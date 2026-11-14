# PROGRESS

Tracking file required by docs/MASTER.md section 8. Updated by the orchestrator only.

## Current phase

Phase 0 (spike). HARD GATE active: no product code until every roster agent is registered in
.github/agents/<id>.agent.md with its model set and the owner approved the roster.

## Gates

| Phase | Build | Tests | Reviewer | Owner device test | Status |
|---|---|---|---|---|---|
| 0 Spike | green (spike) | n/a | n/a | pending | go on emulator, waiting for owner device test (docs/SPIKE.md) |
| 1 | green | 91 pass (Core) | partial | pending | in progress: runs end to end on the emulator |
| 2 | - | - | - | - | not started |
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
| P1-16 | test-writer | Haiku 5.5 | task/p1-16-tests-core-b | 1 (first launch died on a model stream error before writing anything; relaunched, not counted as a failed attempt) | running |
| P1-17 | app-skeleton | Haiku 5.5 | task/p1-17-wiring | 1 | merged. Reviewer: approve. Release manifest has no network permission. |
| P1-18 | app-skeleton | Haiku 5.5 | task/p1-18-shell-route | 1 | merged. One-line fix of an orchestrator design error (root route `explorer` shadowed `explorer/items`). Diff read by the orchestrator, reviewer pass at the gate. |
| P1-19 | player-engine | Fable 5.2 (orchestrator) | task/p1-19-engine-fixes | 1 | merged. Two defects found on the first emulator run (SPIKE.md findings 11 and 12). Not reviewed yet (gate). |
| P1-20 | explorer-ui | Haiku 5.5 | task/p1-20-explorer-polish | 1 | running |
| P1-21 | theming-engine | Sonnet 5.5 | task/p1-21-shell-style | 1 | running |
| P1-22 | device-qa | Haiku 5.5 | task/p1-22-device-qa | 1 | running |

Parallel tasks run in git worktrees under `D:\Projects\Ariel Video Player.wt\<task>` so that each keeps its own branch.

## First run of the product (emulator, Pixel 5 AVD, API 34, 2026-11-14)

Verified by the orchestrator, evidence in `docs/evidence/phase1/`: start, permission dialogs, folder list, item list
with lazy thumbnails and natural sort, open a video, HUD show/hide/auto-hide, decoder HW -> SW -> HW with the position
kept, pause/play, slider seek, end of file -> next file, end of the last file, 6 background/foreground cycles with the
picture restored and hardware decoding kept, vertical video, audio file, 8 rapid open/close cycles, Back.

Defects found and fixed: shell route (P1-18), native library load thread and late picture after backgrounding (P1-19).
Not verified: anything on a physical device, HEVC, 4K, 500+ files, API levels other than 34, a Release build on a device.

Phase 1 tasks not finished yet: P1-16, P1-20, P1-21, P1-22, tests for the coordinator and the data layer,
gate review of the merges that were not reviewed (P1-02, 04, 05, 06, 07, 09, 18, 19), owner device test.

## Known gaps carried forward

- Stretch aspect mode: LibVLC letterboxes inside the surface, so the engine must force the display aspect. Phase 3, needs a contract decision.
- Immersive system bars on the player page, battery in the HUD: Phase 3 (platform code, owner to be decided).
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

## Escalation log

None.

## Owner decisions

| Date | Decision |
|---|---|
| 2026-11-14 | HARD GATE: no product code before agents are registered and the roster is approved. |
| 2026-11-14 | Orchestrator codes only for agents marked Fable; everything else is delegated or the owner switches agent manually. |
| 2026-11-14 | Roster (23 agents), ARCHITECTURE.md section 6 refinements and CONTRACTS.md approved. "Do everything as you see fit." |
