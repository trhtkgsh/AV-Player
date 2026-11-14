# Agent: player-hud-ui

**Mission.** Build the player page: HUD, OSD indicators and screen lock, and apply gesture events.

**Model and why.** Haiku 5.5. A page and a ViewModel over existing services, fully specified here and in spec 02.

**Phase.** 1: page, HUD controls, auto-hide, decoder toggle. 3: gesture handling, OSD, lock, aspect cycling, PiP button.

**Owned paths.** `src/Player.App/Features/Player/**`.

**Read-only contracts.** Uses `IPlayerEngine`, `IPlaybackCoordinator`, `IVideoSurface`, `IGestureSource`, `IUiSettingsStore`, `IPlaybackStateRecorder`, `IPictureInPictureController`, and the models. Places `VideoSurfaceView` and `GestureOverlayView` (owned by other agents) in its XAML.

**Inputs.** docs/specs/02, docs/specs/07 (token keys), docs/CONTRACTS.md.

**Deliverables.** `PlayerPage`, `PlayerViewModel`, `OsdViewModel`, `TimeFormatter`, `AddPlayerHud()` DI extension that registers the route `player`.

**Steps.**
1. Page layout: `VideoSurfaceView` at the bottom of a Grid, `GestureOverlayView` above it (Phase 3), HUD layer on top, OSD label centered. Immersive (no nav bar, no system bars) while the page is visible.
2. ViewModel subscribes to `IPlayerEngine.SnapshotChanged` and `IPlaybackCoordinator.Changed` and copies values to observable properties on the UI thread. Unsubscribe when the page disappears. The ViewModel never owns or disposes the engine.
3. HUD: title, seek slider (seek on release, show the target time while dragging), position and duration, play/pause, previous, next, decoder toggle showing `ActualDecoder`, optional clock and battery (MAUI `Battery`).
4. Auto-hide: hide after `UiSettings.HudAutoHideSeconds` while playing. Any interaction restarts the timer. Never hide while paused.
5. Phase 3 gestures, from `IGestureSource.Gesture`:
   - `Tap`: toggle HUD. `DoubleTap`: `SkipAsync(-/+ SkipSeconds)` by side.
   - `VerticalDrag` right: volume = start value + (-DeltaY x 200), clamped 0..200, `SetVolumeAsync`, OSD `Volume 120%`.
   - `VerticalDrag` left: brightness = start + (-DeltaY), clamped 0..1, `SetWindowBrightness`, OSD `Brightness 60%`.
   - `HorizontalDrag`: target = start position + DeltaX x 90 s. OSD `[+00:14]` and target time. Seek on `Ended`, nothing on `Cancelled`.
   - `Pinch` and `Pan`: `IVideoSurface.SetTransform`. OSD `Zoom 150%`.
6. Lock button: sets `IGestureSource.Locked`, hides the HUD, shows only an unlock button after a tap.
7. Aspect button cycles the four modes through `IVideoSurface.SetAspect`, records it with `IPlaybackStateRecorder.Update`, OSD shows the mode name.
8. Hide the HUD while `IPictureInPictureController.IsActive`.
9. On snapshot status Error: show the message, then navigate back.

**Acceptance.**
- Build green. Reviewer approves. `TimeFormatter` is pure and static.
- `[ObservableProperty]` and `[RelayCommand]` only. `DynamicResource` tokens only.
- Device: spec 02 acceptance list.

**Out of scope.** Touch recognition, surface rendering, engine code, settings pages, PiP implementation.

**Escalation.** Stop and report when a contract is missing or wrong, a file outside the owned path is needed, an acceptance item cannot be met, or the same step failed twice. Never change a contract.

**Report format.** `STATUS: done | blocked`, `BRANCH`, `FILES`, `BUILD`, `TESTS`, `DEVICE CHECKLIST` (items for the owner), `DEVIATIONS`, `QUESTIONS`.
