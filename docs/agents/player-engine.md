# Agent: player-engine

**Mission.** Wrap LibVLC behind `IPlayerEngine` and `IVideoOutputBinding` as a process-wide singleton.

**Model and why.** Fable 5.2. Threading, native lifetime and surface lifecycle errors crash the process and are hard to test (docs/SPIKE.md findings 1-7).

**Phase.** 1: open, play, pause, seek, stop, snapshot, decoder switch, surface binding. 2: speed, volume, tracks, delays, subtitle file, subtitle style. 4: video enable/disable for audio-only.

**Owned paths.** `src/Player.App/Platforms/Android/Engine/**`.

**Read-only contracts.** Implements `IPlayerEngine`, `IVideoOutputBinding`. Uses the models in docs/CONTRACTS.md.

**Inputs.** docs/specs/02 and 03, docs/ARCHITECTURE.md section 3, docs/SPIKE.md, `spike/EngineSpike/SpikeEngine.cs` as reference only (never copied wholesale).

**Deliverables.** `VlcPlayerEngine` (both interfaces), its command queue, `AddPlayerEngine()` DI extension.

**Steps.**
1. Build one dedicated worker thread with a serialized command queue. Every public method enqueues and returns a Task that completes when the command ran.
2. In each LibVLC event handler copy the data and post to the queue. Never call LibVLC inside a handler.
3. Open media from the item's content URI through a file descriptor from `ContentResolver`. Reopen the descriptor on every restart.
4. Decoder switch: stop, new media with `:codec=mediacodec_ndk,all` or `:codec=all` and `:start-time`, play. Parse `using video decoder module` from the LibVLC log to fill `ActualDecoder`.
5. Own LibVLC's `AWindow`. On `SurfaceReady` attach and report the size. On `SurfaceSizeChanged` report the size. On `SurfaceLost` detach and disable the video track. Re-enable the track only after the next `SurfaceReady`.
6. Publish `PlayerSnapshot` on change, position at most 4 times per second, from the queue thread.
7. Phase 2 and 4 members as listed above, each as one queued command.

**Acceptance.**
- `dotnet build src/Player.App -f net10.0-android` green.
- No LibVLC type appears outside the owned path.
- Device: H.264 and HEVC play in HW and SW. Decoder switch resumes within 1 s of the position. 20 background/foreground cycles, 50 rapid seeks and 20 rapid open/close cycles produce no crash and no black picture.

**Out of scope.** Views, gestures, zoom, aspect (video-surface). Queue, repeat, A-B (playback-coordinator). Service, session, audio focus. Any network input.

**Escalation.** Stop and report when a contract is missing or wrong, a file outside the owned path is needed, an acceptance item cannot be met, or the same step failed twice. Never change a contract.

**Report format.** `STATUS: done | blocked`, `BRANCH`, `FILES`, `BUILD`, `TESTS`, `DEVICE CHECKLIST` (items for the owner), `DEVIATIONS`, `QUESTIONS`.
