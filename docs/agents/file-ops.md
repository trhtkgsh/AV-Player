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

## Decisions

- 2026-11-15 (P5-10): Player.App.FileOps namespace, folder Platforms/Android/FileOps. API 29+ tries the DISPLAY_NAME update first and asks consent only after a SecurityException (API 30+: createWriteRequest, API 29: the RecoverableSecurityException intent), then retries once. API 30+ trash is one createTrashRequest dialog. API 26-28: legacy WRITE_EXTERNAL_STORAGE (assembly attribute, max SDK 28, asked at run time); rename moves the file with File.RenameTo and then fixes _display_name and _data, because a row update alone does not rename the file there. Consent goes through the activity's ActivityResultRegistry (one-shot key, serialized, MainActivity unchanged); a destroyed activity or a cancelled token completes as Cancelled. Pure decisions are in FileOperationRules. Cached metadata is not moved on a changed MediaFileId (no contract for it); PruneAsync removes the old rows.
- 2026-11-15 (P5-11, review fixes): The explorer guarantees the extension is kept, so RenameAsync does not add it. Once the write is committed nothing turns the result into Cancelled or Failed: the read-back and the state move/delete run without the caller's token and only log failures; a missing row after rename reports Success with the original item and the new name. Resolver and Create*Request calls run on the thread pool. The duplicate-name check reads the row's own volume and elative_path and queries MediaStore.Files of that volume (any kind, case-insensitive; API <= 28 compares the folder listing ignoring case). A partial trash on API 26-29 deletes the saved state of the items already removed. **Known gap:** when the token is cancelled or the activity is destroyed (for example by a rotation) while the system dialog may still be up, the call returns Cancelled although the user can still approve; the late approval is then ignored and a trash may still happen without the state being cleaned (PruneAsync removes stale metadata, stale playback state stays until the id is reused).

- 2026-11-15 (R7-30, move, copy, subtitles): MediaStoreFileOperations is partial; Move/Copy live in MediaStoreFileOperations.MoveCopy.cs and ISubtitleService is injected. The target is resolved from one MediaStore.Files row of MediaFolder.BucketId (volume_name and relative_path; API <= 28: parent of _data); no row = Failed. Items whose BucketId equals the target are skipped and counted as done. Move API 29+: update relative_path per item first; a SecurityException queues the file, then one createWriteRequest (API 30+, all queued files and their subtitles) or one RecoverableSecurityException intent per file (API 29, also per subtitle) and one retry. Declined = Cancelled with the CompletedCount reached so far (files moved before the dialog stay moved, because the first pass writes what it may without asking). Another volume, a taken name, or IllegalArgument/IllegalState (kind not allowed in that top folder) leave the file in place and the loop goes on. Move API <= 28: File.RenameTo plus _data update. Copy API 29+: insert a pending row (video/audio collection of the target volume), copy with a 1 MiB buffer checking the token per block, clear is_pending; failure or cancel deletes the partial row; a free name (" (1)") is chosen first. API <= 28: FileStream copy and MediaScannerConnection.ScanFile. Subtitles (FindSidecarsAsync, MediaStore.Files URIs, each file once, never a selected file): trash adds them to the same request (retried without them when the request is refused) or deletes them after the item on API <= 29; move moves them with the item; copy inserts them into MediaStore.Files of the target, named after the copy's name; everything about a subtitle is best effort and only logged. Errors: "N of M files were not moved|copied. <file>: <cause>" with causes in FileOperationRules (target folder missing, other volume, name exists in target, Android does not allow this kind of file in that folder, access denied, could not be copied). **Known gaps:** a decline after a partial first pass is Cancelled although some files moved; on API 29 subtitles that need consent each ask their own dialog; Android may refuse a .srt in Movies (then it is skipped); the subtitle search runs before the dialog, so a slow search delays it; cached metadata is not moved (as in rename). Not device-tested.

- 2026-11-15 (R7-31, review fixes): API <= 28 copy deletes only a file this call created, and an unreadable target folder (List() null, null cursor) fails the item instead of assuming a free name. A subtitle outcome never changes the main result: subtitle-only consent is asked best effort (a decline, refusal or exception is ignored, also per subtitle on API 29), and a failed write-request fallback is no longer retried with an empty list. After RenameTo succeeded the move counts and a row-update failure is only logged; a ScanFile failure after a complete copy is only logged. A subtitle shared by several selected files is handled once, after the first of its files was moved, copied or trashed. CompletedCount is set in every trash result (items removed) and is 1 for a successful rename.
