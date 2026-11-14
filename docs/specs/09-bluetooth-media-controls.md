# Spec 09: Bluetooth and hardware media controls

Phase: 4. Agent: media-session-bt. Contracts: `IMediaSessionBridge`, `IPlaybackCoordinator`, `IPlayerEngine`, `IMediaMetadataService`.

## Requirements

1. **Session.** One `MediaSession` for the app's lifetime, created by a singleton and active while a file is open. It is not owned by a page or by the service.
2. **State.** The session's playback state (playing, paused, buffering, stopped), position, speed and available actions follow `IPlayerEngine.SnapshotChanged`. Position is published with its speed so remote displays can extrapolate.
3. **Metadata.** Title (file name without extension), duration, folder name as album, and the cached thumbnail as artwork when it exists.
4. **Transport commands.** Play, pause, play/pause toggle, stop, next, previous, seek to, fast forward and rewind by `UiSettings.SkipSeconds`. Next and previous go through `IPlaybackCoordinator`.
5. **Headset button.** Single press toggles play/pause. Double press: next. Triple press: previous.
6. **Bluetooth.** AVRCP play, pause, next and previous work. Title and position appear on devices that show them.
7. **Disconnect.** Playback pauses when the audio device disconnects. This is implemented by the becoming-noisy receiver in media-service (spec 08). This agent only verifies that the session state follows.
8. **Threading.** Session callbacks never call the engine directly on the callback thread. They enqueue through the contracts.
9. **Token.** `SessionToken` is exposed for the service notification.
10. **No connectivity code.** The app never scans, pairs or manages Bluetooth. It needs no Bluetooth permission.

## Acceptance

- Unit tests: mapping from `PlayerSnapshot` to session state and actions, multi-press detection timing.
- Device (physical, required): wired headset button (1, 2, 3 presses), Bluetooth headset buttons, a car or speaker that shows title and position, disconnect while playing.
- `adb shell dumpsys media_session` shows the session active with correct state, position and metadata.

## Out of scope

Media browser service, Android Auto, Wear OS, voice assistant actions, custom session commands.
