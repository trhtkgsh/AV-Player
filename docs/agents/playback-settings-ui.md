# Agent: playback-settings-ui

**Mission.** Build the in-player settings sheets for speed, repeat, A-B repeat, audio and subtitles.

**Model and why.** Haiku 5.5. Pages over existing services, fully specified here and in spec 03.

**Phase.** 2.

**Owned paths.** `src/Player.App/Features/PlaybackSettings/**`.

**Read-only contracts.** Uses `IPlayerEngine`, `IPlaybackCoordinator`, `ISubtitleService`, `IPlaybackStateRecorder`, `IGlobalDefaultsStore` (read only), and the models.

**Inputs.** docs/specs/03, docs/specs/07 (token keys), docs/CONTRACTS.md.

**Deliverables.** `SpeedSheet`, `RepeatSheet`, `AudioSheet`, `SubtitleSheet`, `SubtitleStyleSheet` with one ViewModel each, `AddPlaybackSettings()` DI extension that registers the routes `player/speed`, `player/repeat`, `player/audio`, `player/subtitles`, `player/subtitle-style`.

**Steps.**
1. Each sheet is a modal page that keeps the video visible behind a translucent panel (`ColorHudBackground`).
2. `SpeedSheet`: slider 0.25-4.0 step 0.05, preset buttons 0.5, 1.0, 1.25, 1.5, 2.0, reset. Calls `SetSpeedAsync` and records `Speed` with `IPlaybackStateRecorder.Update`.
3. `RepeatSheet`: None / File / Folder through `IPlaybackCoordinator.SetRepeatAsync`. A-B: "Set A" and "Set B" take the current position, "Clear" removes it. Disable "Set B" until A is set. Reject B less than 1 s after A with a message.
4. `AudioSheet`: track list from `IPlayerEngine.AudioTracks` with the current one checked, `SelectAudioTrackAsync`. Delay stepper -5000..+5000 ms step 50, `SetAudioDelayAsync`. Record both.
5. `SubtitleSheet`: track list from `SubtitleTracks` plus "Off" (-1), `SelectSubtitleTrackAsync`. Sidecar list from `FindSidecarsAsync`, tap loads with `LoadExternalAsync`. "Open file" calls `PickAndLoadAsync`. Delay stepper as for audio with `SetSubtitleDelayAsync`. Record track and delay.
6. `SubtitleStyleSheet`: font scale 0.5-3.0, text color and outline color from a fixed palette, outline 0-4, background opacity 0-100%, bold, bottom margin 0-30%. Calls `SetSubtitleStyleAsync` on each change. The style is saved to global defaults only through a "Save as default" button that calls `IGlobalDefaultsStore.SaveAsync`. This is the only write to global defaults from this agent.
7. Refresh the lists on `IPlayerEngine.SnapshotChanged`. Marshal to the UI thread. Unsubscribe on disappear.
8. Every engine call is awaited. A failure shows a short message and leaves the control at the previous value.

**Acceptance.**
- Build green. Reviewer approves.
- `[ObservableProperty]` and `[RelayCommand]` only. No logic in code-behind. `DynamicResource` tokens only.
- Device: spec 03 acceptance list.

**Out of scope.** Engine behavior, sidecar matching, repeat and A-B logic, the global defaults page, the HUD.

**Escalation.** Stop and report when a contract is missing or wrong, a file outside the owned path is needed, an acceptance item cannot be met, or the same step failed twice. Never change a contract.

**Report format.** `STATUS: done | blocked`, `BRANCH`, `FILES`, `BUILD`, `TESTS`, `DEVICE CHECKLIST` (items for the owner), `DEVIATIONS`, `QUESTIONS`.
