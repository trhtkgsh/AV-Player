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
- The progress line is a 2-unit `BoxView` that spans the full window width (its container spans the window) and is scaled by `PlaybackOverlayText.Progress` with `AnchorX="0"`, so each report costs no layout pass. It uses `ColorProgress` through `DynamicResource`.
- The text line is one `Label` in the corner `Overlay.Corner`, margin `Overlay.MarginDp` on all sides, size and color from `Overlay`, with a shadow (black, offset 1,1, radius 3, opacity 0.8). No shadow or scrim theme token exists (`ColorHudBackground` is the bar fill at user opacity), so the black is a literal. Its text is `PlaybackOverlayText.FormatLine`.
- The clock and battery are read when the display appears. While it is shown, a loop waits until just after each minute boundary (200 ms slack), then reads them again. The loop checks its cancellation token after each wait and runs only while the display is shown. The clock uses the same `HH:mm` source as the status line; the battery uses the same `ReadBatteryPercent` as the status line.
- The seek row's right label is `-remaining / total` (`PlaybackOverlayText.FormatRemaining`). It is rebuilt on each report only while the controls show, and again when they show. While the duration is unknown it shows `0:00` as before.

**R7-20 (2026-11-15, spec 13 items 7-12; they win over the text above where they differ).**
- Quiet display: the progress line height follows `ProgressLineThickness` (clamped to 1..12). The single corner label is replaced by four input-transparent labels, one per physical corner, each with `PlaybackOverlayText.FormatCorner`, hidden when its text is empty. Only corners whose text changed are set (`OverlayCornerText`). Same update rules as before.
- Restart: Previous above 3000 ms (`RestartConfirmMs`, same value as `PlaybackCoordinator.RestartThresholdMs`) asks "Start again from the beginning?" with Yes / No; Previous is called only on Yes. Playback keeps running; the controls do not auto-hide while the question shows; a second question cannot open. At or under 3000 ms, Previous runs at once.
- Screen on: `PlayerScreenKeeper` keeps `DeviceDisplay.Current.KeepScreenOn` on while the engine is Playing and the picture is on screen (page active, or Picture-in-Picture). It is off when paused, ended, idle or failed, when the page is left, and when the app is in the background without Picture-in-Picture. Nothing set the flag before this task.
- Double tap: a tap in the middle third (`FocusX` 1/3 up to 2/3) toggles play and pause. The left and right thirds skip as before; the direction rules, including Hebrew, are unchanged.
- Bottom bar: row spacing 6 to 2, transport column spacing 20 to 12, transport icons 26/36 to 22/30, transport touch targets 44 (`HudIconButton.TouchHeight`), padding 8 to 4 top and bottom.
- R7-21 (2026-11-15): the seek row is a fixed 32 units (slider and row `HeightRequest` 32), there is no spacing between the seek row and the transport row, and the padding is 2 top and bottom plus the bottom inset. The bar is about 80 units plus the inset.

**Out of scope.** Touch recognition, surface rendering, engine code, settings pages, PiP implementation.

**Escalation.** Stop and report when a contract is missing or wrong, a file outside the owned path is needed, an acceptance item cannot be met, or the same step failed twice. Never change a contract.

**Report format.** `STATUS: done | blocked`, `BRANCH`, `FILES`, `BUILD`, `TESTS`, `DEVICE CHECKLIST` (items for the owner), `DEVIATIONS`, `QUESTIONS`.
