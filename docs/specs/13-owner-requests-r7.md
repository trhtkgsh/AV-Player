# Spec 13: Owner requests of 2026-11-15, 20:21 (after the Phase 5 APK)

Phase: 5, revision 2. The owner's list, sorted by module. Each item names its agent.
Contracts: `IFileOperations.MoveAsync` and `.CopyAsync`, `FileOperationResult.CompletedCount`, `UiSettings.ItemSize` (`ExplorerItemSize`), `UiSettings.ExplorerFields` default None, `PlaybackOverlaySettings.ProgressLineThickness`, `.ClockCorner`, `.BatteryCorner`, `PlaybackOverlayText.FormatCorner`.

## A. File operations (file-ops, explorer-ui)

1. **Move and copy.** A file can be moved or copied to another folder that already holds media (the explorer's folder list is the list of targets; making a new folder is not part of this request). Move changes the file's folder through the media store, with Android's consent dialog when it is needed (one dialog for the whole selection). Copy writes a new file owned by the app and needs no consent. A name that exists in the target folder: the system adds a number for a copy; a move of that file fails and the others go on. Android does not allow every kind of file in every top folder (video: DCIM, Movies, Pictures, Download, Documents; audio: Music and its relatives, Download, Documents): a refusal is reported as a failure with a clear text, never as a crash. The saved playback state follows a moved file. A copy is a new file with no state.
2. **Subtitles go with the file.** Delete also moves the subtitle files that belong to the file (`ISubtitleService.FindSidecarsAsync`) to the trash, in the same consent dialog. Move takes them along and copy copies them, best effort: a subtitle that cannot be handled does not fail the operation.
3. **Selection mode.** A long press on a file or a folder turns the page into selection mode with that item selected. A tap then selects or clears an item; the top bar shows the count and the actions Delete, Move, Copy, and Rename when exactly one file is selected; Back or clearing the last item leaves the mode. A selected folder stands for the media files in it (delete, move and copy; a folder cannot be renamed). The confirmation names the number of files. The ⋮ button stays, with Rename, Delete, Move and Copy for a file.
4. **Progress.** While an operation runs the page shows a busy state with a Cancel button for copy; the list is edited in place afterwards (or reloaded once after a multi-file operation).

## B. Explorer display (explorer-ui, ui-settings)

5. **Smaller items.** A setting "Item size" (Large, Small) on the Interface page, section "File list". Small: rows with a small thumbnail; in the grid three tiles in a portrait row (Large stays two), more in landscape by the same rule. The row height and the tile's text area follow the names' font scale so that names are not cut at any scale.
6. **Name only.** The details under a file name are off by default (`ExplorerFields = None`), and a phone that has the older default stored is switched to None once. The setting stays, so details can be turned on again. The NEW and Watched marks and the progress bar are not details and stay.

## C. Quiet display (ui-settings, player-hud-ui)

7. **Line thickness.** A setting for the height of the progress line, 1 to 12 units (default 2).
8. **A corner for each text.** The playback times, the clock and the battery level each have their own corner setting. Texts that share a corner are joined in one line as before. Defaults: times top right, clock and battery top left. Margin, size and color stay shared.

## D. Player (player-hud-ui)

9. **Confirm a restart.** A press on Previous that would start the file again from the beginning (position over 3 s) asks first: "Start again from the beginning?" Yes / No. Previous from a headset, the notification or the small window does not ask.
10. **Keep the screen on.** The screen stays on while a video is playing on the screen, in the player and in Picture-in-Picture. It may turn off when playback is paused, ended, or when only sound plays in the background.
11. **Double tap in the middle.** A double tap in the middle third of the picture's width pauses or resumes. The left and right thirds skip back and forward as before.
12. **Lower bottom bar.** The bottom part of the controls (seek row and transport buttons) is made lower.

## E. Leaving the player (global-defaults-store)

13. **One choice instead of two switches.** "When leaving the player while a video plays": Small window (Picture-in-Picture), Keep playing the sound, Pause. Stored in the two existing fields: window = both true; sound = `BackgroundAudio` true and `PictureInPictureOnLeave` false; pause = both false. The combination window without background sound is no longer offered; a phone that has it stored shows "Small window".

## F. Name and icon (app-skeleton)

14. **Name.** The app is called "AV Player" everywhere the user sees a name. The application id stays `com.arielplayer.app`, so the update installs over the app and keeps its data.
15. **Icon.** Green background, and a smaller play symbol. The splash screen uses the same symbol.

## Acceptance

Unit tests for the pure parts (`FormatCorner`, selection rules, grid columns by size, the one-time switch of the details, the store fields). Emulator: each item seen once. Device: `docs/qa/phase-5-r2-checklist.md`.
