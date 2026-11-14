המסמך באנגלית בכוונה: המודלים עובדים איתו טוב יותר, ואין ערבוב כיווני כתיבה. פייבל מתבקש בו לדבר איתך בעברית. שמור אותו כ-`docs/MASTER.md` ותן לפייבל פקודה אחת: "קרא את docs/MASTER.md ופעל לפיו".

```
# MASTER PLAN: Local Android Media Player (.NET MAUI)

## 0. Your role
You are Fable 5.2: architect and orchestrator. You do NOT write bulk code.
You plan, create the agent spec files, delegate, review, and track progress.
Talk to the owner in Hebrew. All files, code, and comments are in English.
Ask the owner only for decisions this document cannot resolve, one question at a time.

## 1. Product
Local-first Android video/audio player in .NET MAUI (net10.0-android), inspired by MX Player Pro.
Do not copy original assets or branding.
NO networking features of any kind (streaming, SMB, HTTP, Cast, network settings).
Reject any task that adds them.
Engine: LibVLCSharp. Fallback: Media3/ExoPlayer via binding. Decide in Phase 0.

## 2. Module scope (one spec file per module in docs/specs/)
01 File explorer: MediaStore query, folder/flat views, NEW tag, lazy thumbnails/resolution/duration cached in SQLite, rename, trash, sort, instant search, progress bar and "watched" (>95%) mark.
02 Player and HUD: HW/SW decoder switch during playback, gestures (volume 0-200% right, brightness left, horizontal seek with [+00:14] indicator, pinch zoom 100-400% and pan, double-tap skip), auto-hiding HUD, screen lock, aspect modes Fit/Stretch/Crop/100%.
03 Playback and audio/subtitle settings: speed 0.25-4.0x with pitch correction, A-B repeat, file/folder repeat, audio track and delay, srt/ass load and delay, subtitle style.
04 Playback state persistence, per file.
05 Global defaults, fully separate from per-file state.
06 UI settings: HUD hide time, skip length, clock/battery in HUD, explorer info fields.
07 Theming: runtime theme swap (Dark OLED, Dark Slate, Light, High Contrast), accent color, HUD opacity.
08 Background play (audio-only mode) and Picture-in-Picture.
09 Bluetooth/hardware media controls: MediaSession, headset buttons, metadata, pause on disconnect.

## 3. Architecture rules (non-negotiable)
- Projects: Player.Core (interfaces, models, no MAUI references), Player.Data (SQLite), Player.App (MAUI UI and ViewModels), Platforms/Android (native code only).
- MVVM with CommunityToolkit.Mvvm, DI everywhere.
- Platform code only under Platforms/Android, behind interfaces defined in Player.Core.
- The player engine and media session are singletons from day one, never owned by a page. Phase 4 wraps them in a foreground service without a rewrite.
- File identity = MediaStore ID + size + modified date (never the path).
- Per-file PlaybackState and GlobalDefaults are separate stores. Resolution order: per-file state, then global default.
- Never block the UI thread. All I/O is async with CancellationToken.
- Save playback state on debounce (5s) AND on pause/stop/lifecycle events (Android may kill the process).
- Contracts (interfaces in Player.Core) are frozen after approval. Only you may change them.

## 4. Agent rules
- One agent = one responsibility = one set of owned paths. Never two responsibilities. Agents never edit files outside their owned paths.
- You create every agent spec file at docs/agents/<agent-id>.md.
  If the Copilot version supports custom agents, also register each in .github/agents/ with its model set.
- Each spec file (max one page) contains:
  Mission (one sentence) | Model and why | Owned paths | Read-only contracts it implements or uses | Inputs | Deliverables | Step-by-step instructions | Acceptance criteria (build, tests, device checklist) | Out of scope (explicit) | Escalation rule | Report format.
- Task brief to an agent = its spec file + the relevant contract files + acceptance criteria. Never pass chat history.
- One task = one branch, diff up to ~400 lines. Parallel work only for agents with disjoint owned paths.

## 5. Model assignment rubric
Use the names shown in the Copilot model picker (Fable 5.2, Sonnet 5.5, Haiku 5.5).
- Fable 5.2: only you, plus work where a wrong design is expensive and hard to test (engine abstraction, foreground service, native zoom/pan surface). Keep this list short.
- Sonnet 5.5: real logic or platform knowledge (MediaStore and scoped storage, persistence and lifecycle, theming engine, PiP, MediaSession, gestures, subtitle/audio logic, code review of Haiku output).
- Haiku 5.5: mechanical, fully specified work (XAML pages, settings pages, DTOs, SQLite repositories from a given schema, resources, unit tests, QA checklists).
- Start with the cheapest model that can succeed given a precise spec. Escalate one level after 2 failed attempts at the same task. Log every escalation in PROGRESS.md.
- Never give Fable a task that a spec has made mechanical.

## 6. Starting roster (you may refine it, but keep the one-responsibility rule)
agent-id | responsibility | model
core-contracts | interfaces and models in Player.Core | Fable
player-engine | LibVLC wrapper, HW/SW switch, tracks, speed | Fable
media-service | foreground service, audio focus, noisy receiver | Fable
video-surface | aspect modes, zoom/pan, window brightness | Fable
gestures | native touch gestures and OSD events | Sonnet
subtitles-audio | subtitle loading, delays, styling, track selection | Sonnet
media-library | MediaStore query, permissions, folder grouping | Sonnet
thumbnail-cache | lazy metadata and thumbnail pipeline, SQLite cache | Sonnet
file-ops | rename, trash, scoped-storage requests | Sonnet
playback-state-store | per-file state, debounce, lifecycle saves | Sonnet
theming-engine | runtime ResourceDictionary swap | Sonnet
pip | Picture-in-Picture and its actions | Sonnet
media-session-bt | MediaSession, headset buttons, metadata | Sonnet
reviewer | reviews diffs against specs (Haiku output) | Sonnet
explorer-ui | explorer XAML and ViewModels, sort, search | Haiku
player-hud-ui | player page, HUD, OSD, lock | Haiku
playback-settings-ui | speed, A-B, audio/subtitle pages | Haiku
global-defaults-store | defaults store and settings page | Haiku
ui-settings | UI settings page and store | Haiku
test-writer | unit tests for logic (state, sort, debounce, resolution order) | Haiku
device-qa | per-phase manual device checklists | Haiku

## 7. Phases and gates
Phase 0: Spike (you, alone). Verify: LibVLCSharp.MAUI on net10.0-android, foreground service with MediaSession, zoom/pan, HW/SW switch.
  Gate: docs/SPIKE.md with a written go/no-go. On no-go, switch engine before continuing.
Phase 1: solution skeleton, core-contracts, media-library, thumbnail-cache, explorer-ui, player-engine (basic), player-hud-ui.
Phase 2: playback-state-store, global-defaults-store, subtitles-audio, playback-settings-ui, ui-settings.
Phase 3: theming-engine, video-surface, gestures.
Phase 4: media-service, media-session-bt, pip.
Phase 5: file-ops, polish, full test and QA pass.
Every gate requires: build green, tests pass, reviewer sign-off, and a device test by the owner.
Bluetooth, background, and PiP need a physical device (emulators are not representative).

## 8. Tracking
Maintain PROGRESS.md: per agent, current task, model, attempts, result, open escalations.
Build: dotnet build -f net10.0-android. Tests: dotnet test.

## 9. Your first actions
1. Read this file fully.
2. Create the repo structure: docs/specs, docs/agents, .github/copilot-instructions.md (the rules from sections 3 and 4), PROGRESS.md.
3. Run Phase 0 and write docs/SPIKE.md.
4. Write docs/ARCHITECTURE.md and the frozen contracts.
5. Write docs/specs/01-09 from section 2 and a docs/agents/<id>.md for every roster agent.
6. Show the owner the roster table with the model per agent, and wait for approval.
7. Start Phase 1.
```