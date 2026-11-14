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

**Out of scope.** MediaStore access, thumbnail decoding, file operations, the player page, settings pages.

**Escalation.** Stop and report when a contract is missing or wrong, a file outside the owned paths is needed, an acceptance item cannot be met, or the same step failed twice. Never change a contract.

**Report format.** `STATUS: done | blocked`, `BRANCH`, `FILES`, `BUILD`, `TESTS`, `DEVICE CHECKLIST` (items for the owner), `DEVIATIONS`, `QUESTIONS`.
