# Phase 5, revision 2 device checklist

- Phase: 5, revision 2 (owner requests of 2026-11-15, spec 13: file move and copy, subtitles with the file, selection mode, smaller items, name only, line thickness, one corner per text, restart question, screen on, double tap in the middle, lower bottom bar, one leave choice, name and icon)
- Date: 2026-11-15
- APK to install on a phone: `artifacts\AVPlayer-phase5-r2.apk` (copy it to the phone and open it, or `adb install -r artifacts\AVPlayer-phase5-r2.apk`; it installs over the app on the phone, keeps its data, and the app is now called "AV Player")
- SHA-256 of the APK: CABD3CCE0B1CE98FFF2F86317C3019896FF86B621A8736B0EEC36C5244A6DC4E
- Developer build to an emulator or a phone with USB debugging: `dotnet build src\Player.App -f net10.0-android -t:Install`
- Owner's phone: Samsung Galaxy A55, Android 17, One UI 9

## Phone under test

| Phone model | Android version | Result (PASS / FAIL, and the item IDs that failed) |
|---|---|---|
|  |  |  |
|  |  |  |

## How to read this list

- `[emulator ok]`: can be run on an emulator.
- `[physical device required]`: must be run on a phone. Picture-in-Picture, background play, screen-on timing and headset buttons are always physical.
- Every item has one expected result. If the result is not as written, write what you saw in the report.
- Items that say "as before" compare with the Phase 5 app you used before this update.
- "Explorer" means the file list: Folders, Browser, Favorites and All files pages.
- The earlier lists (`phase-5-checklist.md`, `phase-4-checklist.md`, `phase-4-r2-checklist.md`) are not repeated here. Run them as a regression after this list.
- Items marked "Hebrew" need the phone language set to Hebrew (Settings, Language, or the device language). Set it back to English after those items.
- Settings paths: Interface, section "File list" (Item size, Details shown for each file, Size of names); Interface, section "Player", group "While the controls are hidden" (Line thickness, Corner of the times, Corner of the clock, Corner of the battery level, margin, size, color); Settings, Playback defaults, "When leaving the player while a video plays".
- For the screen-on items, set the phone's screen timeout to 30 seconds (Settings, Display, Screen timeout). Set it back afterwards.

## Setup

Put these files on the phone (internal storage, for example `Movies/` and `Music/`).

1. A film or an episode of 20 minutes or more (used by most items).
2. Folder A: at least six videos, one folder inside it, and three spare copies of a short video (used by section 3 and section 4). The spare copies may be renamed, copied, moved and deleted. Do not use the film for these actions.
3. A spare video `spare.mp4` with a subtitle file `spare.srt` next to it (used by section 5). Make a second copy of this pair for each run, because the actions remove or move it.
4. Folder B: a folder that already holds at least one video (the target for copy and move). It must also hold a file with the same name as one of the spare copies (used by MOV-9 and MOV-10).
5. An audio file in `Music/` (used by MOV-13 and PLR-3).
6. A large file of at least 1 GB in folder A (used by MOV-11 and MOV-12).
7. A wired headset with a button, or Bluetooth headphones (used by PLR-9).
8. Two videos in the same folder, so that one has a previous file (used by PLR-8).

## 1. Install and name (INS, 7 items)

- [ ] **INS-1** `[physical device required]`
  - Action: Copy `AVPlayer-phase5-r2.apk` to the phone and open it (or run `adb install -r`). Do not uninstall the app first.
  - Expected: It installs as an update. No uninstall is needed.
  - Source: (spec 13, item 14; app-skeleton, R7-60: application id unchanged)
- [ ] **INS-2** `[physical device required]`
  - Action: Before installing, change a setting you can recognize (for example the theme). After installing, open the app and go to that setting.
  - Expected: The setting still has the value you chose. The data is kept.
  - Source: (spec 13, item 14, "keeps its data")
- [ ] **INS-3** `[emulator ok]`
  - Action: Look at the app's icon in the launcher, and its name under the icon.
  - Expected: The name under the icon is "AV Player".
  - Source: (spec 13, item 14; app-skeleton, R7-60)
- [ ] **INS-4** `[emulator ok]`
  - Action: Look at the app's icon in the launcher.
  - Expected: A green background with a play symbol. The play symbol is smaller than in the earlier icon.
  - Source: (spec 13, item 15; app-skeleton, R7-60)
- [ ] **INS-5** `[emulator ok]`
  - Action: Close the app completely (Recents, then swipe it away). Start it from the launcher and look at the first screen.
  - Expected: The splash screen shows the same green background and the same smaller play symbol as the icon.
  - Source: (spec 13, item 15; app-skeleton, R7-60)
- [ ] **INS-6** `[emulator ok]`
  - Action: Open the app and read the title at the top of the Folders page.
  - Expected: The title is "AV Player".
  - Source: (spec 13, item 14; app-skeleton, R7-60)
- [ ] **INS-7** `[emulator ok]`
  - Action: Delete one spare copy from folder A (see section 4) and read the text of Android's dialog. Then tap "Cancel" or "Deny".
  - Expected: The dialog text names the app "AV Player", not the old name.
  - Source: (spec 13, item 14, "everywhere the user sees a name"; explorer-ui, permission text en/he)

## 2. Explorer display (EXP, 9 items)

- [ ] **EXP-1** `[emulator ok]`
  - Action: After the update, open the Folders page and a folder with videos. Do the same in Browser and All files.
  - Expected: Each file row shows the name only. There is no details line under the name.
  - Source: (spec 13, item 6; ui-settings, R7-10: one-time switch to "no details")
- [ ] **EXP-2** `[emulator ok]`
  - Action: Settings, Interface, section "File list": turn on "Details shown for each file". Close the app completely, open it again and open a folder.
  - Expected: The details show under the file names again. The setting stayed on after the restart.
  - Source: (spec 13, item 6, "details can be turned on again"; ui-settings, R7-10)
- [ ] **EXP-3** `[emulator ok]`
  - Action: Turn "Details shown for each file" off again. Open a folder with a new file, a watched file and a partly played file.
  - Expected: The NEW mark and the Watched mark show. The partly played file shows its progress bar. No details line is shown.
  - Source: (spec 13, item 6; explorer-ui, R7-40)
- [ ] **EXP-4** `[emulator ok]`
  - Action: Settings, Interface, section "File list": set "Item size" to Small. Open a folder in the List layout.
  - Expected: The thumbnails are small, about half the width they have with Large.
  - Source: (spec 13, item 5; explorer-ui, R7-40: 48 x 27 against 96 x 54)
- [ ] **EXP-5** `[emulator ok]`
  - Action: With "Item size" Small, switch to the Grid layout on a portrait screen. Open a folder with at least six videos.
  - Expected: Three tiles in each row.
  - Source: (spec 13, item 5)
- [ ] **EXP-6** `[emulator ok]`
  - Action: With "Item size" Small and the Grid layout, turn the phone to landscape. Look at the same folder.
  - Expected: More than three tiles in each row, and no more than six.
  - Source: (spec 13, item 5, "more in landscape by the same rule"; explorer-ui, R7-40: six at most)
- [ ] **EXP-7** `[emulator ok]`
  - Action: Set "Item size" to Large. Use the Grid layout on a portrait screen, then the List layout.
  - Expected: Two tiles in each row in the Grid. The list looks as it did before the update.
  - Source: (spec 13, item 5, "Large stays two" and "Large unchanged")
- [ ] **EXP-8** `[emulator ok]`
  - Action: Set "Size of names" (Interface, section "File list") to its largest value. Set "Item size" to Small. Open a folder with long file names, in the List layout and in the Grid layout.
  - Expected: Each name is shown in full, on more lines if needed. No name is cut off at the edge of a row or tile.
  - Source: (spec 13, item 5, "names are not cut at any scale"; explorer-ui, R7-40)
- [ ] **EXP-9** `[emulator ok]`
  - Action: Same as EXP-8, with "Item size" set to Large.
  - Expected: Each name is shown in full. No name is cut off.
  - Source: (spec 13, item 5; explorer-ui, R7-40)

## 3. Selection mode (SEL, 14 items)

- [ ] **SEL-1** `[emulator ok]`
  - Action: In the List layout, long press a file row.
  - Expected: The page enters selection mode, the file row is selected (tinted) and the file does not open.
  - Source: (spec 13, item 3; explorer-ui, R7-42 and R7-44)
- [ ] **SEL-2** `[emulator ok]`
  - Action: In the Grid layout, long press a file tile.
  - Expected: The page enters selection mode, the tile is selected and the file does not open.
  - Source: (spec 13, item 3; explorer-ui, R7-42 and R7-44)
- [ ] **SEL-3** `[emulator ok]`
  - Action: On the Folders page in the List layout, long press a folder row.
  - Expected: The page enters selection mode, the folder row is selected and the folder does not open.
  - Source: (spec 13, item 3; explorer-ui, R7-41)
- [ ] **SEL-4** `[emulator ok]`
  - Action: On the Folders page in the Grid layout, long press a folder tile.
  - Expected: The page enters selection mode, the folder tile is selected and the folder does not open.
  - Source: (spec 13, item 3; explorer-ui, R7-41)
- [ ] **SEL-5** `[emulator ok]`
  - Action: In selection mode, tap another item.
  - Expected: That item becomes selected, the count in the top bar goes up by one, and nothing opens.
  - Source: (spec 13, item 3, "a tap then selects or clears an item"; explorer-ui, R7-41)
- [ ] **SEL-6** `[emulator ok]`
  - Action: In selection mode, tap an item that is selected.
  - Expected: It is cleared, and the count in the top bar goes down by one.
  - Source: (spec 13, item 3; explorer-ui, R7-41)
- [ ] **SEL-7** `[emulator ok]`
  - Action: Outside selection mode, tap a file row once.
  - Expected: The file opens in the player.
  - Source: (explorer-ui, R7-42: a tap runs Open outside the mode)
- [ ] **SEL-8** `[emulator ok]`
  - Action: Long press a file row, then drag the list up and down with a finger.
  - Expected: Scrolling does not select any further item, and no file opens.
  - Source: (explorer-ui, R7-42: scrolling cancels the tap and the long press)
- [ ] **SEL-9** `[emulator ok]`
  - Action: In selection mode, tap "Select all" in the top bar.
  - Expected: Every item of the list is selected, and the count shows the total.
  - Source: (spec 13, item 3; explorer-ui, R7-41)
- [ ] **SEL-10** `[emulator ok]`
  - Action: Clear the selection and select exactly one file.
  - Expected: "Rename" is in the top bar.
  - Source: (spec 13, item 3, "Rename when exactly one file is selected")
- [ ] **SEL-11** `[emulator ok]`
  - Action: Select two files, then select one file and one folder.
  - Expected: "Rename" is not in the top bar in either case. "Delete", "Move" and "Copy" are in the top bar.
  - Source: (spec 13, item 3; explorer-ui, R7-41: Rename only for exactly one file)
- [ ] **SEL-12** `[emulator ok]`
  - Action: In selection mode, press the Back button of the phone.
  - Expected: The mode ends. No item is selected and the normal top bar is back.
  - Source: (spec 13, item 3, "Back ... leaves the mode")
- [ ] **SEL-13** `[emulator ok]`
  - Action: In selection mode with one item selected, tap that item to clear it.
  - Expected: The mode ends by itself, and the normal top bar is back.
  - Source: (spec 13, item 3, "clearing the last item leaves the mode")
- [ ] **SEL-14** `[emulator ok]`
  - Action: In selection mode, look at the rows or tiles.
  - Expected: The ⋮ buttons are hidden. They come back after the mode ends.
  - Source: (spec 13, item 3; explorer-ui, R7-41)

## 4. Move and copy (MOV, 14 items)

- [ ] **MOV-1** `[emulator ok]`
  - Action: On a spare copy in folder A, tap ⋮, then "Copy". In the folder picker, choose folder B.
  - Expected: The copy appears in folder B. The original is still in folder A.
  - Source: (spec 13, item 1; explorer-ui, R7-41: the ⋮ menu has Copy)
- [ ] **MOV-2** `[emulator ok]`
  - Action: Copy a spare file to folder B as in MOV-1, and watch the screen during the copy.
  - Expected: No Android permission dialog appears. The copy runs without asking.
  - Source: (spec 13, item 1, "Copy ... needs no consent")
- [ ] **MOV-3** `[emulator ok]`
  - Action: Copy a file that shows the Watched mark, or that has a saved position, to folder B. Open the copy.
  - Expected: The copy shows no Watched mark and starts from the beginning.
  - Source: (spec 13, item 1, "A copy is a new file with no state")
- [ ] **MOV-4** `[emulator ok]`
  - Action: On a spare copy in folder A, tap ⋮, then "Move". Choose folder B. Tap "Allow" in Android's dialog.
  - Expected: Android asks once. After "Allow" the file is gone from folder A's list.
  - Source: (spec 13, item 1; file-ops, R7-30)
- [ ] **MOV-5** `[emulator ok]`
  - Action: Before the move, play a spare file for one minute and go back. Then move it to folder B (MOV-4). Open the moved file in folder B.
  - Expected: It resumes near the position where you stopped. Its Watched mark, if it had one, shows in folder B.
  - Source: (spec 13, item 1, "the saved playback state follows a moved file"; ProgressCalculator, Watched mark)
- [ ] **MOV-6** `[emulator ok]`
  - Action: Select three spare files in folder A. Tap "Move" and choose folder B. Tap "Allow" once.
  - Expected: Android asks once for all three. After "Allow" all three are gone from folder A's list.
  - Source: (spec 13, item 1, "one dialog for the whole selection"; file-ops, R7-30)
- [ ] **MOV-7** `[emulator ok]`
  - Action: Long press a folder that holds media files. Tap "Move" and choose folder B. Allow.
  - Expected: The media files of that folder are moved to folder B. The folder itself is not renamed.
  - Source: (spec 13, item 3, "a selected folder stands for the media files in it")
- [ ] **MOV-8** `[emulator ok]`
  - Action: Select three spare files in folder A. Tap "Move" and choose folder B. In Android's dialog, tap "Deny".
  - Expected: The files that moved before the dialog stay in folder B. The other files stay in folder A. The list reloads. Write down the text the app shows, or that it shows no text.
  - Source: (spec 13, item 1; file-ops, R7-30: a declined move is Cancelled with the count reached; explorer-ui, R7-41)
- [ ] **MOV-9** `[emulator ok]`
  - Action: Copy a spare file to folder B where a file with the same name already exists.
  - Expected: The copy appears in folder B with a number added to its name, as "<name> (1)". The existing file is not changed.
  - Source: (spec 13, item 1, "the system adds a number for a copy"; file-ops, R7-30)
- [ ] **MOV-10** `[emulator ok]`
  - Action: Select two files in folder A: one with a name that already exists in folder B, and one without. Move both to folder B and tap "Allow".
  - Expected: The file with the existing name stays in folder A. The app reports that this file was not moved, with its name. The other file moves to folder B.
  - Source: (spec 13, item 1, "a move of that file fails and the others go on"; file-ops, R7-30: "N of M files were not moved")
- [ ] **MOV-11** `[emulator ok]`
  - Action: Copy the large file (1 GB or more) from folder A to folder B.
  - Expected: While it copies, the list shows a busy state with the operation name and a "Cancel" button.
  - Source: (spec 13, item 4; explorer-ui, R7-41: busy state, Cancel for copy)
- [ ] **MOV-12** `[emulator ok]`
  - Action: Start the copy of MOV-11 and tap "Cancel" before it ends. Then look in folder B, in the app and in the phone's file manager.
  - Expected: The copy stops. No partial file of that name is left in folder B.
  - Source: (spec 13, item 4; file-ops, R7-30: a cancelled copy deletes its partial file)
- [ ] **MOV-13** `[emulator ok]`
  - Action: Move the audio file from `Music/` to a folder that holds videos (for example `Movies/`).
  - Expected: Either the file moves, or the app shows a clear message that it could not be moved and keeps running. A crash is a failure. Write the message text.
  - Source: (spec 13, item 1, "a refusal is reported as a failure with a clear text, never as a crash"; file-ops, R7-30)
- [ ] **MOV-14** `[emulator ok]`
  - Action: Select one file in folder A and tap "Move". Look at the folder list in the picker.
  - Expected: Folder A is not in the list. The other folders are.
  - Source: (explorer-ui, R7-41: the source folder is left out when all selected files share one folder)

## 5. Subtitles go with the file (SUB, 5 items)

- [ ] **SUB-1** `[emulator ok]`
  - Action: Long press `spare.mp4` (with `spare.srt` next to it), or tap ⋮ on it, and tap "Delete". Confirm.
  - Expected: Android's dialog lists both `spare.mp4` and `spare.srt`.
  - Source: (spec 13, item 2; file-ops, R7-30: the subtitle joins the same trash request)
- [ ] **SUB-2** `[emulator ok]`
  - Action: Continue SUB-1 and tap "Allow" in Android's dialog. Look at the phone's file manager, in the trash or in folder A.
  - Expected: Both `spare.mp4` and `spare.srt` are gone from folder A and both are in the trash.
  - Source: (spec 13, item 2, "in the same consent dialog")
- [ ] **SUB-3** `[emulator ok]`
  - Action: Use a new copy of the pair. Move `spare.mp4` to folder B and tap "Allow". Look in folder B in the phone's file manager.
  - Expected: Both `spare.mp4` and `spare.srt` are in folder B, and both are gone from folder A.
  - Source: (spec 13, item 2, "Move takes them along"; file-ops, R7-30)
- [ ] **SUB-4** `[emulator ok]`
  - Action: Open the moved `spare.mp4` in folder B. Do not choose a subtitle file.
  - Expected: Its subtitles show at once.
  - Source: (spec 13, item 2; the automatic load after a move is the owner's request, 2026-11-15)
- [ ] **SUB-5** `[emulator ok]`
  - Action: Use a new copy of the pair. Copy `spare.mp4` to folder B.
  - Expected: The copy appears in folder B, and a subtitle file for it appears next to it in folder B, named after the copy. Both originals stay in folder A.
  - Source: (spec 13, item 2, "copy copies them"; file-ops, R7-30: copy named after the copy's name)

## 6. Leaving the player (LEA, 5 items)

- [ ] **LEA-1** `[emulator ok]`
  - Action: Before the update, note the two old switches (the Picture-in-Picture switch and the background sound switch). After the update, open Settings, Playback defaults, and read the choice under "When leaving the player while a video plays".
  - Expected: The choice matches the old switches: Picture-in-Picture on gives "Small window (Picture-in-Picture)", with Picture-in-Picture off and background sound on gives "Keep playing the sound", and both off gives "Pause". If both were on in your old settings, the choice reads "Small window (Picture-in-Picture)".
  - Source: (spec 13, item 13; global-defaults-store, 2026-11-15 decision)
- [ ] **LEA-2** `[physical device required]`
  - Action: Choose "Small window (Picture-in-Picture)". Play a video and press Home.
  - Expected: The video shows in a small window.
  - Source: (spec 13, item 13)
- [ ] **LEA-3** `[physical device required]`
  - Action: Choose "Keep playing the sound". Play a video and press Home.
  - Expected: No small window appears, and the sound keeps playing.
  - Source: (spec 13, item 13; global-defaults-store, 2026-11-15 decision)
- [ ] **LEA-4** `[physical device required]`
  - Action: With "Keep playing the sound" chosen, play a video, press Home, and then turn the screen off.
  - Expected: The sound keeps playing while the screen is off.
  - Source: (global-defaults-store, 2026-11-15: the sound choice also applies when the screen is turned off)
- [ ] **LEA-5** `[physical device required]`
  - Action: Choose "Pause". Play a video and press Home.
  - Expected: The video pauses. No small window appears and no sound plays.
  - Source: (spec 13, item 13)

## 7. Player (PLR, 17 items)

- [ ] **PLR-1** `[physical device required]`
  - Action: With the screen timeout at 30 seconds, play the film in the player and do not touch the screen for two minutes.
  - Expected: The screen stays on for the whole two minutes.
  - Source: (spec 13, item 10; player-hud-ui, R7-20 and R7-22)
- [ ] **PLR-2** `[physical device required]`
  - Action: Play a video, press the Picture-in-Picture button, and do not touch the small window for two minutes.
  - Expected: The screen stays on for the whole two minutes.
  - Source: (spec 13, item 10, "in the player and in Picture-in-Picture")
- [ ] **PLR-3** `[physical device required]`
  - Action: Play the audio file from `Music/` for two minutes with the screen timeout at 30 seconds. Do not touch the screen.
  - Expected: The screen turns off at its timeout as usual. It is not kept on for audio.
  - Source: (spec 13, item 10; player-hud-ui, R7-22: only when the picture is shown)
- [ ] **PLR-4** `[physical device required]`
  - Action: Play a video, open the settings sheet over the picture, and keep it open for two minutes without touching the screen.
  - Expected: The screen stays on while the sheet is open.
  - Source: (player-hud-ui, R7-22: a settings sheet keeps the screen flag)
- [ ] **PLR-5** `[emulator ok]`
  - Action: Play a video and wait until it is more than 3 seconds in. Press Previous.
  - Expected: A question appears: "Start again from the beginning?" with "Yes" and "No". Playback is not restarted yet.
  - Source: (spec 13, item 9; player-hud-ui, R7-20)
- [ ] **PLR-6** `[emulator ok]`
  - Action: In the question of PLR-5, tap "No".
  - Expected: The question closes and the video keeps playing from the same place.
  - Source: (spec 13, item 9; player-hud-ui, R7-20: No keeps playing)
- [ ] **PLR-7** `[emulator ok]`
  - Action: Press Previous again and tap "Yes".
  - Expected: The video starts again from 0:00.
  - Source: (spec 13, item 9)
- [ ] **PLR-8** `[emulator ok]`
  - Action: Play the second of two videos in one folder and press Previous within the first 3 seconds.
  - Expected: The previous file starts at once. No question appears.
  - Source: (spec 13, item 9, "Previous within 3 seconds"; player-hud-ui, R7-20: 3000 ms)
- [ ] **PLR-9** `[physical device required]`
  - Action: Play a video for one minute. Press Previous on the wired or Bluetooth headset.
  - Expected: No question appears.
  - Source: (spec 13, item 9, "Previous from a headset ... does not ask")
- [ ] **PLR-10** `[physical device required]`
  - Action: Enter the small window (Picture-in-Picture) from a video played for one minute. Press Previous in the small window.
  - Expected: No question appears.
  - Source: (spec 13, item 9, "... or the small window does not ask")
- [ ] **PLR-11** `[emulator ok]`
  - Action: Play a video for one minute. Pull down the notification shade and press Previous in the playback notification.
  - Expected: No question appears.
  - Source: (spec 13, item 9, "... or the notification does not ask")
- [ ] **PLR-12** `[emulator ok]`
  - Action: Play a video. Double tap the middle third of the picture. Then double tap the middle third again.
  - Expected: The first double tap pauses. The second resumes.
  - Source: (spec 13, item 11; player-hud-ui, R7-20)
- [ ] **PLR-13** `[emulator ok]`
  - Action: Play a video. Double tap the left third of the picture, then double tap the right third.
  - Expected: The left double tap skips back and the right double tap skips forward, as before.
  - Source: (spec 13, item 11, "the left and right thirds skip back and forward as before")
- [ ] **PLR-14** `[emulator ok]` (Hebrew)
  - Action: With the phone in Hebrew, play a video. Double tap the middle third, then the left third, then the right third.
  - Expected: The middle double tap pauses or resumes. The left and right double taps skip in the same direction as before the update.
  - Source: (spec 13, item 11, "also in Hebrew" (owner's request); player-hud-ui, R7-20: direction rules including Hebrew unchanged)
- [ ] **PLR-15** `[emulator ok]`
  - Action: Look at the bottom bar of the player in portrait, and compare it with Phase 5.
  - Expected: The bar sits lower on the screen. The seek row and the transport buttons are closer together.
  - Source: (spec 13, item 12; player-hud-ui, R7-20 and R7-21)
- [ ] **PLR-16** `[emulator ok]`
  - Action: Drag the seek handle slowly from the left end to the right end of the bar, then let go.
  - Expected: The handle follows the finger and stays where you let go. The picture moves to that time.
  - Source: (spec 13, item 12, "the seek bar is still easy to drag"; player-hud-ui, R7-22)
- [ ] **PLR-17** `[emulator ok]`
  - Action: Tap Previous, then Play or Pause, then Next, one tap each.
  - Expected: Each button responds to the first tap.
  - Source: (spec 13, item 12, "transport buttons easy to hit"; player-hud-ui, R7-20: 44-unit touch targets)

## 8. Quiet display (OVL, 7 items)

- [ ] **OVL-1** `[emulator ok]`
  - Action: Settings, Interface, section "Player", group "While the controls are hidden". Move the "Line thickness" slider to its left end, then to its right end.
  - Expected: The slider runs from 1 (thinnest) to 12 (thickest).
  - Source: (spec 13, item 7; ui-settings, R7-10)
- [ ] **OVL-2** `[emulator ok]`
  - Action: Set "Line thickness" to 12 and release the slider. Open a video and let the controls hide.
  - Expected: The thin line at the bottom edge is clearly thicker than at the default 2, in the progress color.
  - Source: (spec 13, item 7; player-hud-ui, R7-20: the line height follows the setting)
- [ ] **OVL-3** `[emulator ok]`
  - Action: Set the corners to their defaults: "Corner of the times" top right, "Corner of the clock" top left, "Corner of the battery level" top left. Open a video and let the controls hide.
  - Expected: The elapsed and total times are in the top right. The clock and the battery percent are together in one line in the top left.
  - Source: (spec 13, item 8, defaults; ui-settings, R7-10; player-hud-ui, R7-20)
- [ ] **OVL-4** `[emulator ok]`
  - Action: Set "Corner of the times" to bottom left. Let the controls hide.
  - Expected: The times are alone in the bottom left. The clock and the battery stay in the top left.
  - Source: (spec 13, item 8, "a corner for each text")
- [ ] **OVL-5** `[emulator ok]`
  - Action: Set "Corner of the times" to top right, and "Corner of the clock" to top right too. Let the controls hide.
  - Expected: The times and the clock are joined in one line in the top right, in that order. The battery stays in the top left.
  - Source: (spec 13, item 8, "texts that share a corner are joined in one line"; player-hud-ui, R7-20: order times, clock, battery)
- [ ] **OVL-6** `[emulator ok]`
  - Action: Set "Corner of the battery level" to bottom right. Let the controls hide.
  - Expected: The battery percent is in the bottom right, by itself. The other texts stay where they were.
  - Source: (spec 13, item 8)
- [ ] **OVL-7** `[emulator ok]`
  - Action: In Interface, section "Player", group "While the controls are hidden", change the margin, the text size and the color. Let the controls hide with the times, the clock and the battery in different corners.
  - Expected: The margin, the size and the color change in every corner that has text.
  - Source: (spec 13, item 8, "margin, size and color stay shared"; ui-settings, R7-10)

## Items per section

| Section | Items | Physical device required | Emulator ok |
|---|---|---|---|
| 1 Install and name (INS) | 7 | 2 | 5 |
| 2 Explorer display (EXP) | 9 | 0 | 9 |
| 3 Selection mode (SEL) | 14 | 0 | 14 |
| 4 Move and copy (MOV) | 14 | 0 | 14 |
| 5 Subtitles go with the file (SUB) | 5 | 0 | 5 |
| 6 Leaving the player (LEA) | 5 | 4 | 1 |
| 7 Player (PLR) | 17 | 6 | 11 |
| 8 Quiet display (OVL) | 7 | 0 | 7 |
| Total | 78 | 12 | 66 |

Notes for the owner:

- Long press, tap and scroll in the explorer (section 3) were written from the agent reports and are not yet checked on a device. Write what you see.
- Folder rows in the list have no symbol, so "Item size" Small does not change them.
- Subtitle moves are best effort: a subtitle that Android refuses is skipped, and the video still moves.
- Known gap (from file-ops, 2026-11-15): if the screen is rotated while Android's dialog is open, the app may say "cancelled" although the file was changed. Write it down if you see it.

## Report back

Write the results in the table at the top of this file and send:

- Phone model and Android version: ____
- Number of failed items, per section (1 Install and name, 2 Explorer display, 3 Selection mode, 4 Move and copy, 5 Subtitles, 6 Leaving the player, 7 Player, 8 Quiet display): ____
- Failed item IDs: ____
- What was seen instead (for each failed item): ____
- MOV-8: what the app showed after "Deny" (the text, or no text), and which files moved: ____
- MOV-13: the message text for the audio file, or that it moved: ____
- LEA-1: which choice the phone showed, and which old switches it matched: ____
- PLR-14: which direction the left and right thirds skipped in Hebrew, and whether it matched the update before: ____
- Items not run, and why: ____
