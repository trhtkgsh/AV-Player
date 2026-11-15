# ARCHITECTURE

Status: APPROVED by the owner on 2026-11-14, including the roster and the refinements in section 6.
Sources: docs/MASTER.md (rules), docs/SPIKE.md (measured constraints), docs/CONTRACTS.md (interfaces).

## 1. Solution layout

```
ArielPlayer.sln
src/Player.Core/      net10.0          interfaces, models, pure logic. No MAUI, no Android, no LibVLC, no SQLite.
src/Player.Data/      net10.0          SQLite stores (sqlite-net-pcl). References Player.Core only.
src/Player.App/       net10.0-android  MAUI UI, ViewModels, composition root. References Core and Data.
src/Player.App/Platforms/Android/      native code only, behind Player.Core interfaces.
tests/Player.Core.Tests/, tests/Player.Data.Tests/   net10.0, xUnit.
spike/                Phase 0 only. Never referenced by the solution.
```

Packages: Microsoft.Maui.Controls (workload version), CommunityToolkit.Mvvm 8.4.2, sqlite-net-pcl 1.11.285,
LibVLCSharp 3.10.2, VideoLAN.LibVLC.Android 3.6.5. Only Player.App references LibVLC.
App id: `com.arielplayer.app`. Min API 26, target API 36.

## 2. Ownership map (one path set per agent, all disjoint)

| Path | Owner |
|---|---|
| `*.sln`, `Directory.Build.props`, every `*.csproj`, `src/Player.App/{MauiProgram,App,AppShell}.*`, `src/Player.App/Composition/**`, `src/Player.App/Resources/**` except `Styles`, `src/Player.App/Platforms/Android/{MainActivity,MainApplication}.cs`, `AndroidManifest.xml`, `Platforms/Android/Resources/**`, `Platforms/Android/Lifecycle/**`, `src/Player.Data/Database/**` | app-skeleton |
| `src/Player.Core/Contracts/**`, `src/Player.Core/Models/**`, `src/Player.Core/Logic/Resolution/**` | core-contracts |
| `src/Player.App/Platforms/Android/Engine/**` | player-engine |
| `src/Player.App/Platforms/Android/Service/**` | media-service |
| `src/Player.App/Platforms/Android/Surface/**`, `src/Player.Core/Logic/Surface/**` | video-surface |
| `src/Player.App/Platforms/Android/Gestures/**`, `src/Player.Core/Logic/Gestures/**` | gestures |
| `src/Player.App/Platforms/Android/Subtitles/**`, `src/Player.Core/Logic/Subtitles/**` | subtitles-audio |
| `src/Player.App/Platforms/Android/Library/**`, `src/Player.Core/Logic/Library/**` | media-library |
| `src/Player.App/Platforms/Android/Thumbnails/**`, `src/Player.Data/Metadata/**` | thumbnail-cache |
| `src/Player.App/Platforms/Android/FileOps/**` | file-ops |
| `src/Player.Data/PlaybackState/**`, `src/Player.Core/Logic/PlaybackState/**` | playback-state-store |
| `src/Player.Core/Logic/Playback/**` | playback-coordinator |
| `src/Player.App/Theming/**`, `src/Player.App/Resources/Styles/**`, `src/Player.Data/Theme/**`, `src/Player.Core/Logic/Theming/**` | theming-engine |
| `src/Player.App/Platforms/Android/Pip/**` | pip |
| `src/Player.App/Platforms/Android/Session/**`, `src/Player.Core/Logic/Session/**` | media-session-bt |
| `src/Player.App/Features/Explorer/**`, `src/Player.Core/Logic/Explorer/**` | explorer-ui |
| `src/Player.App/Features/Player/**` | player-hud-ui |
| `src/Player.App/Features/PlaybackSettings/**` | playback-settings-ui |
| `src/Player.App/Features/GlobalDefaults/**`, `src/Player.Data/GlobalDefaults/**` | global-defaults-store |
| `src/Player.App/Features/UiSettings/**`, `src/Player.Data/UiSettings/**` | ui-settings |
| `tests/**` | test-writer |
| `docs/qa/**` | device-qa |
| none (read-only) | reviewer |
| `docs/**` except `docs/qa`, `.github/**`, `PROGRESS.md` | orchestrator |

Rules that keep the paths disjoint:

- **No shared composition file.** Each owner exposes one `Add<Area>(this IServiceCollection)` extension inside its own path. Only app-skeleton calls them from `MauiProgram.cs`, in a wiring task issued by the orchestrator.
- **No shared manifest edits.** Permissions are declared with `[assembly: UsesPermission(...)]` and services with `[Service]` attributes inside the owner's path. `AndroidManifest.xml` stays minimal.
- **No shared schema file.** Each store owner ships its own `IDbMigration` class in its path. The database bootstrap (app-skeleton) runs all registered migrations in `Order`.
- **Pure logic lives in `Player.Core/Logic/<Area>`** so that net10.0 tests can reach it. It may use only Player.Core types and the BCL.

## 3. Runtime rules

**Singletons.** `IPlayerEngine`, `IPlaybackCoordinator`, `IMediaSessionBridge`, `IPlaybackServiceController` and all stores are DI singletons created at app start. No page owns them. Pages and ViewModels are transient and only observe. In Phase 4 the foreground service resolves the same singletons from the app container. Nothing moves.

**Engine threading (SPIKE findings 1, 2).** The engine owns one serialized command queue on a dedicated thread. Every public method enqueues and returns a Task. LibVLC events are copied into plain data inside the callback and posted to the same queue. No code ever calls LibVLC from a LibVLC callback. Engine events are raised from the queue thread. ViewModels marshal to the UI thread themselves.

**Decoder switch (SPIKE 3, 4).** Stop, rebuild the media with `:codec=mediacodec_ndk,all` or `:codec=all`, start at the saved position. The snapshot reports both the requested and the actual decoder, parsed from LibVLC's log.

**Video surface (SPIKE 5, 6, 7, 8).** video-surface owns a native `SurfaceView` and knows nothing about LibVLC. player-engine owns LibVLC's `AWindow`. They meet in `IVideoOutputBinding`: the surface reports ready / size changed / lost, the engine attaches, reports the window size explicitly and restarts the video track only after the surface is ready. With no surface the engine disables the video track (audio-only) instead of retrying.

**File identity.** `MediaFileId = (MediaStoreId, SizeBytes, DateModifiedSeconds)`. Paths are display text only. Media is opened by content URI through a file descriptor (verified in SPIKE check 1).

**Settings resolution.** `PlaybackSettingsResolver.Resolve(PlaybackState?, GlobalDefaults)` returns `EffectivePlaybackSettings`. A null field in `PlaybackState` means "not set for this file". The two stores never write to each other.

**State saving.** `IPlaybackStateRecorder` saves on a 5 s debounce while the position changes, and immediately on pause, stop, end, item change, `IAppLifecycle.Backgrounded` and `Stopping`. A file is "watched" when position > 95% of duration.

**I/O.** Async with CancellationToken everywhere. Nothing blocks the UI thread. SQLite goes through `SQLiteAsyncConnection`.

**No network.** No INTERNET or network-state permission, no network API, no streaming URL input. The reviewer rejects any diff that adds one. The check is done on the Release merged manifest: the .NET Android SDK injects INTERNET into Debug builds for the debugger, and that cannot be removed without breaking debugging.

## 4. Data (single SQLite file `player.db3` in app data)

| Table | Owner | Columns |
|---|---|---|
| `playback_state` | playback-state-store | `file_key TEXT PK` (`"{id}:{size}:{modified}"`), `media_store_id INTEGER` (indexed), `position_ms`, `duration_ms`, `finished INTEGER`, `last_played_utc INTEGER`, `overrides_json TEXT` (nullable per-file fields) |
| `media_metadata` | thumbnail-cache | `file_key TEXT PK`, `media_store_id INTEGER` (indexed), `duration_ms`, `width`, `height`, `thumbnail_path TEXT`, `failed INTEGER`, `created_utc INTEGER` |
| `settings` | app-skeleton (table), each store owns its row | `key TEXT PK` (`global_defaults`, `ui_settings`, `theme`), `json TEXT`, `updated_utc INTEGER` |

Thumbnails are JPEG files in the app cache directory. Rows for files that no longer exist are pruned by their owner.

## 5. Build, test and publish

`dotnet build src/Player.App -f net10.0-android` and `dotnet test`. Every task must leave both green.

Installable APK (Release, trimmed and AOT compiled, signed with the debug key, sideload only):

`dotnet publish src/Player.App -f net10.0-android -c Release -p:AndroidPackageFormat=apk -o artifacts/release`

The result is `artifacts/release/com.arielplayer.app-Signed.apk` (about 70 MB, `arm64-v8a` for phones and `x86_64` for the emulator). `artifacts/` is ignored by git.

Do NOT pass `-r android-arm64` or `-p:RuntimeIdentifiers=...` on the command line:
- With `-r`, the VideoLAN.LibVLC.Android package does not add `libvlc.so` and `libc++_shared.so` (its targets look only at the plural `RuntimeIdentifiers`). The APK builds, installs, and every file fails with "Cannot play".
- A global `RuntimeIdentifiers` also reaches Player.Core and Player.Data and breaks their build.
A single-ABI APK needs a property set inside `Player.App.csproj` (app-skeleton task, not done yet).

Before handing over an APK, check it: it must contain `lib/arm64-v8a/libvlc.so`, and `dumpsys package com.arielplayer.app` must list no INTERNET or network permission.

## 6. Refinements to MASTER.md (approved by the owner on 2026-11-14)

1. **Two agents added to the roster.** `app-skeleton` (Haiku): solution files, composition root, manifest, database bootstrap. Without it no agent may touch those shared files. `playback-coordinator` (Sonnet): queue, next/previous, repeat modes, A-B repeat, applying resolved settings on open. Without it that logic has no owner.
2. **`Player.Core/Logic`.** MASTER says Core holds interfaces and models. Pure, platform-free logic is placed there too, in per-agent folders, because net10.0 tests cannot reference the Android app project.
3. **Volume above 100%** is done with LibVLC software gain inside the engine. Not verified in the spike. If it fails on a device, the fallback is an Android `LoudnessEnhancer`.
4. **Phase 1 slices for three agents that MASTER lists later.** The app cannot show a picture or a themed page in Phase 1 without them: video-surface (plain Fit surface with the lifecycle from SPIKE findings 5-7), theming-engine (token dictionary with one static theme), playback-coordinator (open, next, previous). Their full scope stays in the phase MASTER assigns.
5. **Phase 1 placeholders.** app-skeleton registers in-memory implementations of the Phase 2 stores so Phase 1 runs. They are replaced, not extended, in Phase 2.
