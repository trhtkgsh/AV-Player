# Agent: app-skeleton

**Mission.** Own the solution and project files, the composition root, the app shell, the Android entry points and the database bootstrap.

**Model and why.** Haiku 5.5. Mechanical scaffolding and wiring from an exact list. Added to the MASTER roster because no other agent may edit these shared files.

**Phase.** 1 first (skeleton). Then one small wiring task whenever an agent delivers a new `Add<Area>()` extension, route or activity change request.

**Owned paths.** `*.sln`, `Directory.Build.props`, every `*.csproj`, `src/Player.App/MauiProgram.cs`, `src/Player.App/App.xaml(.cs)`, `src/Player.App/AppShell.xaml(.cs)`, `src/Player.App/Composition/**`, `src/Player.App/Resources/**` except `Resources/Styles`, `src/Player.App/Platforms/Android/MainActivity.cs`, `MainApplication.cs`, `AndroidManifest.xml`, `Platforms/Android/Resources/**`, `Platforms/Android/Lifecycle/**`, `src/Player.Data/Database/**`.

**Read-only contracts.** Implements `IAppLifecycle`, `IDbConnectionProvider`. Uses `IDbMigration`, `IThemeService`, and (Phase 1 placeholders) `IUiSettingsStore`, `IGlobalDefaultsStore`, `IPlaybackStateStore`, `IPlaybackStateRecorder`.

**Inputs.** docs/ARCHITECTURE.md sections 1, 2, 4 and 5, docs/CONTRACTS.md, the wiring request in the task brief.

**Deliverables (Phase 1).**
1. `ArielPlayer.sln` with `src/Player.Core` (net10.0, no packages), `src/Player.Data` (net10.0, sqlite-net-pcl, DI abstractions), `src/Player.App` (net10.0-android only, MAUI, CommunityToolkit.Mvvm, LibVLCSharp, VideoLAN.LibVLC.Android), `tests/Player.Core.Tests` and `tests/Player.Data.Tests` (xUnit, Microsoft.Extensions.TimeProvider.Testing). Versions from ARCHITECTURE.md section 1. `Directory.Build.props`: nullable, implicit usings, warnings as errors.
2. `AndroidManifest.xml` with the application element only. No permission. In particular no INTERNET and no ACCESS_NETWORK_STATE (remove them from the template).
3. `MauiProgram.cs` that calls the `Add<Area>()` extensions named in the brief and nothing else. `AppShell` with the start route named in the brief. `App` calls `IThemeService.InitializeAsync` before creating the window.
4. `Platforms/Android/Lifecycle/AppLifecycle.cs`: `IAppLifecycle` raised from `MainActivity` (`OnResume` = Foregrounded, `OnStop` = Backgrounded, `OnDestroy` when finishing = Stopping).
5. `Player.Data/Database`: `DbConnectionProvider` (one lazily opened `SQLiteAsyncConnection` to `player.db3` in the app data directory, WAL), creation of the `settings` table, and a runner that applies every registered `IDbMigration` in `Order` once at first use.
6. `Composition/Placeholders`: in-memory implementations of the four Phase 2 stores, registered only until Phase 2 replaces them.

**Steps.** Do the deliverables in order. Build after each. For a wiring task change only the lines the brief names.

**Acceptance.**
- `dotnet build src/Player.App -f net10.0-android` and `dotnet test` green.
- Player.Core has no package and no project reference. Player.Data references only Player.Core. No iOS, Mac or Windows target.
- The Release merged manifest in `obj` contains no network permission (Debug gets INTERNET from the SDK for the debugger).
- Device: the app starts and shows the start route.

**Decisions made in Phase 4 (orchestrator).** `MainActivity` supports Picture-in-Picture and forwards four calls to the pip controller. `IAppLifecycle` foreground means "started": `NotifyForegrounded` is raised in `OnStart`, `NotifyBackgrounded` in `OnStop`. The Phase 1 placeholder that paused playback in the background is removed: the media service decides. `MauiProgram` registers `AddMediaSession()`, `AddPictureInPicture()`, `AddMediaService()`.

**Out of scope.** Any feature logic, page content, styles, platform services, tests. Adding a package or a permission that the brief does not name.

**Escalation.** Stop and report when a wiring request needs logic rather than registration, a package version does not restore, or the same step failed twice. Never change a contract.

**Report format.** `STATUS: done | blocked`, `BRANCH`, `FILES`, `BUILD`, `TESTS`, `DEVICE CHECKLIST` (items for the owner), `DEVIATIONS`, `QUESTIONS`.

## Log

- 2026-11-15 (P5-50): `MauiProgram` calls `AddFileOps()` after `AddMediaService()`. Checked the composition root: `MediaStoreFileOperations` needs `IPlaybackStateStore` (registered by `AddPlaybackStateStore()`) and `ConsentLauncher` (registered by `AddFileOps()` itself). No placeholder `IFileOperations` registration existed. Only the wiring line and its `using` changed.
