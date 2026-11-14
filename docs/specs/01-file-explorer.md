# Spec 01: File explorer

Phase: 1 (browse, thumbnails, sort, search, progress). Phase 5 (rename, trash).
Agents: media-library, thumbnail-cache, explorer-ui, file-ops. Contracts: `IMediaLibrary`, `IMediaMetadataService`, `IFileOperations`, `IPlaybackStateStore`, `IUiSettingsStore`, `IPlaybackCoordinator`.

## Requirements

1. **Source.** All items come from MediaStore (video and audio collections). No direct file-system scan.
2. **Permission.** On first open ask for `READ_MEDIA_VIDEO` and `READ_MEDIA_AUDIO` (API 33+) or `READ_EXTERNAL_STORAGE` (API 26-32). If denied, show an explanation with a retry button and a link to app settings.
3. **Views.** Folders view (one row per MediaStore bucket with item count and NEW count) and Flat view (all items). The choice is stored in `UiSettings.ViewMode`.
4. **NEW tag.** An item is NEW when it was added within `UiSettings.NewTagDays` days and has no `PlaybackState`.
5. **Row content.** Thumbnail, name, and the fields enabled in `UiSettings.ExplorerFields` (duration, resolution, size, date, path).
6. **Lazy metadata.** Thumbnail, resolution and duration are produced only for rows that become visible, with bounded concurrency (max 2), cancelled when the row leaves the screen, and cached in SQLite + JPEG files. A cached row never triggers decoding again.
7. **Progress.** Each row shows a progress bar from `PlaybackState.PositionMs / DurationMs`, and a "watched" mark when `Finished` (> 95%).
8. **Sort.** Name, date added, size, duration. Ascending or descending. Stored in `UiSettings`. Name sort is natural ("ep2" before "ep10") and case-insensitive.
9. **Search.** Instant filter by name as the user types (debounce 150 ms), case-insensitive, on the current view.
10. **Open.** Tapping an item calls `IPlaybackCoordinator.OpenAsync(item, queue)` where the queue is the current list in its current order.
11. **Refresh.** The list refreshes on `IMediaLibrary.Changed` and on pull-to-refresh.
12. **Rename.** Changes the display name through MediaStore. On API 30+ uses the system write-consent dialog.
13. **Trash.** Moves items to the MediaStore trash (API 30+) with the system consent dialog. On API 26-29 deletes after an in-app confirmation.
14. After rename or trash the per-file state and cached metadata follow the new `MediaFileId` or are removed.

## Acceptance

- Unit tests: natural sort, search filter, NEW rule, watched rule, folder grouping.
- Device: 500+ files scroll without visible stutter, thumbnails appear lazily, no thumbnail is regenerated on second launch.
- Device: rename and trash work on API 30+ with the system dialog and survive an app restart.

## Out of scope

Network locations, SD-card SAF trees, playlists, file copy/move, hidden files.
