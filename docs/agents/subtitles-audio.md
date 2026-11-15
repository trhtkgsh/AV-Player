# Agent: subtitles-audio

**Mission.** Implement subtitle loading and the audio/subtitle track policy on top of the engine primitives.

**Model and why.** Sonnet 5.5. Needs scoped-storage knowledge (sidecar files are other MediaStore or SAF documents) and careful matching logic.

**Phase.** 2.

**Owned paths.** `src/Player.App/Platforms/Android/Subtitles/**`, `src/Player.Core/Logic/Subtitles/**`.

**Read-only contracts.** Implements `ISubtitleService`. Uses `IPlayerEngine`, `IPlaybackCoordinator`, `IPlaybackStateRecorder`, `GlobalDefaults`, `TrackInfo`, `MediaItem`.

**Inputs.** docs/specs/03 requirements 4-10, docs/CONTRACTS.md, docs/ARCHITECTURE.md section 3.

**Deliverables.** `SidecarMatcher` and `PreferredTrackSelector` (pure, in Core/Logic/Subtitles), `SubtitleService` (Android), `AddSubtitlesAudio()` DI extension.

**Steps.**
1. `SidecarMatcher`: given a video display name and candidate file names, return the candidates ordered by match quality. Match: same base name, optional `.lang` suffix, extensions `.srt`, `.ass`, `.ssa`. Case-insensitive.
2. `PreferredTrackSelector`: given `TrackInfo` lists and the preferred languages, return the track ids to select, or null to keep the engine's default.
3. `FindSidecarsAsync`: query MediaStore Files for the item's bucket and filter with `SidecarMatcher`. Return content URIs. Return an empty list when the files are not visible to the app.
4. `LoadExternalAsync`: copy the content URI to a file in the app data folder `subtitles` (keep the extension; not the cache, so a saved choice survives a cache clear), call `IPlayerEngine.AddSubtitleFileAsync`, record the URI with `IPlaybackStateRecorder.Update`.
5. `PickAndLoadAsync`: system document picker (`ACTION_OPEN_DOCUMENT`), then `LoadExternalAsync`.
6. `ApplyPreferredTracksAsync`: use `PreferredTrackSelector` and the engine's select methods. Skip any track the per-file state already fixes.
7. Delete cached subtitle copies older than 30 days.

**Acceptance.**
- Build green. Pure classes covered by test-writer.
- Device: a video with `name.srt` and `name.he.ass` next to it offers both. Hebrew text renders right to left. The picker loads a file from Downloads. The chosen subtitle is restored when the file is reopened.

**Out of scope.** Settings pages (playback-settings-ui). Engine internals. Subtitle download or search. Subtitle rendering.

**Escalation.** Stop and report when a contract is missing or wrong, a file outside the owned paths is needed, an acceptance item cannot be met, or the same step failed twice. Never change a contract.

**Report format.** `STATUS: done | blocked`, `BRANCH`, `FILES`, `BUILD`, `TESTS`, `DEVICE CHECKLIST` (items for the owner), `DEVIATIONS`, `QUESTIONS`.
