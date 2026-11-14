# Agent: thumbnail-cache

**Mission.** Produce and cache thumbnail, resolution and duration for media items, lazily.

**Model and why.** Sonnet 5.5. Needs a careful concurrency and cancellation design plus Android decoding APIs.

**Phase.** 1.

**Owned paths.** `src/Player.App/Platforms/Android/Thumbnails/**`, `src/Player.Data/Metadata/**`.

**Read-only contracts.** Implements `IMediaMetadataService`, one `IDbMigration`. Uses `IDbConnectionProvider`, `MediaItem`, `MediaMetadata`, `MediaFileId`.

**Inputs.** docs/specs/01 requirement 6, docs/ARCHITECTURE.md section 4 (table `media_metadata`), docs/CONTRACTS.md.

**Deliverables.** `MediaMetadataRepository` and its migration (Player.Data), `AndroidMetadataExtractor` and `MediaMetadataService` (Android), `AddThumbnailCache()` DI extension.

**Steps.**
1. Migration creates `media_metadata` exactly as in ARCHITECTURE.md section 4.
2. Repository: get many by key, upsert, delete missing. Async only.
3. Extractor: `ContentResolver.LoadThumbnail` (API 29+) or `MediaMetadataRetriever` for the frame, `MediaMetadataRetriever` for duration, width, height and rotation. Open by content URI. Save a JPEG (max 320 px wide, quality 80) named by the file key in the cache directory.
4. `GetOrCreateAsync`: return the cached row if present. Otherwise extract with a semaphore of 2, store, return. Concurrent calls for the same id share one extraction. Cancelling the token cancels the wait, not a running extraction another caller needs.
5. On extraction failure store a row with `failed = 1` so it is not retried on every scroll. Retry failed rows once per app start.
6. `GetCachedAsync`: one query for many ids. No decoding.
7. `PruneAsync`: delete rows and JPEG files whose id is not in the given set.

**Acceptance.**
- Build green. Repository covered by test-writer against a real SQLite file.
- Device: scrolling 500+ items stays smooth. Second launch decodes nothing (verify with a log counter). A corrupt file does not block the others.

**Out of scope.** List UI and image display (explorer-ui). MediaStore queries (media-library). Session artwork wiring.

**Escalation.** Stop and report when a contract is missing or wrong, a file outside the owned paths is needed, an acceptance item cannot be met, or the same step failed twice. Never change a contract.

**Report format.** `STATUS: done | blocked`, `BRANCH`, `FILES`, `BUILD`, `TESTS`, `DEVICE CHECKLIST` (items for the owner), `DEVIATIONS`, `QUESTIONS`.
