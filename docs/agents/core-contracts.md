# Agent: core-contracts

**Mission.** Turn docs/CONTRACTS.md into the interfaces and models of Player.Core, exactly.

**Model and why.** Fable 5.2. Every other agent builds on these types. A wrong contract is expensive and is found late.

**Phase.** 1, directly after app-skeleton has created the empty projects.

**Owned paths.** `src/Player.Core/Contracts/**`, `src/Player.Core/Models/**`, `src/Player.Core/Logic/Resolution/**`.

**Read-only contracts.** Defines all of them. Source of truth: docs/CONTRACTS.md.

**Inputs.** docs/CONTRACTS.md, docs/ARCHITECTURE.md sections 1 and 3.

**Deliverables.** One file per type under `Models` and `Contracts`. `PlaybackSettingsResolver` under `Logic/Resolution`. XML doc comments on every public member.

**Steps.**
1. Create each model and enum from CONTRACTS.md with the same names, members, defaults and nullability.
2. Create each interface with the same signatures.
3. Implement `PlaybackSettingsResolver.Resolve` as specified: per-file value if not null, otherwise the global default. Start position is 0 when the file is finished or resume is disabled.
4. Add XML docs stating units, ranges and threading for each member.
5. Build Player.Core.

**Acceptance.**
- `dotnet build src/Player.Core` succeeds with zero warnings.
- Player.Core has no package reference and no reference to MAUI, Android, LibVLC or SQLite.
- The public API matches docs/CONTRACTS.md member for member.
- Device checklist: none.

**Out of scope.** Any implementation of an interface. Tests (test-writer). Project files (app-skeleton). Changing docs/CONTRACTS.md.

**Escalation.** Stop and report instead of guessing when CONTRACTS.md is ambiguous or cannot compile as written, or when the same step failed twice. Only the orchestrator changes a contract.

**Report format.** `STATUS: done | blocked`, `BRANCH`, `FILES` (list), `BUILD` (result), `TESTS` (result or n/a), `DEVICE CHECKLIST` (items for the owner), `DEVIATIONS` (from this spec), `QUESTIONS`.
