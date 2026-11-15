# Agent: media-session-bt

**Mission.** Expose playback to headset buttons, Bluetooth and system media controls through one MediaSession.

**Model and why.** Sonnet 5.5. MediaSession state, actions and media-button routing need platform knowledge.

**Phase.** 4.

**Owned paths.** `src/Player.App/Platforms/Android/Session/**`, `src/Player.Core/Logic/Session/**`.

**Read-only contracts.** Implements `IMediaSessionBridge`. Uses `IPlayerEngine`, `IPlaybackCoordinator`, `IMediaMetadataService`, `IUiSettingsStore`, `PlayerSnapshot`.

**Inputs.** docs/specs/09, docs/SPIKE.md check 2 and finding 1, docs/CONTRACTS.md.

**Deliverables.** `SessionStateMapper` and `MultiPressDetector` (pure, in Core/Logic/Session), `MediaSessionBridge` with its callback (Android), `AddMediaSession()` DI extension.

**Steps.**
1. `SessionStateMapper`: from a `PlayerSnapshot` and queue info produce a plain record (state, position, speed, action flags). No Android types.
2. `MultiPressDetector`: 1, 2 or 3 presses within a 400 ms window between presses map to toggle, next, previous. Uses `TimeProvider`.
3. `ActivateAsync` creates the session once and sets it active. `DeactivateAsync` sets it inactive. The session is released only when the process ends.
4. Subscribe to `IPlayerEngine.SnapshotChanged` and `IPlaybackCoordinator.Changed`. Publish playback state through the mapper. Publish position only on state changes and seeks, with the speed, not on every tick.
5. Publish metadata on item change: title without extension, duration, folder as album, cached thumbnail bitmap when `GetCachedAsync` has one.
6. Callback: play, pause, stop, seek, next, previous, fast forward, rewind. Headset hook goes through `MultiPressDetector`. Every command is posted to the thread pool and calls the contracts. Never call the engine on the callback thread.
7. Expose the session token as `SessionToken`.
8. No Bluetooth API and no Bluetooth permission.

**Acceptance.**
- Build green. Pure classes covered by test-writer.
- `adb shell dumpsys media_session` shows the session active with the right state, position and metadata.
- Device (physical): spec 09 acceptance list.

**Decisions made while building Phase 4 (they win over the steps above).**
- Platform `MediaSession` (no AndroidX media package). Namespaces `Player.Core.Logic.Session` and `Player.App.MediaControls`. The bridge is split into `MediaSessionBridge.cs` and `MediaSessionBridge.Commands.cs`.
- The session is created once on the main thread and never released; it is active while the coordinator has a current item. `ActivateAsync` and `DeactivateAsync` are idempotent entry points for the service.
- `SessionPublishGate` decides when a state is published: a change of state, actions, speed or error, or a position more than 1.5 s away from the extrapolated one.
- `MultiPressDetector` arms one timer per press with a generation number (no elapsed-time check: a timer may fire a moment early).
- The headset hook and the play/pause key are counted on key-down with repeat 0 and always swallowed; other keys go to the base class.
- A custom action "Close" (`SessionStrings`) is published while a file is open: from API 33 the system's media control shows it (spec 08).

**Out of scope.** Notification and foreground service (media-service). Becoming-noisy and audio focus (media-service). Media browser, Android Auto.

**Escalation.** Stop and report when a contract is missing or wrong, a file outside the owned paths is needed, an acceptance item cannot be met, or the same step failed twice. Never change a contract.

**Report format.** `STATUS: done | blocked`, `BRANCH`, `FILES`, `BUILD`, `TESTS`, `DEVICE CHECKLIST` (items for the owner), `DEVIATIONS`, `QUESTIONS`.
