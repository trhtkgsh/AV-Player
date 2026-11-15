# Spec 03: Playback, audio and subtitle settings

Phase: 2. Agents: player-engine (primitives), subtitles-audio (policy), playback-coordinator (repeat, A-B), playback-settings-ui (pages).
Contracts: `IPlayerEngine`, `ISubtitleService`, `IPlaybackCoordinator`, `IPlaybackStateRecorder`, `IGlobalDefaultsStore`.

## Requirements

1. **Speed.** 0.25x to 4.0x in steps of 0.05, with pitch correction. Presets 0.5, 1.0, 1.25, 1.5, 2.0.
2. **A-B repeat.** Mark A, mark B, clear. While set, playback jumps to A when it reaches B. B must be at least 1 s after A. Not persisted.
3. **Repeat.** None, File (restart at end), Folder (continue through the queue and wrap). The mode is a global default.
4. **Audio track.** List tracks with name and language, select one. If the file has no per-file choice, the preferred audio language from `GlobalDefaults` is selected when present.
5. **Audio delay.** -5000 ms to +5000 ms in 50 ms steps.
6. **Subtitles.** Embedded tracks plus external `.srt` and `.ass`/`.ssa`. Off is a selectable option (`-1`).
7. **Sidecar discovery.** When a file opens, subtitle files in the same MediaStore folder whose name starts with the video's base name are offered. The best match loads automatically when `GlobalDefaults.SubtitlesEnabled`.
8. **Manual load.** A system file picker loads any subtitle file. The content is copied into the app data folder (`subtitles`), converted to UTF-8, before it is given to the engine. The copy is kept, so the saved choice of a file survives a cache clear.
9. **Subtitle delay.** -5000 ms to +5000 ms in 50 ms steps.
10. **Subtitle style.** Font scale, text color, outline color and thickness, background opacity, bold, bottom margin. ASS files keep their own styling.
11. **Persistence.** Speed, decoder, audio track, subtitle track, the external subtitle file and both delays are written to the per-file state through `IPlaybackStateRecorder.Update`. Two exceptions, decided in Phase 2: the repeat mode is a global default (requirement 3) and is saved through the coordinator; the subtitle style applies to the running session and reaches the global defaults only through "Save as default" in the style sheet. Nothing else here writes to global defaults.
12. All changes apply to the running playback without leaving the player.

## Known risk

LibVLC 3 applies some subtitle style options only when the video output is created. If a style change needs a restart of the video track, the engine does it at the current position.

## Acceptance

- Unit tests: A-B validation, repeat transitions at end of file, sidecar name matching, preferred language selection.
- Device: speed 0.25x and 4.0x keep the pitch. Delays are audible/visible. An external srt and an ass file load and render Hebrew text correctly (right to left).

## Out of scope

Online subtitle search or download, audio equalizer, audio pass-through, subtitle editing.
