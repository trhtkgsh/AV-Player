# Spec 11: Owner requests of 2026-11-15 (after the Phase 3 build), sorted by phase

The owner tested `ArielPlayer-phase3.apk`, reported no faults so far, sent eleven requests and asked to sort them by the order of the phases. Each request is assigned to the phase whose module it changes, and the work is done in that order. This file is the requirement source for the task briefs. It amends specs 01, 02, 05 and 06 where they differ.

## Phase 1 scope: file explorer (spec 01). Agents: media-library, explorer-ui

- **R1-1 View modes.** The explorer has four sources, chosen from a "View" entry in the title bar: "Media folders" (every folder that holds media: today's folder list), "File browser" (the same folders as a tree: start at the storage root and go down one directory level at a time; only directories that hold media somewhere below are shown), "Favorites" (the folders the user marked), "All files" (today's flat list). Stored in `UiSettings.ViewMode` (`Folders`, `Tree`, `Favorites`, `Flat`).
- **R1-2 List or grid.** A second entry switches between rows ("List") and tiles with a large thumbnail and the name under it ("Grid"). It applies to folder lists and file lists. Stored in `UiSettings.Layout`.
- **R1-3 Audio-only folders.** A folder with no video file is shown only when `UiSettings.IncludeAudioOnlyFolders` is on (default on). `MediaFolder.AudioOnly` tells the explorer.
- **R1-4 Folder options.** A "more" button on every folder row or tile opens the folder options: "Add to favorites" or "Remove from favorites", and "Default speed for this folder". A favorite folder shows a star. File options (rename, delete) join the same pattern in Phase 5. (MAUI has no long-press recognizer for list rows without platform code, so the entry is a button.)

## Phase 2 scope: stores and settings (specs 05, 06). Agents: folder-settings-store (new), core-contracts, playback-coordinator, ui-settings

- **R2-1 Default speed per folder.** `FolderSettings(BucketId)` holds an optional speed and the favorite mark, in `IFolderSettingsStore`. Resolution order for the speed: the file's own value, then the folder's, then the global default. Nothing else is per folder.
- **R2-2 Font size of names.** `UiSettings.ExplorerFontScale` (80% to 160% in steps of 10%, default 100%) scales folder names and file names in the explorer. Set on the Interface page.
- **R2-3 Long-press speed.** `UiSettings.LongPressSpeed` (0.25x to 4.0x in steps of 0.25, default 2.0x). Set on the Interface page.
- **R2-4** The Interface page also gets the switch "Show folders with audio only" (R1-3).

## Phase 3 scope: player and gestures (spec 02). Agents: video-surface, player-engine, gestures, player-hud-ui

- **R3-1 System bars follow the controls.** The status bar and the navigation bar are visible exactly while the player controls are visible. The picture never moves or resizes when they appear: the player window is laid out edge to edge and the control bars keep clear of the system bars. In the player the system bars are see-through with white symbols, in every theme.
- **R3-2 Title on its own line.** The file name has the first line of the top bar to itself.
- **R3-3 Top bar, second line.** Clock and battery at the start; then the speed chip, the Aspect button, a menu button and the Lock button at the end. The speed chip moves here from the bottom bar.
- **R3-4 No Back button** in the player. The system Back closes it.
- **R3-5 Menu.** The menu button (next to Lock) opens a list: Subtitles, Audio track, Repeat, Decoder (shows the decoder in use; choosing it switches hardware and software). The four separate buttons and the decoder chip are removed. (Orchestrator's reading of "move the speed and the decoder up" together with "decoder in the menu": the speed is a chip in the top bar, the decoder is inside the menu of the top bar.)
- **R3-6 Bottom bar.** Seek bar with the two times, and Previous, Play or Pause, Next. Always left to right.
- **R3-7 Seek gesture shows the picture.** During a horizontal drag the picture shows the frame of the target position, updated while the finger moves, and playback does not run. Releasing continues from the target (playing if it was playing before). A cancelled drag returns to the start position.
- **R3-8 Long press plays faster.** Holding a finger still on the picture for half a second plays at `UiSettings.LongPressSpeed` until the finger lifts; then the speed from before returns. The file's saved speed is not changed. `GestureKind.LongPress`.

## Phase 5 scope: file operations (spec 01, file-ops)

- **R5-1** Rename and delete for files use the options pattern of R1-4. Nothing to build now.

## Phase 4

No request touches background playback, Picture-in-Picture or the media session.

## Acceptance (device)

Written by device-qa as its own list, `docs/qa/phase-3-r2-checklist.md` (the items that changed since the Phase 3 APK); the regression blocks of `docs/qa/phase-3-checklist.md` still apply.
