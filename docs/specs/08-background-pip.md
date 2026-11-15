# Spec 08: Background play and Picture-in-Picture

Phase: 4. Agents: media-service, pip, player-engine (video enable/disable), video-surface (surface lifecycle).
Contracts: `IPlaybackServiceController`, `IPictureInPictureController`, `IMediaSessionBridge`, `IPlayerEngine`, `IAppLifecycle`.

## Requirements

1. **Foreground service.** Type `mediaPlayback`, with a media-style notification (title, play/pause, previous, next, close) bound to the media session. It wraps the existing engine and session singletons. No engine code moves.
2. **Start and stop.** The service starts when playback starts and stops when playback is stopped or closed, or after 10 minutes paused in the background.
3. **Background audio.** When `GlobalDefaults.BackgroundAudio` is on and the app leaves the foreground, playback continues with the video track disabled. When the app returns, the video track is enabled again and the picture is restored. When the setting is off, playback pauses on leave.
4. **Audio focus.** Request on play. Pause on loss and on transient loss, resume after a transient loss only if we paused because of it. Lower volume on "can duck".
5. **Becoming noisy.** Pause when headphones are unplugged or a Bluetooth audio device disconnects.
6. **Notification permission.** Ask for `POST_NOTIFICATIONS` on API 33+ when background audio is first enabled. Playback still works if it is denied.
7. **Picture-in-Picture.** Entered from a HUD button, and automatically on leaving the player when `GlobalDefaults.PictureInPictureOnLeave` is on and a video is playing. The window uses the video's aspect ratio. Actions: play/pause, previous, next. The HUD is hidden in PiP.
8. **PiP and background audio.** If both are enabled, PiP wins while a video is playing. Closing the PiP window stops playback.
9. **State.** Per-file state is saved on every transition (spec 04).
10. **No surface.** The engine never keeps a video output alive without a surface (SPIKE finding 7).

## Acceptance

- Device (physical, required): 30 minutes of background audio with the screen off, on a phone with default battery settings.
- Device: focus loss from a phone call and from another media app, headphones unplugged.
- Device: enter and leave PiP 10 times, rotate in PiP, use each PiP action.
- Device: force-stop during background play, reopen: position was saved.

## Decisions made while building Phase 4 (orchestrator, 2026-11-15)

- **Close button (requirement 1).** On API 26 to 32 it is an action of the notification. From API 33 Android draws the media notification from the media session, so the close button is a custom action of the session. Where Android lets the user swipe the notification away (while paused), that also closes playback; this was not run on a device.
- **Stop (requirement 2).** The service stops when playback is closed (Back in the player, the close button, the dismissed Picture-in-Picture window, the app removed from the recent apps list) or after 10 minutes without playing in the background. It does not stop when a file ends in the background: the next file or a repeat may follow, and a stopped service cannot start again from the background.
- **Background (requirement 3).** "Leaves the foreground" means the activity is stopped. Picture-in-Picture is not that: the activity is only paused there. With the setting on, the picture is switched off in the background whatever the status, and on again when the app returns.
- **Can duck (requirement 4).** Left to Android, which lowers the app's volume itself from API 26. To check on the phone.
- **Becoming noisy (requirement 5).** The receiver follows playback (registered while a file plays), not the service.
- **Notification permission (requirement 6).** Asked by the defaults page each time "Keep playing sound in the background" is turned on; Android shows its dialog only while it may.
- **Picture-in-Picture alone (requirements 3 and 7).** When only "Picture-in-Picture when leaving the player" is on, a playing video goes to the small window on leave and keeps playing: Picture-in-Picture wins over "pause on leave" as well.
- **Closing the small window (requirement 8).** Android gives no "dismissed" callback and the order of its callbacks differs between versions; the controller treats a stop without a return to the full screen as a dismissal, except when the screen is off or the device is locked.
- **Both settings off.** Leaving the app pauses playback, as before Phase 4. Both settings are off by default.

## Out of scope

Lock-screen artwork beyond the cached thumbnail, sleep timer, casting, Android Auto.
