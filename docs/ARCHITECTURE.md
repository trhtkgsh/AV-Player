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
| `src/Player.App/Localization/**`, `src/Player.Core/Logic/Localization/**` | localization (added in Phase 3) |
| `src/Player.Data/FolderSettings/**` | folder-settings-store (added in the Phase 3 revision) |
| `tests/**` | test-writer |
| `docs/qa/**` | device-qa |
| none (read-only) | reviewer |
| `docs/**` except `docs/qa`, `.github/**`, `PROGRESS.md` | orchestrator |

Rules that keep the paths disjoint:

- **No shared composition file.** Each owner exposes one `Add<Area>(this IServiceCollection)` extension inside its own path. Only app-skeleton calls them from `MauiProgram.cs`, in a wiring task issued by the orchestrator.
- **No shared manifest edits.** Permissions are declared with `[assembly: UsesPermission(...)]` and services with `[Service]` attributes inside the owner's path. `AndroidManifest.xml` stays minimal.
- **No shared schema file.** Each store owner ships its own `IDbMigration` class in its path. The database bootstrap (app-skeleton) runs all registered migrations in `Order`.
- **Pure logic lives in `Player.Core/Logic/<Area>`** so that net10.0 tests can reach it. It may use only Player.Core types and the BCL.
- **No shared text file.** Each UI module has one `IStringSource` class in its own path with its texts in English and Hebrew, registered in its own `Add<Area>()`. The localization module merges them (docs/specs/10-localization.md).

## 3. Runtime rules

**Singletons.** `IPlayerEngine`, `IPlaybackCoordinator`, `IMediaSessionBridge`, `IPlaybackServiceController` and all stores are DI singletons created at app start. No page owns them. Pages and ViewModels are transient and only observe. In Phase 4 the foreground service resolves the same singletons from the app container. Nothing moves.

**Engine threading (SPIKE findings 1, 2).** The engine owns one serialized command queue on a dedicated thread. Every public method enqueues and returns a Task. LibVLC events are copied into plain data inside the callback and posted to the same queue. No code ever calls LibVLC from a LibVLC callback. Engine events are raised from the queue thread. ViewModels marshal to the UI thread themselves.

**Decoder switch (SPIKE 3, 4).** Stop, rebuild the media with `:codec=mediacodec_ndk,all` or `:codec=all`, start at the saved position. The snapshot reports both the requested and the actual decoder, parsed from LibVLC's log.

**Video surface (SPIKE 5, 6, 7, 8).** video-surface owns a native `SurfaceView` and knows nothing about LibVLC. player-engine owns LibVLC's `AWindow`. They meet in `IVideoOutputBinding`: the surface reports ready / size changed / lost, the engine attaches, reports the window size explicitly and restarts the video track only after the surface is ready. With no surface the engine disables the video track (audio-only) instead of retrying.

**Shape of the picture (Phase 3).** The surface view alone decides the shape: `SurfaceGeometry` computes the rectangle of the native surface from the video format, the container, the aspect mode and the zoom, and the surface is really that large (a zoomed picture is rendered at its size, not upscaled). The engine tells LibVLC to fill the surface exactly (its aspect ratio is set to the surface size), so LibVLC never adds bars of its own; while the format of a new file is unknown it keeps the video's own proportions. The surface keeps its last shape when the engine stops. The aspect mode of a file is applied by the coordinator when it opens the item; the HUD only changes it on request.

**Player window (Phase 3).** `IVideoSurface.SetImmersive` and `SetWindowBrightness` are the only places that touch the activity window for the player. The HUD turns immersive mode on when the player page is active and off, with the brightness reset, when the page closes.

**Gestures (Phase 3).** A native overlay view feeds a pure classifier; `IGestureSource` raises semantic events on the UI thread and has no side effects. The HUD applies them.

**Language and direction (Phase 3).** `ILocalizer` follows `UiSettings.Language` and the language of the device. Texts in XAML are bindings created by `{loc:T Key}`, so they change at once. The flow direction is set explicitly on the window and its pages (never `MatchParent`), so the app's language decides the direction and not the device. The bottom bar of the player is always left to right.

**Start page.** The window starts with an empty black page; the shell is created after the saved theme and language are applied (at most five seconds later), so the first real page never shows the wrong theme or language.

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

The result is `artifacts/release/com.arielplayer.app-Signed.apk` (about 105 MB). `artifacts/` is ignored by git.

The ABIs of the Release APK are set in `Player.App.csproj` (`RuntimeIdentifiers`, Release only): `arm64-v8a` for 64-bit phones, `armeabi-v7a` for phones that run a 32-bit Android, `x86_64` for the emulator. One file for all three, so that the file tested on the emulator is the file the owner installs. To change the list, change the csproj (app-skeleton task).

Do NOT pass `-r android-arm64` or `-p:RuntimeIdentifiers=...` on the command line:
- With `-r`, the VideoLAN.LibVLC.Android package does not add `libvlc.so` and `libc++_shared.so` (its targets look only at the plural `RuntimeIdentifiers`). The APK builds, installs, and every file fails with "Cannot play".
- A global `RuntimeIdentifiers` also reaches Player.Core and Player.Data and breaks their build.

Before handing over an APK, check it:
- It contains `lib/<abi>/libvlc.so` for each of the three ABIs.
- `dumpsys package com.arielplayer.app` lists no INTERNET or network permission.
- It runs on the emulator as x86_64 (`adb install -r <apk>`) and as arm64 (`adb install -r --abi arm64-v8a <apk>`; the API 34 and API 36 emulator images translate arm64). The 32-bit ARM part cannot run on these emulators.

System bars: before `ThemeService` runs, and for the whole of Phase 1, the status bar color comes from `Platforms/Android/Resources/values/colors.xml`. Up to API 34 the status bar uses `colorPrimaryDark`. From API 35 the window is edge to edge and the area behind the status bar is the background of the Shell `AppBarLayout`, which uses `colorPrimary`; `Window.SetStatusBarColor` does nothing there. Both colors are set to the Dark OLED `ColorSurface`. Following a theme change at runtime is a Phase 3 task of theming-engine (spec 07, requirement 7). Phase 3 result: `ThemeService.ApplySystemBars()` sets the bar colors up to API 34 and the icon brightness on every level; from API 35 the Shell colors (shared styles, `DynamicResource`) color the area behind the status bar. `MainActivity` calls it in `OnCreate` and `OnResume`, because the theme can be applied before an activity exists.

## 6. Refinements to MASTER.md (approved by the owner on 2026-11-14)

1. **Two agents added to the roster.** `app-skeleton` (Haiku): solution files, composition root, manifest, database bootstrap. Without it no agent may touch those shared files. `playback-coordinator` (Sonnet): queue, next/previous, repeat modes, A-B repeat, applying resolved settings on open. Without it that logic has no owner.
2. **`Player.Core/Logic`.** MASTER says Core holds interfaces and models. Pure, platform-free logic is placed there too, in per-agent folders, because net10.0 tests cannot reference the Android app project.
3. **Volume above 100%** is done with LibVLC software gain inside the engine. Not verified in the spike. If it fails on a device, the fallback is an Android `LoudnessEnhancer`.
4. **Phase 1 slices for three agents that MASTER lists later.** The app cannot show a picture or a themed page in Phase 1 without them: video-surface (plain Fit surface with the lifecycle from SPIKE findings 5-7), theming-engine (token dictionary with one static theme), playback-coordinator (open, next, previous). Their full scope stays in the phase MASTER assigns.
5. **Phase 1 placeholders.** app-skeleton registers in-memory implementations of the Phase 2 stores so Phase 1 runs. They are replaced, not extended, in Phase 2.
