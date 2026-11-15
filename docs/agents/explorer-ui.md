# Agent: explorer-ui

**Mission.** Build the file explorer pages and ViewModels, including sort and instant search.

**Model and why.** Haiku 5.5. XAML and ViewModels over existing services, fully specified here and in spec 01.

**Phase.** 1: browse, sort, search, progress, open. 5: rename and trash actions.

**Owned paths.** `src/Player.App/Features/Explorer/**`, `src/Player.Core/Logic/Explorer/**`.

**Read-only contracts.** Uses `IMediaLibrary`, `IMediaMetadataService`, `IPlaybackStateStore`, `IUiSettingsStore`, `IPlaybackCoordinator`, `IFileOperations` (Phase 5), and the models.

**Inputs.** docs/specs/01, docs/specs/07 (token keys), docs/CONTRACTS.md.

**Deliverables.** In Core/Logic/Explorer: `NaturalNameComparer`, `MediaSorter`, `SearchFilter`, `ProgressCalculator`. In Features/Explorer: `FoldersPage`, `ItemsPage`, their ViewModels, `MediaRowViewModel`, `AddExplorer()` DI extension that also registers the Shell routes.

**Steps.**
1. `NaturalNameComparer`: case-insensitive, digit runs compared as numbers.
2. `MediaSorter.Sort(items, field, direction, metadata)`: Name, DateAdded, Size, Duration. Stable. Items without a duration sort last.
3. `SearchFilter.Apply(items, text)`: case-insensitive contains on the display name. Empty text returns all.
4. `ProgressCalculator`: fraction 0..1 from position and duration, 0 when the duration is 0.
5. `FoldersPage`: `CollectionView` of `MediaFolder` with name, item count, NEW count. Tap opens `ItemsPage` for the bucket. A toolbar toggle switches to Flat view (`ItemsPage` with no bucket) and saves `UiSettings.ViewMode`.
6. `ItemsPage`: `CollectionView` with virtualization. Row: thumbnail, name, enabled info fields, NEW tag, progress bar, watched mark. Search entry (150 ms debounce), sort menu (saves to `UiSettings`), pull to refresh.
7. Each row asks `GetOrCreateAsync` when it appears and cancels when it disappears. Load cached metadata and states for the whole list first with `GetCachedAsync` and `GetManyAsync`.
8. If `EnsureReadPermissionAsync` returns false show the explanation view with Retry and Open settings.
9. Tap on an item: `IPlaybackCoordinator.OpenAsync(item, currentList)` then navigate to the route `player`.
10. Reload on `IMediaLibrary.Changed` and when the page reappears. All service calls are awaited with a token that is cancelled when the page disappears.
11. Phase 5: long-press menu with Rename (prompt) and Trash (confirm), calling `IFileOperations` and showing the result.

**Acceptance.**
- Build green. Core/Logic/Explorer covered by test-writer. Reviewer approves.
- `[ObservableProperty]` and `[RelayCommand]` only. No logic in code-behind. `DynamicResource` tokens only.
- Device: spec 01 acceptance list.

**Decisions made in the Phase 3 revision (spec 11; they win over the text above).**
- Also uses `IFolderSettingsStore`. New pure logic in Core/Logic/Explorer: `FolderTree` (the media folders as a directory tree, one level at a time) and `GridTiles` (column count).
- "View" opens a list of four modes saved in `UiSettings.ViewMode`: Media folders, File browser (the tree; the system Back goes one level up; a directory with files and sub-directories shows a first row "Files in this folder"), Favorites (folders with `FolderSettings.Favorite`), All files.
- "Grid" or "List" toggles `UiSettings.Layout` for both pages: two columns in portrait, up to four in landscape.
- Folder options open from a three-dot button on the folder's row or tile (MAUI has no long-press recognizer for list rows): add to or remove from favorites, default speed for the folder (a fixed list of speeds and "Use the general default"). File options stay a Phase 5 task.
- Folders with `MediaFolder.AudioOnly` are hidden in every mode while `UiSettings.IncludeAudioOnlyFolders` is false; in "All files" their files are hidden too.
- Folder and file names use `UiSettings.ExplorerFontScale`; detail lines keep their size.

**Decisions made in the third revision (owner's test of 2026-11-15; they win over the text above).**
- The view modes are four direct buttons in a row under the title: "Folders", "Browser", "Favorites", "All files". There is no "View" menu.
- Choosing a default speed for a folder (a value or the general default) clears the own speed of every file of that folder (`IPlaybackStateStore`), so the folder's speed applies to all of them; a later choice in the player wins for that file again. Also uses `IPlaybackStateStore.SaveAsync`.

**Decisions made in P5-20 (2026-11-15; they win over the text above).**
- File rows and tiles open options with the same three-dot button as folders (no long press). Options: "Rename" and "Delete" (`ItemsViewModel.File.cs`).
- Rename prompts with the base name only; the extension is re-added. Validation is in `FileNameRules` (Core/Logic/Explorer). A problem shows its message and re-asks with the typed text; the same name closes quietly.
- Delete wording: "Move {0} to the trash?" on Android 11 and later, otherwise "Delete {0}?" (`DeviceInfo`, so no Android types in the module).
- Success updates the list in place (`MediaListEdits`), with no reload. Cancelled does nothing; Denied and Failed show a short message with the error.
- If the file is the one in playback (including background audio or PiP), playback is closed through `IPlaybackCoordinator.CloseAsync` before the operation.
- Per-file state and metadata are keyed by `MediaFileId` (includes date-modified), so a rename orphans them. Not migrated here; decision for the orchestrator.

**Out of scope.** MediaStore access, thumbnail decoding, file operations, the player page, settings pages.

**Escalation.** Stop and report when a contract is missing or wrong, a file outside the owned paths is needed, an acceptance item cannot be met, or the same step failed twice. Never change a contract.

**Report format.** `STATUS: done | blocked`, `BRANCH`, `FILES`, `BUILD`, `TESTS`, `DEVICE CHECKLIST` (items for the owner), `DEVIATIONS`, `QUESTIONS`.
