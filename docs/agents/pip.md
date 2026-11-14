# Agent: pip

**Mission.** Implement Picture-in-Picture for the player and its remote actions.

**Model and why.** Sonnet 5.5. PiP depends on activity configuration, lifecycle order and per-version APIs.

**Phase.** 4.

**Owned paths.** `src/Player.App/Platforms/Android/Pip/**`.

**Read-only contracts.** Implements `IPictureInPictureController`. Uses `IPlayerEngine`, `IPlaybackCoordinator`, `IVideoOutputBinding` (format for the aspect ratio), `IGlobalDefaultsStore`, `IAppLifecycle`.

**Inputs.** docs/specs/08 requirements 7 and 8, docs/CONTRACTS.md.

**Deliverables.** `PictureInPictureController`, a broadcast receiver for the PiP actions, `AddPictureInPicture()` DI extension, and a written wiring request for app-skeleton (see step 1).

**Steps.**
1. PiP needs `SupportsPictureInPicture = true` and the PiP `ConfigurationChanges` on `MainActivity`, plus forwarding of `OnPictureInPictureModeChanged` and `OnUserLeaveHint`. `MainActivity` belongs to app-skeleton. Write the exact attribute values and the two forwarding calls in the report. Do not edit the file.
2. `IsSupported`: API 26+ and the system feature is present.
3. `EnterAsync`: build `PictureInPictureParams` with the aspect ratio from the video format (clamped to the allowed range), the source rect hint, and three `RemoteAction`s (previous, play/pause, next). Return false when not supported or no video is playing.
4. Update the params when the format or the playing state changes (play/pause icon). On API 31+ set auto-enter and seamless resize.
5. Actions arrive in a non-exported receiver registered only while PiP is active. They call `IPlaybackCoordinator` and `IPlayerEngine` through the contracts.
6. Raise `Changed` when PiP starts and ends. When the PiP window is closed by the user (activity stops while in PiP) call `IPlaybackCoordinator.CloseAsync`.
7. Auto-enter on leave when `GlobalDefaults.PictureInPictureOnLeave` is on and a video is playing.

**Acceptance.**
- Build green.
- Device (physical): enter and leave PiP 10 times, rotate in PiP, each action works, the window ratio matches a 16:9 and a vertical video, closing the window stops playback.

**Out of scope.** Hiding the HUD in PiP (player-hud-ui reacts to `Changed`). Background audio (media-service). Editing `MainActivity`.

**Escalation.** Stop and report when a contract is missing or wrong, a file outside the owned path is needed, an acceptance item cannot be met, or the same step failed twice. Never change a contract.

**Report format.** `STATUS: done | blocked`, `BRANCH`, `FILES`, `BUILD`, `TESTS`, `DEVICE CHECKLIST` (items for the owner), `DEVIATIONS`, `QUESTIONS`.
