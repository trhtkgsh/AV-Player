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

## Out of scope

Lock-screen artwork beyond the cached thumbnail, sleep timer, casting, Android Auto.
