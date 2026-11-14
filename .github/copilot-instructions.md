# Copilot instructions

Read docs/MASTER.md at the start of every session and follow it.

## HARD GATE

No product code (including the skeleton) until every roster agent exists as
.github/agents/<id>.agent.md with its model set and the owner approved the roster.
Phase 0 code lives only in /spike and is never copied into the product wholesale.

The orchestrator writes code itself only for roster agents marked Fable. Everything else
is delegated to a sub-agent, or the orchestrator stops and asks the owner to switch agent manually.

## Architecture rules (non-negotiable) - MASTER.md section 3

- Projects: Player.Core (interfaces, models, no MAUI references), Player.Data (SQLite), Player.App (MAUI UI and ViewModels), Platforms/Android (native code only).
- MVVM with CommunityToolkit.Mvvm, DI everywhere.
- Platform code only under Platforms/Android, behind interfaces defined in Player.Core.
- The player engine and media session are singletons from day one, never owned by a page. Phase 4 wraps them in a foreground service without a rewrite.
- File identity = MediaStore ID + size + modified date (never the path).
- Per-file PlaybackState and GlobalDefaults are separate stores. Resolution order: per-file state, then global default.
- Never block the UI thread. All I/O is async with CancellationToken.
- Save playback state on debounce (5s) AND on pause/stop/lifecycle events (Android may kill the process).
- Contracts (interfaces in Player.Core) are frozen after approval. Only you may change them.

## Agent rules - MASTER.md section 4

- One agent = one responsibility = one set of owned paths. Never two responsibilities. Agents never edit files outside their owned paths.
- You create every agent spec file at docs/agents/<agent-id>.md.
  If the Copilot version supports custom agents, also register each in .github/agents/ with its model set.
- Each spec file (max one page) contains:
  Mission (one sentence) | Model and why | Owned paths | Read-only contracts it implements or uses | Inputs | Deliverables | Step-by-step instructions | Acceptance criteria (build, tests, device checklist) | Out of scope (explicit) | Escalation rule | Report format.
- Task brief to an agent = its spec file + the relevant contract files + acceptance criteria. Never pass chat history.
- One task = one branch, diff up to ~400 lines. Parallel work only for agents with disjoint owned paths.
