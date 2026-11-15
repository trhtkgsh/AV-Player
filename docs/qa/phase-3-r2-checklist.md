# Phase 3 revision 2 device checklist

- Phase: 3, revision 2 (the owner's requests of 2026-11-15: explorer views, favorites, folder speed, name size, new player layout, system bars with the controls, seek preview, long press)
- Date: 2026-11-15
- APK to install on a phone: `artifacts\ArielPlayer-phase3-r2.apk` (copy it to the phone and open it, or `adb install -r artifacts\ArielPlayer-phase3-r2.apk`; it installs over the Phase 3 app)
- SHA-256 of the APK: 86574F14AC9E629558D9DC3CCE88CA9FBCA35D42BCBE5AA5C1FDF433F41707E7
- Developer build to an emulator or a phone with USB debugging: `dotnet build src\Player.App -f net10.0-android -t:Install`

## Phone under test

| Phone model | Android version | Result (PASS / FAIL, and the item IDs that failed) |
|---|---|---|
|  |  |  |
|  |  |  |

## How to read this list

- `[emulator ok]`: can be run on an emulator.
- `[physical device required]`: must be run on a phone.
- `[emulator ok, verified 2026-11-15]`: the orchestrator already checked the item on an emulator (Pixel 5 AVD, API 34 and Pixel 7 AVD, API 36, x86_64, Release APK). It still needs its normal run on the phone. For the physical items with this marker, the behaviour was seen on the emulator, but sound, brightness, sharpness and a real Hebrew phone need the device.
- This list covers only what changed since `ArielPlayer-phase3.apk`. The regression blocks of `phase-3-checklist.md` still apply. Run them if time allows.
- Two items of `phase-3-checklist.md` changed. The player has no Back button, and no separate Subtitles, Audio and Repeat buttons any more. They are in the menu. The speed chip is in the top bar.
- Player top bar, first line: the file name. Second line: clock and battery, then the speed chip, the Aspect button, the menu button (three dots) and the Lock button.
- Player bottom bar: the seek bar with the two times, and Previous, Play or Pause, Next.
- The status bar and the navigation bar of Android appear and disappear together with the player controls.
- The system Back button closes the player.
- Folder options open from the three-dot button on a folder's row or tile.
- The folder options show the folder's name as the title. The speed list offers 0.5x to 3.0x and "Use the general default".

## Setup

Put these files on the phone (internal storage, for example `Movies/`).

1. At least three folders with videos, one of them with two or more files (the Phase 2 folder with `ep2.mp4` and `ep10.mp4` is fine) (used by EXP-9 to EXP-23, SET-1 to SET-5, PLR-19).
2. A folder that holds only audio files (for example `Music/Tones`) (used by EXP-22).
3. A folder two levels deep (for example `Movies/Series`), so the file browser has something to go into. Put one video directly in it and one sub-folder with a video in it, so that one directory holds both files and sub-directories (used by EXP-10 to EXP-13).
4. A video of at least 2 minutes with clear motion (used by PLR-1 to PLR-18).

## Explorer (spec 11, Phase 1 scope)

- [ ] **EXP-9** `[emulator ok]` `[emulator ok, verified 2026-11-15]`
  - Action: In the folder list, press "View".
  - Expected: A list with "Media folders", "File browser", "Favorites" and "All files" opens. The current one has a check mark.
  - Source: (spec 11, R1-1)
- [ ] **EXP-10** `[emulator ok]` `[emulator ok, verified 2026-11-15]`
  - Action: Choose "File browser".
  - Expected: The list shows the top directories of the storage that hold media (for example "Movies", "Music"), each with its totals.
  - Source: (spec 11, R1-1)
- [ ] **EXP-11** `[emulator ok]` `[emulator ok, verified 2026-11-15]`
  - Action: Tap a directory, then press the system Back button.
  - Expected: The list goes one level down and the title is the directory's name. Back goes one level up. At the top level Back leaves the app as before.
  - Source: (spec 11, R1-1)
- [ ] **EXP-12** `[emulator ok]` `[emulator ok, verified 2026-11-15]`
  - Action: In the file browser, tap a directory that holds files and has no sub-directories.
  - Expected: Its file list opens.
  - Source: (spec 11, R1-1)
- [ ] **EXP-13** `[emulator ok]`
  - Action: In the file browser, go into a directory that holds both files and sub-directories.
  - Expected: The first row is "Files in this folder" with the number of files. Tapping it opens the file list.
  - Source: (spec 11, R1-1)
- [ ] **EXP-14** `[emulator ok]` `[emulator ok, verified 2026-11-15]`
  - Action: Choose "All files", then "Media folders".
  - Expected: All files are in one list. Then the folder list is shown as before.
  - Source: (spec 11, R1-1)
- [ ] **EXP-15** `[emulator ok]`
  - Action: Choose a view, close the app from the recent apps list and open it again.
  - Expected: The app opens in the chosen view.
  - Source: (spec 11, R1-1)
- [ ] **EXP-16** `[emulator ok]` `[emulator ok, verified 2026-11-15]`
  - Action: Press the three-dot button of a folder and choose "Add to favorites".
  - Expected: The folder gets a star.
  - Source: (spec 11, R1-4)
- [ ] **EXP-17** `[emulator ok]` `[emulator ok, verified 2026-11-15]`
  - Action: Choose the view "Favorites".
  - Expected: Only the folders with a star are listed.
  - Source: (spec 11, R1-1)
- [ ] **EXP-18** `[emulator ok]`
  - Action: In "Favorites", open the options of the folder and choose "Remove from favorites".
  - Expected: The folder leaves the list. With no favorites, a text explains how to add one.
  - Source: (spec 11, R1-4)
- [ ] **EXP-19** `[emulator ok]` `[emulator ok, verified 2026-11-15]`
  - Action: Press "Grid" in the folder list and in a file list.
  - Expected: Tiles instead of rows. Files show a large thumbnail with the name under it. Folders show a folder symbol with the name. The entry now reads "List".
  - Source: (spec 11, R1-2)
- [ ] **EXP-20** `[physical device required]` `[emulator ok, verified 2026-11-15]`
  - Action: With the grid on, turn the phone to landscape and back.
  - Expected: Two columns in portrait, four in landscape. Thumbnails load.
  - Source: (spec 11, R1-2)
- [ ] **EXP-21** `[emulator ok]`
  - Action: In the grid, open a file, go back, sort and search.
  - Expected: Everything works as in the list. The progress bar and the NEW and Watched marks are on the tiles.
  - Source: (spec 11, R1-2)
- [ ] **EXP-22** `[emulator ok]` `[emulator ok, verified 2026-11-15]`
  - Action: In Settings, "Interface", turn off "Show folders with audio only". Go back to the folder list.
  - Expected: The folder with only audio files is gone, in every view. Turning it on brings it back.
  - Source: (spec 11, R1-3)
- [ ] **EXP-23** `[emulator ok]`
  - Action: Press "Grid" in a file list, close the app from the recent apps list and open it again.
  - Expected: The lists still show tiles.
  - Source: (spec 11, R1-2, "Stored in UiSettings.Layout")

## Settings (spec 11, Phase 2 scope)

- [ ] **SET-1** `[emulator ok]` `[emulator ok, verified 2026-11-15]`
  - Action: Open the options of a folder and choose "Default speed for this folder", then "1.5x". Open a file of that folder that was never played.
  - Expected: It plays at 1.5x and the speed chip shows 1.5x.
  - Source: (spec 11, R2-1)
- [ ] **SET-2** `[emulator ok]` `[emulator ok, verified 2026-11-15]`
  - Action: Open a never-played file of another folder.
  - Expected: It plays at the general default speed.
  - Source: (spec 11, R2-1)
- [ ] **SET-3** `[emulator ok]`
  - Action: In the folder with 1.5x, open a file, set its speed to 2.0x in the Speed sheet, go back and open it again. Then open another file of the folder.
  - Expected: The first file plays at 2.0x (its own value wins). The other file plays at 1.5x.
  - Source: (spec 11, R2-1)
- [ ] **SET-4** `[emulator ok]`
  - Action: Choose "Default speed for this folder", then "Use the general default". Open a never-played file of the folder.
  - Expected: It plays at the general default speed.
  - Source: (spec 11, R2-1)
- [ ] **SET-5** `[emulator ok]`
  - Action: In Settings, "Interface", move "Size of names" to 140%. Go back to the folder list and to a file list, in list and in grid.
  - Expected: Folder names and file names are larger. The detail lines keep their size.
  - Source: (spec 11, R2-2)
- [ ] **SET-6** `[emulator ok]`
  - Action: In Settings, "Interface", set "Speed while holding the picture" to 3.0x. Close the app from the recent apps list and open it again. Open the Interface page.
  - Expected: The value shows 3.0x and is kept after the app is closed and opened.
  - Source: (spec 11, R2-3)

- [ ] **SET-7** `[emulator ok]`
  - Action: Set a default speed for a folder ("Default speed for this folder") and "Size of names" to 140%. Close the app from the recent apps list and open it again.
  - Expected: The names are still large. A never-played file of the folder plays at the folder's speed.
  - Source: (spec 11, R2-1, R2-2)

## Player (spec 11, Phase 3 scope)

- [ ] **PLR-1** `[emulator ok]` `[emulator ok, verified 2026-11-15]`
  - Action: Open a video and show the controls.
  - Expected: The file name has the first line to itself. Under it: clock and battery, the speed chip, Aspect, the three-dot menu, Lock. There is no Back button.
  - Source: (spec 11, R3-2, R3-3, R3-4)
- [ ] **PLR-2** `[emulator ok]` `[emulator ok, verified 2026-11-15]`
  - Action: Look at the bottom bar.
  - Expected: The seek bar with the two times, and Previous, Play or Pause, Next. Nothing else.
  - Source: (spec 11, R3-6)
- [ ] **PLR-3** `[emulator ok]` `[emulator ok, verified 2026-11-15]`
  - Action: Hold the phone in landscape and play a video that does not fill the screen height (a 4:3 video is best). Tap the picture to show the controls, then tap again to hide them.
  - Expected: The status bar and the navigation bar appear with the controls and disappear with them. The picture does not move or change size. The bars are see-through over the picture, with white symbols.
  - Source: (spec 11, R3-1)
- [ ] **PLR-4** `[physical device required]` `[emulator ok, verified 2026-11-15]`
  - Action: Repeat PLR-3 in landscape on a phone with a camera hole or a notch.
  - Expected: The control bars keep clear of the system bars and of the camera hole. The picture does not move.
  - Source: (spec 11, R3-1)
- [ ] **PLR-5** `[emulator ok]`
  - Action: Let the controls hide by themselves during playback.
  - Expected: The system bars hide at the same moment.
  - Source: (spec 11, R3-1)
- [ ] **PLR-6** `[emulator ok]` `[emulator ok, verified 2026-11-15]`
  - Action: Press the three-dot menu.
  - Expected: A list with "Subtitles", "Audio track", "Repeat" and "Decoder: HW" (or SW) opens.
  - Source: (spec 11, R3-5)
- [ ] **PLR-7** `[emulator ok]` `[emulator ok, verified 2026-11-15]`
  - Action: Choose "Audio track", close the sheet, then open the menu and choose "Subtitles", then "Repeat".
  - Expected: Each one opens its sheet as before.
  - Source: (spec 11, R3-5)
- [ ] **PLR-8** `[emulator ok]` `[emulator ok, verified 2026-11-15]`
  - Action: Open the menu and choose "Decoder: HW". Open the menu again.
  - Expected: The picture restarts at the same position. The item now reads "Decoder: SW".
  - Source: (spec 11, R3-5)
- [ ] **PLR-9** `[emulator ok]`
  - Action: Press the speed chip in the top bar.
  - Expected: The Speed sheet opens as before.
  - Source: (spec 11, R3-3)
- [ ] **PLR-10** `[emulator ok]` `[emulator ok, verified 2026-11-15]`
  - Action: Press the system Back button.
  - Expected: The player closes and the file list is shown with its system bars.
  - Source: (spec 11, R3-4)
- [ ] **PLR-20** `[emulator ok]` `[emulator ok, verified 2026-11-15]`
  - Action: Play a video, press the system Back button, and look at the bottom of the file list. Repeat three times, in portrait and in landscape.
  - Expected: The list reaches the navigation bar every time. There is no empty band above the bar.
  - Source: (spec 11, R3-1; found on the API 34 emulator)
- [ ] **PLR-21** `[emulator ok]` `[emulator ok, verified 2026-11-15]`
  - Action: Choose the theme "Light" in Settings, "Theme". Play a video and show the controls. Press Home, open the app again, then press Back.
  - Expected: In the player the bars are see-through with white symbols, also after the return from Home. After Back the bars are light with dark symbols again.
  - Source: (spec 07; spec 11, R3-1)
- [ ] **PLR-11** `[physical device required]` `[emulator ok, verified 2026-11-15]`
  - Action: During playback drag a finger slowly to the right on the picture and hold it.
  - Expected: Playback stops. The picture shows the frame of the target position and changes while the finger moves. The indicator shows the change and the target time.
  - Source: (spec 11, R3-7)
- [ ] **PLR-12** `[physical device required]` `[emulator ok, verified 2026-11-15]`
  - Action: Lift the finger.
  - Expected: Playback continues from the target position. If the video was paused before the drag, it stays paused on the target frame.
  - Source: (spec 11, R3-7)
- [ ] **PLR-13** `[physical device required]`
  - Action: Drag back and forth quickly for several seconds.
  - Expected: The picture follows without freezing the app. After the lift, playback is at the last target.
  - Source: (spec 11, R3-7)
- [ ] **PLR-14** `[physical device required]` `[emulator ok, verified 2026-11-15]`
  - Action: During playback hold a finger still on the picture for more than half a second.
  - Expected: Playback speeds up to the value of "Speed while holding the picture" and an indicator shows it.
  - Source: (spec 11, R3-8)
- [ ] **PLR-15** `[physical device required]`
  - Action: Lift the finger. Open the file again later.
  - Expected: The speed returns to what it was before at once. The file's saved speed did not change.
  - Source: (spec 11, R3-8)
- [ ] **PLR-16** `[physical device required]`
  - Action: With the general default speed at 2.0x and "Speed while holding the picture" at 3.0x, open a new file and hold a finger on the picture.
  - Expected: 2.0x normally, 3.0x while holding, 2.0x after the lift.
  - Source: (spec 11, R3-8; the owner's example)
- [ ] **PLR-17** `[emulator ok]`
  - Action: Hold a finger on the picture while the video is paused, and again while the player's Lock is on.
  - Expected: Nothing happens.
  - Source: (spec 11, R3-8)
- [ ] **PLR-18** `[emulator ok]`
  - Action: Tap, double tap, drag for volume and brightness, pinch.
  - Expected: All work as in Phase 3. A short tap is still a tap.
  - Source: (spec 11, R3-8; regression)
- [ ] **PLR-19** `[emulator ok]` `[emulator ok, verified 2026-11-15]`
  - Action: Switch the interface to Hebrew and open the player and the folder options.
  - Expected: The menu, the folder options and the new settings rows are in Hebrew. The bottom bar of the player is unchanged.
  - Source: (spec 10; spec 11)

## Report back

Write the results in the table at the top of this file and send:

- Phone model and Android version: ____
- Number of failed items: ____
- Failed item IDs: ____
- What was seen instead (for each failed item): ____
- Items not run, and why: ____
