# Agent: playback-state-store

**Mission.** Persist per-file playback state and save it on a debounce and on lifecycle events.

**Model and why.** Sonnet 5.5. Timing, lifecycle and process-death behavior need real reasoning, and mistakes lose the user's position.

**Phase.** 2.

**Owned paths.** `src/Player.Data/PlaybackState/**`, `src/Player.Core/Logic/PlaybackState/**`.

**Read-only contracts.** Implements `IPlaybackStateStore`, `IPlaybackStateRecorder`, one `IDbMigration`. Uses `IPlayerEngine`, `IAppLifecycle`, `IDbConnectionProvider`, `PlaybackState`, `MediaFileId`.

**Inputs.** docs/specs/04, docs/ARCHITECTURE.md sections 3 and 4 (table `playback_state`), docs/CONTRACTS.md.

**Deliverables.** `PlaybackStateRepository` and its migration (Player.Data), `PlaybackStateRecorder` (pure, in Core/Logic/PlaybackState), `AddPlaybackStateStore()` DI extension.

**Steps.**
1. Migration creates `playback_state` exactly as in ARCHITECTURE.md section 4.
2. Repository: get, get many (one query), upsert, delete. Position, duration, finished and last-played are columns. The nullable overrides are one JSON column. Null stays null.
3. Recorder takes `IPlayerEngine`, `IPlaybackStateStore`, `IAppLifecycle` and `TimeProvider` in its constructor. It keeps the current file's state in memory.
4. On each snapshot update position and duration in memory. Set `Finished` when position > 95% of duration. Mark dirty.
5. Debounce: when dirty, save once 5 s after the first unsaved change. Use `TimeProvider` timers only.
6. Save immediately on: status change to Paused, Ended or Idle, change of the current file, `Backgrounded`, `Stopping`, and every `Update` call.
7. When the current file changes, flush the old file's state before loading the new one.
8. Never save when nothing changed. Saves are serialized. A failed save is retried on the next trigger and never throws into the engine event.
9. `FlushAsync` saves now and completes when the write finished.

**Acceptance.**
- Build green. Recorder and repository covered by test-writer (fake `TimeProvider`, real SQLite file).
- Device: force-stop during playback, reopen: resumes within 5 s of the last position. Pause then swipe the app away: exact position kept.

**Out of scope.** Global defaults. The resolver (core-contracts). Applying state on open (playback-coordinator). UI.

**Escalation.** Stop and report when a contract is missing or wrong, a file outside the owned paths is needed, an acceptance item cannot be met, or the same step failed twice. Never change a contract.

**Report format.** `STATUS: done | blocked`, `BRANCH`, `FILES`, `BUILD`, `TESTS`, `DEVICE CHECKLIST` (items for the owner), `DEVIATIONS`, `QUESTIONS`.
