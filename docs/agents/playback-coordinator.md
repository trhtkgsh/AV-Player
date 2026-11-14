# Agent: playback-coordinator

**Mission.** Run a queue of media items on top of the engine: open with resolved settings, next/previous, repeat modes and A-B repeat.

**Model and why.** Sonnet 5.5. State-machine logic with ordering and race conditions between engine events and user commands. Added to the MASTER roster because this logic had no owner.

**Phase.** 1: open, queue, next, previous, skip, close. 2: resolved settings on open, repeat modes, A-B repeat.

**Owned paths.** `src/Player.Core/Logic/Playback/**`.

**Read-only contracts.** Implements `IPlaybackCoordinator`. Uses `IPlayerEngine`, `IPlaybackStateStore`, `IPlaybackStateRecorder`, `IGlobalDefaultsStore`, `PlaybackSettingsResolver`, and the models.

**Inputs.** docs/specs/03 requirements 2 and 3, docs/specs/04 requirement 7, docs/specs/05 requirement 4, docs/CONTRACTS.md, docs/ARCHITECTURE.md section 3.

**Deliverables.** `PlaybackCoordinator` (pure, constructor-injected contracts and `TimeProvider`). Its DI registration line is given to app-skeleton in the report.

**Steps.**
1. All public methods run through one `SemaphoreSlim` so that commands never interleave. Engine events are handled through the same gate.
2. `OpenAsync`: flush the recorder, set `Current` and `Queue`, read the file's state and the global defaults, call `PlaybackSettingsResolver.Resolve`, then `IPlayerEngine.OpenAsync`. Phase 1 uses `new GlobalDefaults()` and a null state through the placeholder stores, so the code path is already final.
3. `NextAsync` / `PreviousAsync`: move within `Queue`. At the ends do nothing unless repeat is Folder, then wrap. `PreviousAsync` restarts the current file when its position is above 3 s.
4. `SkipAsync(deltaMs)`: seek to the clamped position.
5. On snapshot status `Ended`: File = seek to 0 and play. Folder = next with wrap. None = next if there is one, otherwise stay ended.
6. `SetRepeatAsync`: set the mode, save it to global defaults, raise `Changed`.
7. A-B: validate `EndMs - StartMs >= 1000` and both inside the duration, otherwise throw `ArgumentException`. While set, when a snapshot position reaches `EndMs` seek to `StartMs`. Clear A-B when the current item changes.
8. `CloseAsync`: flush the recorder, stop the engine, clear `Current`.
9. Raise `Changed` after every state change, outside the gate.
10. A newer `OpenAsync` cancels an older one that is still opening.

**Acceptance.**
- Player.Core builds. No reference outside Player.Core types and the BCL.
- Covered by test-writer with fakes: every step above, including two overlapping `OpenAsync` calls and an `Ended` event that arrives during `NextAsync`.
- Device: next/previous through a folder, each repeat mode at end of file, an A-B loop of 5 s runs 10 times without drifting.

**Out of scope.** Engine internals. Persisting per-file state (playback-state-store). UI. Subtitle and track policy (subtitles-audio). Media session and service.

**Escalation.** Stop and report when a contract is missing or wrong, a file outside the owned path is needed, an acceptance item cannot be met, or the same step failed twice. Never change a contract.

**Report format.** `STATUS: done | blocked`, `BRANCH`, `FILES`, `BUILD`, `TESTS`, `DEVICE CHECKLIST` (items for the owner), `DEVIATIONS`, `QUESTIONS`.
