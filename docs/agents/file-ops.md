# Agent: file-ops

**Mission.** Rename files and move them to the trash through MediaStore with the system consent flow.

**Model and why.** Sonnet 5.5. Scoped-storage write rules differ per API level and need platform knowledge.

**Phase.** 5.

**Owned paths.** `src/Player.App/Platforms/Android/FileOps/**`.

**Read-only contracts.** Implements `IFileOperations`. Uses `MediaItem`, `MediaFileId`, `FileOperationResult`, `IPlaybackStateStore`, `IMediaMetadataService`.

**Inputs.** docs/specs/01 requirements 12-14, docs/CONTRACTS.md.

**Deliverables.** `MediaStoreFileOperations`, an activity-result bridge for the consent dialog, `AddFileOps()` DI extension.

**Steps.**
1. Rename, API 30+: `MediaStore.createWriteRequest` for the item's URI, wait for the user's answer, then update `DISPLAY_NAME`. Keep the extension. Reject empty names and names with path separators before asking.
2. Rename, API 29: update and handle `RecoverableSecurityException` by launching its intent, then retry once.
3. Rename, API 26-28: update through `ContentResolver` with `WRITE_EXTERNAL_STORAGE` (declared with maxSdkVersion 28 as an assembly attribute).
4. Trash, API 30+: `MediaStore.createTrashRequest(uris, true)` for all items in one dialog.
5. Trash, API 26-29: delete through `ContentResolver` after the caller's in-app confirmation. Handle `RecoverableSecurityException` on 29.
6. After a rename re-query the row and return the updated `MediaItem`. If its `MediaFileId` changed, copy the playback state to the new id and delete the old one.
7. After a trash delete the playback state for each item. Metadata rows are pruned by the next `PruneAsync`.
8. Map outcomes: user declined = `Cancelled`, security exception with no recovery = `Denied`, anything else = `Failed` with the message.

**Acceptance.**
- Build green.
- Device: rename and trash on API 30+ show one system dialog and take effect. Declining returns `Cancelled` and changes nothing. The renamed file keeps its resume position. On API 29 or lower if a device is available.

**Out of scope.** Explorer UI and confirmation dialogs (explorer-ui). Copy, move, restore from trash, permanent delete on API 30+.

**Escalation.** Stop and report when a contract is missing or wrong, a file outside the owned path is needed, an acceptance item cannot be met, or the same step failed twice. Never change a contract.

**Report format.** `STATUS: done | blocked`, `BRANCH`, `FILES`, `BUILD`, `TESTS`, `DEVICE CHECKLIST` (items for the owner), `DEVIATIONS`, `QUESTIONS`.
