# Spec 02: Player and HUD

Phase: 1 (basic playback and HUD). Phase 3 (aspect modes, zoom/pan, gestures, lock).
Agents: player-engine, video-surface, gestures, player-hud-ui, playback-coordinator. Contracts: `IPlayerEngine`, `IVideoOutputBinding`, `IVideoSurface`, `IGestureSource`, `IPlaybackCoordinator`, `IUiSettingsStore`.

## Requirements

1. **Playback.** Opens a `MediaItem` by content URI through a file descriptor. Play, pause, seek, stop, next, previous.
2. **Decoder switch.** HW/SW toggle in the HUD during playback. Playback resumes at the same position. The HUD shows the actual decoder (`PlayerSnapshot.ActualDecoder`), which can differ from the requested one.
3. **HUD.** Title, seek bar with position and duration, play/pause, previous/next, decoder, aspect, lock, speed, tracks, optional clock and battery (`UiSettings`). Auto-hides after `UiSettings.HudAutoHideSeconds` while playing. A single tap toggles it.
4. **Gestures** (native touch, reported through `IGestureSource`, applied by the HUD ViewModel):
   - Vertical drag on the right half: volume 0-200%, with an OSD value.
   - Vertical drag on the left half: window brightness, with an OSD value.
   - Horizontal drag: seek preview with an indicator such as `[+00:14]`, applied on release.
   - Pinch: zoom 100-400%. Drag while zoomed: pan, clamped to the picture edges.
   - Double tap left/right: skip back/forward by `UiSettings.SkipSeconds`.
5. **Screen lock.** While locked all gestures and buttons are ignored except the unlock control.
6. **Aspect modes.** Fit, Stretch, Crop, 100%. Cycled from the HUD. Zoom and pan reset when the mode changes.
7. **Surface lifecycle.** Returning from the background restores the picture without restarting audio.
8. **Orientation.** Player page follows the sensor. The HUD layout works in portrait and landscape.
9. **Errors.** An unplayable file shows a message and returns to the explorer. No crash.

## Constraints from the spike (docs/SPIKE.md)

Findings 1-8 apply. In particular: no LibVLC call from a LibVLC callback, engine commands serialized off the UI thread, explicit window-size reports, surface re-attach after backgrounding, sharp zoom needs more than a view transform.

## Acceptance

- Unit tests: gesture classification thresholds, surface geometry for each aspect mode, zoom/pan clamping.
- Device: 1080p H.264 and HEVC files play in HW and SW. Decoder switch resumes within 1 s of the position.
- Device: all gestures respond without visible lag. Lock blocks everything except unlock.
- Device: 20 background/foreground cycles while playing never leave a black picture.

## Out of scope

Streaming, casting, playlists beyond the current folder queue, video filters, screenshots.
