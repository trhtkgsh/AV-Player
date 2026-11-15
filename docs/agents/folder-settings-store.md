# Agent: folder-settings-store

**Mission.** Store the per-folder settings: a default speed and the favorite mark (`IFolderSettingsStore`).

**Model and why.** Haiku 5.5. One small store that copies the pattern of the UI settings store. Its output always gets a reviewer pass.

**Phase.** 3 revision (added on 2026-11-15, owner request R2-1 in docs/specs/11-owner-requests-r3.md).

**Owned paths.** `src/Player.Data/FolderSettings/**`.

**Read-only contracts.** Implements `IFolderSettingsStore`. Uses `FolderSettings`, `IDbConnectionProvider`, the shared `SettingsRepository`.

**Inputs.** docs/specs/11-owner-requests-r3.md (R2-1, R1-4), docs/CONTRACTS.md, docs/ARCHITECTURE.md section 4.

**Deliverables.** `FolderSettingsStore`, its private DTO and source-generated JSON context, `AddFolderSettings()` DI extension.

**Steps.**
1. One row in the `settings` table, key `folder_settings`: a JSON object that maps the bucket id (as text) to `{ "Speed": 1.5, "Favorite": true }`. Source-generated JSON only. Private DTOs with nullable fields.
2. Load once, lazily, into memory behind a `SemaphoreSlim`; every read answers from memory. A broken row loads as empty; one bad entry never loses the others.
3. `SaveAsync`: clamp the speed to 0.25 .. 4.0 (NaN or infinity means no speed); an entry with no speed and no favorite mark is removed; write the whole map; raise `Changed` once, after the write, outside the lock. A save that changes nothing writes nothing and raises nothing.
4. `GetAllAsync` returns a snapshot (a copy), never the live dictionary.
5. All I/O async with `CancellationToken`. Nothing blocks the caller.

**Acceptance.**
- Build green. Data tests by test-writer against a real SQLite file: round trip, removal of empty entries, clamping, tolerant load, `Changed` once per effective save, concurrent saves keep every entry.
- No reflection-based JSON. Namespace `Player.Data.FolderSettingsStorage` (a namespace must never equal a type name).

**Out of scope.** The resolver (core-contracts). Applying the speed (playback-coordinator). The folder options menu and the favorites list (explorer-ui).

**Escalation.** Stop and report when a contract is missing or wrong, a file outside the owned path is needed, an acceptance item cannot be met, or the same step failed twice. Never change a contract.

**Report format.** `STATUS: done | blocked`, `BRANCH`, `FILES`, `BUILD`, `TESTS`, `DEVIATIONS`, `QUESTIONS`.
