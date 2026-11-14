# Agent: media-service

**Mission.** Keep playback alive in the background with a `mediaPlayback` foreground service that wraps the existing singletons.

**Model and why.** Fable 5.2. Foreground-service rules, audio focus and process lifetime differ per Android version and fail only on real devices.

**Phase.** 4.

**Owned paths.** `src/Player.App/Platforms/Android/Service/**`.

**Read-only contracts.** Implements `IPlaybackServiceController`. Uses `IPlayerEngine`, `IPlaybackCoordinator`, `IMediaSessionBridge`, `IGlobalDefaultsStore`, `IAppLifecycle`, `IPlaybackStateRecorder`.

**Inputs.** docs/specs/08, docs/ARCHITECTURE.md section 3, docs/SPIKE.md check 2 and finding 7, `spike/EngineSpike/Platforms/Android/SpikePlaybackService.cs` as reference only.

**Deliverables.** `PlaybackService` (`[Service]`, type mediaPlayback), notification builder, audio-focus handler, becoming-noisy receiver, `PlaybackServiceController`, `AddMediaService()` DI extension, permissions as assembly attributes.

**Steps.**
1. Declare `FOREGROUND_SERVICE`, `FOREGROUND_SERVICE_MEDIA_PLAYBACK` and `POST_NOTIFICATIONS` with `[assembly: UsesPermission]` in the owned path.
2. The service resolves the singletons from the app container. It creates no engine and no session.
3. Start the service when the snapshot becomes Playing. Call `StartForeground` within 5 s with a media-style notification bound to `IMediaSessionBridge.SessionToken`.
4. Stop it on Idle, on close, and after 10 minutes paused in the background.
5. Audio focus: request on play, pause on loss and transient loss, resume only after a transient loss we paused for, duck on can-duck.
6. Becoming-noisy: pause.
7. On `IAppLifecycle.Backgrounded`: if `BackgroundAudio` is on call `SetVideoEnabledAsync(false)`, else pause. On `Foregrounded` enable video again.
8. Flush state (`IPlaybackStateRecorder.FlushAsync`) on every transition and in `OnTaskRemoved`.

**Acceptance.**
- Build green. No engine or session instance is created in the owned path.
- Device (physical): spec 08 acceptance list items for background audio, focus loss, unplug, force-stop.
- `adb shell dumpsys activity services` shows `isForeground=true` with type mediaPlayback while playing.

**Out of scope.** MediaSession content and transport callbacks (media-session-bt). Picture-in-Picture (pip). Engine internals.

**Escalation.** Stop and report when a contract is missing or wrong, a file outside the owned path is needed, an acceptance item cannot be met, or the same step failed twice. Never change a contract.

**Report format.** `STATUS: done | blocked`, `BRANCH`, `FILES`, `BUILD`, `TESTS`, `DEVICE CHECKLIST` (items for the owner), `DEVIATIONS`, `QUESTIONS`.
