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

**Decisions made in Phase 3 (orchestrator).**
- Top bar: Back, a title block with a small status line (clock and battery), Subtitles, Audio, Repeat, Aspect, Lock. The bottom bar (seek row and button row) is `FlowDirection="LeftToRight"` in every language. `HudIconButton` never mirrors its symbol unless `MirrorInRightToLeft` is set (only Back).
- Gesture handling lives in `PlayerGestureHandler`, the indicator in `OsdViewModel`. Volume, brightness and zoom show a held indicator; a seek drag shows `[+0:14]  12:30` and seeks on release.
- The page is immersive while active (`IVideoSurface.SetImmersive`); leaving restores the bars and the system brightness.
- The lock survives the background and the screen turning off; it is released only by the unlock button, a close or an error close. While locked a tap or the Back button shows the unlock button for three seconds.
- The aspect mode at the start of a file is applied by the coordinator. The Aspect button cycles the modes and records the choice for the file.
- The battery level comes from MAUI's `Battery` API; the permission it requires is declared in `BatteryPermission.cs`.
- Texts come from `PlayerStrings` (spec 10). Error alerts show localized generic texts; the engine's own message goes to the debug log.

**Decisions made in the Phase 3 revision (spec 11; they win over the text above).**
- Top bar: the title has the first line to itself. Second line: status (clock and battery), the speed chip, Aspect, the menu button, Lock. There is no Back button (the system Back closes the player) and no Subtitles, Audio, Repeat or decoder button.
- The menu is an action sheet: "Subtitles", "Audio track", "Repeat", "Decoder: HW" or "Decoder: SW". The first three open their sheets; the last one switches the decoder.
- Bottom bar: the seek row and Previous, Play or Pause, Next. Nothing else.
- The system bars follow the controls: `IVideoSurface.SetSystemBarsVisible` is called with the visibility of the HUD. The bars' paddings come from `IVideoSurface.SystemBarInsets`, so the controls keep clear of the bars and of a camera hole. The picture never moves: the page and its root `Grid` set `SafeAreaEdges="None"`, because MAUI otherwise pads the children by the safe area while the bars are visible (SPIKE finding 27).
- Seek drag: playback is paused when the drag starts, each move seeks to the target (only the latest target is sent while a seek is running), so the picture shows the target frame. On release playback continues when it was playing before; a cancelled drag returns to the start position.
- `LongPress` while playing and not locked: the speed becomes `UiSettings.LongPressSpeed` with a held indicator, and the earlier speed returns when the finger lifts. The change is never recorded for the file.

**Decisions made in Phase 4 and the third revision (orchestrator).**
- Seek drag: the engine now completes the seek of a paused video when its picture is shown, so the drag sends a target only when it is the first, at least 300 ms away from the last one sent, or an end of the file; one loop sends the newest target; each drag has its own cancellation, and a finish of an older drag never seeks, writes or plays once a newer drag began.
- A Picture-in-Picture button stands between Aspect and the menu button, for videos, where the device supports it. While the small window is shown, no control, indicator or unlock button is drawn and gestures are ignored.
- The page leaves itself once when the coordinator has no current item (playback was closed from the notification or with the small window), also when that happened while the page was not active. `Deactivate` never pauses, stops or closes playback.

**Decisions made in spec 12 (2026-11-15, owner request; they win over the text above where they differ).**
- The quiet display is visible exactly while a file is open, the controls are hidden, Picture-in-Picture is off, the page is active and no gesture indicator (`Osd.IsVisible`) is on. `PlayerViewModel.UpdateOverlayVisibility` decides it; it runs on every change of those inputs. Nothing is computed while it is hidden.
- Its container is a Grid in the page's root Grid, after the gesture indicator and before the unlock button. It has `InputTransparent` and `SafeAreaEdges="None"` and no padding, so it spans the whole window and never takes touches. The page and root Grid already use `SafeAreaEdges="None"`, so the corner is the real screen corner; the HUD's inset paddings are not applied to it.
- The progress line is a 2-unit `BoxView` whose width is the screen width times `PlaybackOverlayText.Progress`. The page reports its width on `SizeChanged`. The fill uses `ColorProgress` through `DynamicResource`.
- The text line is one `Label` in the corner `Overlay.Corner`, margin `Overlay.MarginDp` on all sides, size and color from `Overlay`, with a dark shadow. Its text is `PlaybackOverlayText.FormatLine`.
- The clock and battery are read when the display appears and then once a minute by a timer that runs only while the display is shown. The clock uses the same `HH:mm` source as the status line; the battery uses the same `ReadBatteryPercent` as the status line.
- The seek row's right label is `-remaining / total` (`PlaybackOverlayText.FormatRemaining`). While the duration is unknown it shows `0:00` as before.

**Out of scope.** Touch recognition, surface rendering, engine code, settings pages, PiP implementation.

**Escalation.** Stop and report when a contract is missing or wrong, a file outside the owned path is needed, an acceptance item cannot be met, or the same step failed twice. Never change a contract.

**Report format.** `STATUS: done | blocked`, `BRANCH`, `FILES`, `BUILD`, `TESTS`, `DEVICE CHECKLIST` (items for the owner), `DEVIATIONS`, `QUESTIONS`.
