# Agent: media-library

**Mission.** Read the device's video and audio files from MediaStore and group them by folder.

**Model and why.** Sonnet 5.5. Needs MediaStore and scoped-storage knowledge across API 26-36 (permissions, volumes, column availability).

**Phase.** 1.

**Owned paths.** `src/Player.App/Platforms/Android/Library/**`, `src/Player.Core/Logic/Library/**`.

**Read-only contracts.** Implements `IMediaLibrary`. Uses `MediaItem`, `MediaFolder`, `MediaQuery`, `MediaFileId`, `IPlaybackStateStore`.

**Inputs.** docs/specs/01 requirements 1-4 and 11, docs/CONTRACTS.md, docs/ARCHITECTURE.md section 3 (file identity).

**Deliverables.** `FolderGrouper` and `NewTagRule` (pure, in Core/Logic/Library), `MediaStoreLibrary` (Android), `AddMediaLibrary()` DI extension, permissions as assembly attributes.

**Steps.**
1. Declare `READ_MEDIA_VIDEO`, `READ_MEDIA_AUDIO` and `READ_EXTERNAL_STORAGE` (maxSdkVersion 32) with `[assembly: UsesPermission]` in the owned path.
2. `EnsureReadPermissionAsync`: check and request the right permission for the API level through MAUI Permissions. Return false when denied. Accept partial access (API 34 "selected media") as granted.
3. `GetItemsAsync`: query the video and audio collections on a background thread with a projection of only the needed columns. Build `MediaFileId` from `_ID`, `SIZE`, `DATE_MODIFIED`. Build the content URI with `ContentUris.WithAppendedId`. Skip pending and trashed rows. Honor the `CancellationToken` between rows.
4. `FolderGrouper`: group items by `BucketId`, count items, count NEW items using `NewTagRule`.
5. `NewTagRule`: NEW = added within N days and no playback state.
6. `GetFoldersAsync`: items + states (`GetManyAsync`) + `FolderGrouper`.
7. Register a `ContentObserver` on both collections. Raise `Changed` at most once per second.
8. Never use a file path to open or identify a file. `RelativePath` is display text.

**Acceptance.**
- Build green. Pure classes covered by test-writer.
- Device: API 30+ and, if available, API 29 or lower. 500+ files load in under 2 s. Denying the permission returns false without a crash. Adding a file with another app refreshes the list.

**Out of scope.** Thumbnails and durations (thumbnail-cache). Sort and search (explorer-ui). Rename and trash (file-ops). SAF folder trees.

**Escalation.** Stop and report when a contract is missing or wrong, a file outside the owned paths is needed, an acceptance item cannot be met, or the same step failed twice. Never change a contract.

**Report format.** `STATUS: done | blocked`, `BRANCH`, `FILES`, `BUILD`, `TESTS`, `DEVICE CHECKLIST` (items for the owner), `DEVIATIONS`, `QUESTIONS`.
