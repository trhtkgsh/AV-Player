# Spec 04: Per-file playback state

Phase: 2. Agents: playback-state-store, playback-coordinator (reads on open). Contracts: `IPlaybackStateStore`, `IPlaybackStateRecorder`, `IAppLifecycle`, `IPlayerEngine`.

## Requirements

1. **Key.** State is keyed by `MediaFileId` (MediaStore id + size + modified date). Never by path.
2. **Content.** Position, duration, finished flag, last-played time, and the optional per-file overrides listed in `PlaybackState`. A null override means "not set".
3. **Storage.** Table `playback_state` (docs/ARCHITECTURE.md section 4). Overrides are stored as JSON so new fields need no migration.
4. **Debounce.** While the position changes, save at most once every 5 s.
5. **Immediate save** on: pause, stop, end of file, switching to another item, `IAppLifecycle.Backgrounded`, `IAppLifecycle.Stopping`, and every `Update` of an override.
6. **Finished.** `Finished = PositionMs > 0.95 * DurationMs`. A finished file opens from the start.
7. **Resume.** The coordinator reads the state on open and passes it to `PlaybackSettingsResolver`.
8. **Process death.** After the process is killed at any moment, at most 5 s of position is lost.
9. **Lookup for the explorer.** `GetManyAsync` returns states for a list of ids in one query.
10. **Separation.** This store never reads or writes global defaults.
11. **Cleanup.** `DeleteAsync` removes one file's state (used by file-ops after trash).

## Acceptance

- Unit tests with a fake `TimeProvider`: debounce timing, immediate-save triggers, finished rule, no save when nothing changed, flush on lifecycle events.
- Data tests against a real SQLite file: round trip of every field, `GetManyAsync`, null overrides stay null.
- Device: play, force-stop the app from system settings, reopen the file: it resumes within 5 s of the last position.

## Out of scope

Cloud sync, history screen, export/import.
