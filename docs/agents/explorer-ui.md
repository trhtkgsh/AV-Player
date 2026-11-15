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
- Per-file state and metadata are keyed by `MediaFileId` (includes date-modified). The file-ops task moves the saved state to the new id on rename and deletes it on trash. The view model re-keys its in-memory `_states` and `_metadata` entries the same way (P5-21), so the row keeps its thumbnail, duration and progress until the next reload.
- Known gap (accepted): the coordinator's queue can still hold a renamed or deleted item; a later Next may use the stale item. Left alone, no coordinator change.

**Out of scope.** MediaStore access, thumbnail decoding, file operations, the player page, settings pages.

**Escalation.** Stop and report when a contract is missing or wrong, a file outside the owned paths is needed, an acceptance item cannot be met, or the same step failed twice. Never change a contract.

**Report format.** `STATUS: done | blocked`, `BRANCH`, `FILES`, `BUILD`, `TESTS`, `DEVICE CHECKLIST` (items for the owner), `DEVIATIONS`, `QUESTIONS`.

**Decisions made for spec 13, items 5 and 6 (2026-11-15, R7-40; they win over the text above).**
- Item size: `UiSettings.ItemSize` (Large, Small) applies to file rows and tiles and folder tiles in every mode. The explorer only reads it and follows the store's change at once (`RebuildRows` and `ApplySettings`).
- Large is the look before the setting. Small list: file thumbnail 48 x 27 (Large 96 x 54), progress bar as wide as the thumbnail. Small grid: three columns in portrait, six at most in landscape (Large two to four); folder symbol 36 x 28 (Large 72 x 56). Sizes come from `ItemSizes` (Core/Logic/Explorer).
- Folder list rows have no symbol today, so Small adds none (open question for the owner).
- Name only: an empty details line is hidden (`MediaRowViewModel.HasInfoLine`) and takes no space in a row or tile. The NEW and Watched marks and the progress bar stay. Grid marks wrap in a `FlexLayout` so they stay inside the tile at a large font scale.
- Names keep `ExplorerFontScale`; no fixed heights on names. A tile's ⋮ button sits inside the thumbnail's top-end corner.
- Pure signatures changed or added: `GridTiles.Columns(double width, ExplorerItemSize size)` (new overload; `Columns(double)` kept for Large), `GridTiles.SmallMinTileWidth` (120), `GridTiles.SmallMinColumns` (3), `GridTiles.SmallMaxColumns` (6); new `ItemSizes.ListThumbnailWidth(size)`, `ListThumbnailHeight(size)`, `FolderIconWidth(size)`, `FolderIconHeight(size)`. No existing test covered `GridTiles`.
- View models: `ItemsViewModel` and `FoldersViewModel` keep the list width and derive columns from width and size; `ItemsViewModel` exposes `ListThumbnailWidth` and `ListThumbnailHeight`; `FoldersViewModel` exposes `FolderIconWidth` and `FolderIconHeight`; `MediaRowViewModel` exposes `HasInfoLine`.

**Decisions made for spec 13, section A items 1, 3, 4 (2026-11-16, R7-41; they win over the text above).**
- Menus: the file ⋮ menu has Rename, Delete, Move, Copy. Move and Copy run through one `TransferAsync` path for the ⋮ menu and the selection.
- Selection mode (items and folders pages, every mode): a long press on a row or tile starts it with that item selected. `LongPressBehavior` (App, `#if ANDROID`) sets the native view's `LongClickable` and hooks `LongClick`; it runs `BeginSelect` and unhooks on detach. A tap within 600 ms after a long press is ignored, so the press does not also toggle (a guess, not measured on a device). In the mode, a tap toggles; selected rows and tiles get a tint (`ColorAccent` at 0.2 opacity). The toolbar shows Select all, Rename (exactly one file, not a folder), Move, Copy, Delete, and the count as the title. Sort, layout, search and Settings are hidden in the mode. Back and the bar's back arrow leave the mode. Clearing the last item leaves it. The ⋮ buttons are hidden in the mode. Leaving the page clears the selection.
- A selected folder stands for its media files (`IMediaLibrary.GetItemsAsync` with its bucket; no duplicates). The delete confirmation names the files, or "files in N folders".
- Picker: `FolderPickerPage` (modal, in a `NavigationPage`) lists `GetFoldersAsync` in list order, audio-only folders included. The source folder is left out only when all selected files share one folder. One tap chooses; Cancel or leaving the page gives no folder.
- Running: move closes an open file first, and copy does not. A busy state covers the list with the operation's name (`Explorer.OperationMove`, `Explorer.OperationCopy`, `Explorer.OperationDelete`). Only copy has a Cancel button, wired to a linked token. Input is blocked by `_fileOperationRunning`.
- After a batch: the selection mode ends. Success of one file moves or deletes it in place. Success of several files reloads once. Copy reloads only when the shown folder is the target, or when the list is All media (its list includes every folder). Cancelled reloads only when `CompletedCount` is above 0. Denied and Failed reload and show the localized partial line (`Explorer.SomeNotMoved`, `Explorer.SomeNotCopied`, `Explorer.SomeNotDeleted`) with the implementation's English error. Folders always reload.
- Pure signatures (Core/Logic/Explorer): `ExplorerSelection<TKey>` with `Count`, `IsActive`, `Keys`, `Contains(key)`, `Toggle(key)` (returns the new state), `Add(key)`, `AddRange(keys)`, `Remove(key)`, `Clear()`; `enum SelectionAction { Delete, Move, Copy, Rename }`; static `SelectionRules.Allows(SelectionAction action, int selectedFiles, int selectedFolders)` (Rename: exactly one file and no folder; the others: at least one row); `SelectionRules.Distinct<T, TKey>(IEnumerable<T>, Func<T, TKey>)`; `SelectionRules.SingleBucket(IEnumerable<long>)` (null when they differ or none); `SelectionRules.TargetChoices(IEnumerable<MediaFolder>, long? sourceBucket)`.
- App-side (not pure, for reference): `SelectionOperations` (`FilesAsync`, `ConfirmDeleteAsync`, `TrashAsync`, `MoveAsync`, `CopyAsync`, `ChooseTargetAsync`, `FailureText`); the constructor now takes `IUiSettingsStore`.
- Texts (EN and HE, in `ExplorerStrings`): `Explorer.ChooseFolder`, `Explorer.Copy`, `Explorer.Move`, `Explorer.OperationCopy`, `Explorer.OperationMove`, `Explorer.SelectAll`, `Explorer.SelectedCount`, `Explorer.SomeNotCopied`, `Explorer.SomeNotDeleted`, `Explorer.SomeNotMoved`, `Explorer.FilesInFolders`, `Explorer.Files.One/Many`, `Explorer.OperationDelete`.
- Not verified on a device: that `LongClick` coexists with the row's tap and the ⋮ button, and that the 600 ms guard is right. Known gap (from R7-40): queued coordinator items may keep a stale id after rename or delete.

**Long press fix (2026-11-17, R7-42; it wins over the R7-41 text above on the long press and the tap).**
- Cause: the R7-41 rows had a MAUI `TapGestureRecognizer`. MAUI installs a touch listener on the platform view that takes the touch stream, so Android's `onTouchEvent` never ran and `LongClick` never fired. The release then opened the file.
- Fix: rows and tiles (items and folders pages, every mode) have no MAUI tap recognizer. `LongPressBehavior` (App, `#if ANDROID`) sets `Clickable` and `LongClickable` on the native view and hooks `Click` (runs `TapCommand`, i.e. `Open` or `OpenRow`) and `LongClick` (runs `LongPressCommand`, i.e. `BeginSelect`, with the row as parameter). It unhooks on detach and on handler change, since recycled rows get new native views. Android sends no `Click` after a `LongClick`, so the 600 ms tap guard was removed from both view models. The ⋮ button is a native button and takes its own clicks. Scrolling cancels both `Click` and `LongClick` on the native side.
- Behavior properties: `TapCommand`, `LongPressCommand`, `CommandParameter` (the old `Command` is gone). Overlays that block input during a batch keep their MAUI `TapGestureRecognizer`; they are not rows.
- Pure signatures: unchanged.
- Not verified on a device: the user's emulator check of the long press, the tap, and the scroll.
