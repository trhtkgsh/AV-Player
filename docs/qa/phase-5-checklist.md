# Phase 5 device checklist

- Phase: 5 (quiet display during playback, spec 12; rename and delete from the file list, spec 01 requirements 12 to 14)
- Date: 2026-11-15
- APK to install on a phone: `artifacts\ArielPlayer-phase5.apk` (copy it to the phone and open it, or `adb install -r artifacts\ArielPlayer-phase5.apk`; it installs over the app on the phone and keeps its data)
- SHA-256 of the APK: BEADD66B82991289F0DECC8BAB9088DFAC41227798260EB51FD28BBBAE7E19DA
- Developer build to an emulator or a phone with USB debugging: `dotnet build src\Player.App -f net10.0-android -t:Install`
- Owner's phone: Samsung Galaxy A55, Android 17, One UI 9

## Phone under test

| Phone model | Android version | Result (PASS / FAIL, and the item IDs that failed) |
|---|---|---|
|  |  |  |
|  |  |  |

## How to read this list

- `[emulator ok]`: can be run on an emulator.
- `[physical device required]`: must be run on a phone. Picture-in-Picture, background play and Bluetooth are always physical.
- Every item has one expected result. If the result is not as written, write what you saw in the report.
- Settings path for the quiet display: Settings, Interface, section "Player", group "While the controls are hidden". Its values: margin 0 to 48, text size 10 to 24.
- The quiet display is the thin line and the text that show while the controls are hidden. It is the same in portrait and landscape.
- The earlier lists (`phase-4-checklist.md`, `phase-4-r2-checklist.md`) are not repeated here. Section 5 lists the Phase 4 items that were never reported. Section 6 is the short regression.
- Items marked "Hebrew" need the phone language set to Hebrew (Settings, Language, or the device language). Set it back to English after those items.

## Setup

Put these files on the phone (internal storage, for example `Movies/`).

1. A film or an episode of 20 minutes or more (used by most items).
2. A folder with at least three videos. One of them is a spare copy that you may rename and delete (used by section 4). Do not use the film for rename or delete.
3. A video with a subtitle file `name.srt` next to it, with the same base name (used by FIL-20 and REG-12).
4. A wired headset with a button, or Bluetooth headphones, or both (used by FOC-4, BTN-1 to BTN-4).
5. A second app that plays music (used by FOC-1, FOC-2).
6. A portrait video (used by OVL-25, so that the picture has bars in landscape).

## 1. Install (3 items)

- [ ] **INS-1** `[physical device required]`
  - Action: Install `ArielPlayer-phase5.apk` over the app that is on the phone (copy and open it, or `adb install -r`).
  - Expected: It installs as an update. No uninstall is needed.
  - Source: (device-qa spec, step 1; task P5-60)
- [ ] **INS-2** `[physical device required]`
  - Action: Open the app. Go to Settings and look at a setting you changed before the update (for example a theme or a speed default).
  - Expected: The setting still has the value you chose.
  - Source: (task P5-60, section 1)
- [ ] **INS-3** `[physical device required]`
  - Action: Before installing, note the position of the film. After installing, open the film from the list.
  - Expected: It resumes at the same position.
  - Source: (task P5-60, section 1; spec 04)

## 2. Quiet display (OVL, 39 items)

- [ ] **OVL-1** `[emulator ok]`
  - Action: With the settings still at their defaults (see OVL-2), open the film and let the controls hide.
  - Expected: A thin line at the bottom edge in the accent (progress) color. In the top right corner, white text with "elapsed / total", the clock and the battery percent, flush with the screen edges.
  - Source: (spec 12, requirements 2, 3, 5, 7)
- [ ] **OVL-2** `[emulator ok]`
  - Action: Open Settings, Interface, section "Player", group "While the controls are hidden".
  - Expected: Progress line on, clock on, battery on. Line edge bottom. Times "elapsed / total". Corner top right. Margin 0. Text size 12. Color white.
  - Source: (spec 12, requirement 7)
- [ ] **OVL-3** `[emulator ok]`
  - Action: Play the film. Tap the picture so the controls show.
  - Expected: The line and the text disappear.
  - Source: (spec 12, requirement 1)
- [ ] **OVL-4** `[emulator ok]`
  - Action: Let the controls hide (or tap the picture again).
  - Expected: The line and the text come back.
  - Source: (spec 12, requirement 1)
- [ ] **OVL-5** `[emulator ok]`
  - Action: With the line and text shown, double tap the right side of the picture.
  - Expected: Playback jumps forward. The text does not block the tap.
  - Source: (spec 12, requirement 1, "takes no touches")
- [ ] **OVL-6** `[emulator ok]`
  - Action: With the line and text shown, drag a finger up or down on the right side for volume. Lift the finger.
  - Expected: While the finger is down the line and the text are hidden. They come back after the finger is lifted.
  - Source: (spec 12, requirement 1, "no gesture indicator is on")
- [ ] **OVL-7** `[physical device required]`
  - Action: Play the film and press the Picture-in-Picture button.
  - Expected: The small window shows the picture only, with no line and no text.
  - Source: (spec 12, requirement 1; spec 08, requirement 7)
- [ ] **OVL-8** `[emulator ok]`
  - Action: Play to about the middle of the film with the controls hidden. Look at the line.
  - Expected: The line is filled from the left to about half the width. The unfilled part is not drawn.
  - Source: (spec 12, requirement 2)
- [ ] **OVL-9** `[emulator ok]`
  - Action: Change the theme in Settings. Go back to the player with the controls hidden.
  - Expected: The line has the progress color of the new theme.
  - Source: (spec 12, requirement 2; spec 07, requirement on theme tokens)
- [ ] **OVL-10** `[emulator ok]`
  - Action: Set "Line edge" to Top. Go back to the player with the controls hidden.
  - Expected: The line is at the top edge of the screen.
  - Source: (spec 12, requirements 2 and 7)
- [ ] **OVL-11** `[emulator ok]`
  - Action: Set "Line edge" back to Bottom. Go back to the player.
  - Expected: The line is at the bottom edge of the screen.
  - Source: (spec 12, requirements 2 and 7)
- [ ] **OVL-12** `[emulator ok]`
  - Action: Turn the progress line switch off. Go back to the player.
  - Expected: No line is drawn. The text is unchanged.
  - Source: (spec 12, requirement 7)
- [ ] **OVL-13** `[emulator ok]`
  - Action: Turn the clock switch off. Go back to the player.
  - Expected: No clock is shown. The line and the battery are unchanged.
  - Source: (spec 12, requirements 3 and 7)
- [ ] **OVL-14** `[emulator ok]`
  - Action: Turn the battery switch off. Go back to the player.
  - Expected: No battery percent is shown. The line and the clock are unchanged.
  - Source: (spec 12, requirements 3 and 7)
- [ ] **OVL-15** `[emulator ok]`
  - Action: Set the times to "Off". Go back to the player.
  - Expected: No times are shown. The clock and the battery are still shown.
  - Source: (spec 12, requirements 4 and 7)
- [ ] **OVL-16** `[emulator ok]`
  - Action: Set the times to "Elapsed". Go back to the player.
  - Expected: Only the elapsed time is shown, followed by the clock and the battery.
  - Source: (spec 12, requirements 4 and 7)
- [ ] **OVL-17** `[emulator ok]`
  - Action: Set the times to "Elapsed / total". Go back to the player.
  - Expected: Elapsed and total time are shown, separated by " / ".
  - Source: (spec 12, requirements 4 and 7)
- [ ] **OVL-18** `[emulator ok]`
  - Action: Set the times to "Remaining". Go back to the player.
  - Expected: Only the remaining time is shown, with a minus sign in front (for example -32:30).
  - Source: (spec 12, requirements 4 and 7)
- [ ] **OVL-19** `[emulator ok]`
  - Action: Set the times to "Elapsed / remaining". Go back to the player.
  - Expected: Elapsed time, then the remaining time with a minus sign.
  - Source: (spec 12, requirements 4 and 7)
- [ ] **OVL-20** `[emulator ok]`
  - Action: Set the times to "Elapsed / total / remaining". Go back to the player.
  - Expected: Elapsed, total and remaining time are shown, the remaining time with a minus sign.
  - Source: (spec 12, requirements 4 and 7)
- [ ] **OVL-21** `[emulator ok]`
  - Action: Set the corner to Top left. Go back to the player with the controls hidden.
  - Expected: The text is in the top left corner of the screen, flush with the edges (margin 0).
  - Source: (spec 12, requirements 5 and 7)
- [ ] **OVL-22** `[emulator ok]`
  - Action: Set the corner to Bottom left. Go back to the player.
  - Expected: The text is in the bottom left corner of the screen, flush with the edges.
  - Source: (spec 12, requirements 5 and 7)
- [ ] **OVL-23** `[emulator ok]`
  - Action: Set the corner to Bottom right. Go back to the player.
  - Expected: The text is in the bottom right corner of the screen, flush with the edges.
  - Source: (spec 12, requirements 5 and 7)
- [ ] **OVL-24** `[emulator ok]`
  - Action: Set the corner to Top right. Go back to the player.
  - Expected: The text is in the top right corner of the screen, flush with the edges.
  - Source: (spec 12, requirements 5 and 7)
- [ ] **OVL-25** `[emulator ok]`
  - Action: With the corner set to Bottom left, play the portrait video and turn the phone to landscape. Look at the bars at the sides of the picture.
  - Expected: The text is in the bottom left corner of the screen, not of the picture.
  - Source: (spec 12, requirement 5)
- [ ] **OVL-26** `[emulator ok]` (Hebrew)
  - Action: With the phone in Hebrew, set the corner to Top left and go back to the player.
  - Expected: The text is in the top left corner. It is not mirrored to the top right.
  - Source: (spec 12, requirement 5; spec 10)
- [ ] **OVL-27** `[emulator ok]` (Hebrew)
  - Action: With the phone in Hebrew, watch the line with the controls hidden.
  - Expected: The line fills from the left, as in English.
  - Source: (spec 12, requirement 2)
- [ ] **OVL-28** `[emulator ok]` (Hebrew)
  - Action: With the phone in Hebrew, look at the text with the controls hidden.
  - Expected: The text reads left to right, in the same order as in English: times, clock, battery.
  - Source: (spec 12, requirement 3)
- [ ] **OVL-29** `[emulator ok]`
  - Action: Set the corner to Top right and the margin to 16. Go back to the player.
  - Expected: The text stands clearly away from the top edge and from the right edge.
  - Source: (spec 12, requirement 5)
- [ ] **OVL-30** `[emulator ok]`
  - Action: Drag the margin slider to the left end, then to the right end.
  - Expected: The slider stops at 0 and at 48.
  - Source: (spec 12, requirements 5 and 7)
- [ ] **OVL-31** `[physical device required]`
  - Action: Set the margin to 0. Use the corner where the screen is rounded or the camera hole is. Go back to the player with the controls hidden.
  - Expected: The text is flush with the edges. A rounded corner or the camera hole may cut part of it. This is expected. Write in the report which corner and how it looks.
  - Source: (spec 12, requirement 5)
- [ ] **OVL-32** `[emulator ok]`
  - Action: Set the text size to 10, go back to the player. Then set it to 24, go back again.
  - Expected: The text is smaller at 10 and larger at 24. The slider stops at 10 and at 24.
  - Source: (spec 12, requirements 6 and 7)
- [ ] **OVL-33** `[emulator ok]`
  - Action: In the color choice, pick each color in the list, one after the other. Go back to the player after each one.
  - Expected: The text takes that color. The line keeps the progress color. Set white again at the end.
  - Source: (spec 12, requirements 6 and 7)
- [ ] **OVL-34** `[emulator ok]`
  - Action: Change the corner in Settings. Go back to the player without restarting the app.
  - Expected: The new corner shows at once.
  - Source: (spec 12, requirement 7, acceptance "takes effect at once")
- [ ] **OVL-35** `[emulator ok]`
  - Action: Turn off the progress line, the clock and the battery, and set the times to "Off". Go back to the player with the controls hidden.
  - Expected: Nothing is shown. The picture is unchanged.
  - Source: (spec 12, requirements 2, 3 and 4)
- [ ] **OVL-36** `[emulator ok]`
  - Action: With the clock shown and the controls hidden, note the time. Wait until the minute changes, without restarting the app.
  - Expected: The clock shows the new minute within about one minute.
  - Source: (spec 12, requirement 8)
- [ ] **OVL-37** `[emulator ok]`
  - Action: Open the quick settings and note the battery percent. Close them. Show and hide the controls once.
  - Expected: The battery percent in the text is the same as in the quick settings.
  - Source: (spec 12, requirement 8)
- [ ] **OVL-38** `[emulator ok]`
  - Action: Play the film with the times shown, then pause it.
  - Expected: The elapsed time moves while playing and stops while paused.
  - Source: (spec 12, requirement 8)
- [ ] **OVL-39** `[emulator ok]`
  - Action: With the film playing, show and hide the controls several times.
  - Expected: The picture does not move, resize or jump when the line and the text appear or disappear.
  - Source: (spec 12, acceptance "nothing moves the picture")

## 3. Controls (CTL, 3 items)

- [ ] **CTL-1** `[emulator ok]`
  - Action: Play the film and show the controls.
  - Expected: The seek row shows the elapsed time at the left and "-remaining / total" at the right (for example -32:30 / 45:00).
  - Source: (spec 12, requirement 9)
- [ ] **CTL-2** `[emulator ok]`
  - Action: Drag the seek bar slowly from left to right and back.
  - Expected: Both times change while the finger drags. The elapsed time stays at the left and the remaining and total time at the right.
  - Source: (spec 12, requirement 9)
- [ ] **CTL-3** `[emulator ok]` (Hebrew)
  - Action: With the phone in Hebrew, play the film and show the controls.
  - Expected: The seek row looks the same as in English: elapsed at the left, "-remaining / total" at the right.
  - Source: (spec 12, requirement 9; spec 10, acceptance "transport bar identical in both languages")

## 4. File options (FIL, 23 items)

Use the spare copy from setup 2 for every item that renames or deletes a file. The rename prompt shows the name without the extension and adds the extension again.

- [ ] **FIL-1** `[emulator ok]`
  - Action: In the folder list, tap the ⋮ button on a file row.
  - Expected: A menu with "Rename" and "Delete" opens.
  - Source: (explorer-ui decisions, P5-20)
- [ ] **FIL-2** `[emulator ok]`
  - Action: Switch to Grid. Tap the ⋮ button on a file tile.
  - Expected: The same menu with "Rename" and "Delete" opens.
  - Source: (explorer-ui decisions, P5-20)
- [ ] **FIL-3** `[emulator ok]`
  - Action: Tap "Rename".
  - Expected: The name box shows the file name without its extension.
  - Source: (explorer-ui decisions, P5-20)
- [ ] **FIL-4** `[emulator ok]`
  - Action: Type a new name, confirm, and tap "Allow" in Android's dialog "Allow Ariel Player to modify this video?".
  - Expected: The row shows the new name at once. The list does not reload or flash.
  - Source: (spec 01, requirement 12; file-ops decisions, P5-10 and P5-11)
- [ ] **FIL-5** `[emulator ok]`
  - Action: Rename a file, and tap "Deny" in Android's dialog.
  - Expected: The name does not change. No error message and no crash.
  - Source: (file-ops decisions, P5-10 and P5-11)
- [ ] **FIL-6** `[emulator ok]`
  - Action: Rename a file and clear the name box. Confirm.
  - Expected: A message appears and the rename prompt comes back with the typed text.
  - Source: (explorer-ui decisions, P5-20)
- [ ] **FIL-7** `[emulator ok]`
  - Action: Rename a file to a name that contains a slash (/). Confirm.
  - Expected: A message appears and the rename prompt comes back. The same is true for \ : * ? " < > |.
  - Source: (explorer-ui decisions, P5-20; FileNameRules)
- [ ] **FIL-8** `[emulator ok]`
  - Action: Rename a file and type a very long name (hold a key until the field stops taking letters, at 200). Confirm.
  - Expected: Either the field stops at 200 letters, or a message appears and the rename prompt comes back. No crash. (The limit is 200 characters for the whole file name.)
  - Source: (explorer-ui decisions, P5-20; FileNameRules.MaxLength)
- [ ] **FIL-9** `[emulator ok]`
  - Action: Rename a file to its own current name. Confirm.
  - Expected: The prompt closes quietly. No message and no Android dialog.
  - Source: (explorer-ui decisions, P5-20)
- [ ] **FIL-10** `[emulator ok]`
  - Action: Rename a file to the name of another file in the same folder. Confirm.
  - Expected: A message appears. Nothing changes in the list.
  - Source: (file-ops decisions, P5-11; explorer-ui decisions, P5-20)
- [ ] **FIL-11** `[emulator ok]`
  - Action: Play the spare copy to about 5 minutes, go back, rename it and tap "Allow". Open it again.
  - Expected: It resumes at the same position.
  - Source: (spec 01, requirement 14; file-ops decisions, P5-10)
- [ ] **FIL-12** `[emulator ok]`
  - Action: Set the spare copy to 1.5x in the player. Go back, rename it and tap "Allow". Open it again.
  - Expected: It plays at 1.5x.
  - Source: (spec 01, requirement 14; explorer-ui decisions, P5-21)
- [ ] **FIL-13** `[emulator ok]`
  - Action: After FIL-11, look at the renamed file in the list.
  - Expected: It has no NEW tag.
  - Source: (spec 01, requirements 4 and 14)
- [ ] **FIL-14** `[emulator ok]`
  - Action: Tap the ⋮ button on a file row and tap "Delete".
  - Expected: A prompt "Move <file name> to the trash?" with the file's name appears.
  - Source: (explorer-ui decisions, P5-20)
- [ ] **FIL-15** `[emulator ok]`
  - Action: Confirm the prompt, and tap "Allow" in Android's dialog.
  - Expected: The row disappears and the folder's item count goes down by one.
  - Source: (spec 01, requirement 13; file-ops decisions, P5-10)
- [ ] **FIL-16** `[emulator ok]`
  - Action: Open Gallery or My Files, find the trash and look for the deleted file. Restore it.
  - Expected: The file is in the trash and can be restored. After the restore it is back in its folder.
  - Source: (file-ops decisions, P5-10; spec 01, requirement 13)
- [ ] **FIL-17** `[emulator ok]`
  - Action: Delete a file, confirm, and tap "Deny" in Android's dialog.
  - Expected: Nothing changes in the list.
  - Source: (file-ops decisions, P5-10)
- [ ] **FIL-18** `[physical device required]`
  - Action: With "Keep playing sound in the background" on, play a video and press Home. Open the app from the launcher, go to the folder list and rename that same file. Tap "Allow".
  - Expected: Playback closes first, and the sound stops before the rename is done.
  - Source: (explorer-ui decisions, P5-20; spec 08, requirement 2)
- [ ] **FIL-19** `[physical device required]`
  - Action: Play a video in Picture-in-Picture (the PiP button). Open the app from the launcher, go to the folder list and delete that file. Tap "Allow".
  - Expected: Playback closes first, and the small window is gone before the delete. The row disappears.
  - Source: (explorer-ui decisions, P5-20; spec 08, requirement 8)
- [ ] **FIL-20** `[emulator ok]`
  - Action: Rename the video that has `name.srt` next to it (setup 3). Tap "Allow". Open the renamed video.
  - Expected: The subtitle file keeps its old name and is not loaded automatically. This is a known limit: write in the report whether it bothers you.
  - Source: (explorer-ui decisions, P5-20, known gap; file-ops decisions, P5-10)
- [ ] **FIL-21** `[emulator ok]`
  - Action: Rename a file, and while Android's dialog is open, rotate the phone. Then tap "Allow". Pull down the list to refresh.
  - Expected: After the refresh the list shows the new name. If the app said "cancelled" although the file was renamed, write it in the report: this is a known gap.
  - Source: (file-ops decisions, P5-11, known gap)
- [ ] **FIL-22** `[emulator ok]` (Hebrew)
  - Action: With the phone in Hebrew, open "Rename" on a file, type a name and tap "Allow".
  - Expected: The menu, the prompt and the messages are in Hebrew. The rename works as in English.
  - Source: (spec 10, acceptance; explorer-ui decisions, P5-20)
- [ ] **FIL-23** `[emulator ok]` (Hebrew)
  - Action: With the phone in Hebrew, tap "Delete" on a file and confirm.
  - Expected: The "move to the trash" text is in Hebrew. The delete works as in English.
  - Source: (spec 10, acceptance; explorer-ui decisions, P5-20)

## 5. Phase 4 items never reported (13 items)

These are short forms of the items in `phase-4-checklist.md`. Use that file for the full steps if needed.

- [ ] **FOC-1** `[physical device required]` Audio focus: play a video, then start music in another app. Expected: the video pauses and stays paused. (phase-4-checklist, FOC-1; spec 08, requirement 4)
- [ ] **FOC-2** `[physical device required]` Audio focus: play music in another app, then open a video here. Expected: the other app stops and the video plays. (phase-4-checklist, FOC-2; spec 08, requirement 4)
- [ ] **FOC-3** `[physical device required]` Audio focus: during playback a phone call comes in or an alarm sounds, then ends. Expected: the video pauses during the call and continues after it. (phase-4-checklist, FOC-3; spec 08, requirement 4)
- [ ] **FOC-4** `[physical device required]` Headphones unplugged: play with headphones, then unplug them or switch them off. Expected: playback pauses at once and nothing plays from the speaker. (phase-4-checklist, FOC-4; spec 08, requirement 5)
- [ ] **BTN-1** `[physical device required]` Headset button single press: play, press the headset button once, then once again. Expected: pause, then play. (phase-4-checklist, BTN-1; spec 09, requirement 5)
- [ ] **BTN-2** `[physical device required]` Headset button double press: press the headset button twice quickly. Expected: the next file plays. (phase-4-checklist, BTN-2; spec 09, requirement 5)
- [ ] **BTN-3** `[physical device required]` Headset button triple press: press the headset button three times quickly. Expected: the previous file plays, or the file starts again when it is the first. (phase-4-checklist, BTN-3; spec 09, requirement 5)
- [ ] **BTN-4** `[physical device required]` Bluetooth buttons: use play, pause, next and previous on Bluetooth headphones, a speaker or a car. Expected: each one works. (phase-4-checklist, BTN-4; spec 09, requirement 6)
- [ ] **BG-1** `[physical device required]` 30 minutes in the background: turn on "Keep playing sound in the background", play the film, press Home, turn the screen off and wait 30 minutes with default battery settings. Expected: the sound plays the whole time. (phase-4-checklist, BG-1; spec 08, acceptance)
- [ ] **BG-5** `[emulator ok]` Notification: play a video and pull down the notification shade. Expected: a playback notification with the file name and the controls (previous, play or pause, next, close). (phase-4-checklist, BG-5; spec 08, requirement 1)
- [ ] **BG-6** `[emulator ok]` Notification controls: press Home during playback. Use pause, play, next and previous in the notification. Expected: each one works, and after next the notification shows the new file name. (phase-4-checklist, BG-6; spec 08, requirement 1)
- [ ] **BG-7** `[emulator ok]` Notification close: with the app in the background, close playback from the notification. Open the app. Expected: the sound stops, the notification goes away, and the app shows the file list. (phase-4-checklist, BG-7; spec 08, requirement 2)
- [ ] **BG-12** `[physical device required]` Force stop: play in the background, then in Android's settings open Apps, this app, "Force stop". Open the app and the same file. Expected: the file continues at most about 5 seconds before the position where it was stopped. (phase-4-checklist, BG-12; spec 08 and spec 04, acceptance)

## 6. Regression (REG, 17 items)

- [ ] **REG-1** `[emulator ok]` Explorer modes: Folders, Browser, Favorites and All files each show their list; Grid and List switch both pages. Expected: each mode and layout works as before. (spec 01, requirement 3; spec 11, R1-1)
- [ ] **REG-2** `[emulator ok]` Sort: sort by name, date, size and duration, ascending and descending. Expected: the order changes each time, and "ep2" comes before "ep10". (spec 01, requirement 8)
- [ ] **REG-3** `[emulator ok]` Search: type part of a file name. Expected: the list filters at once, and clearing the text shows all files again. (spec 01, requirement 9)
- [ ] **REG-4** `[emulator ok]` Favorites: add a folder to Favorites, then remove it. Expected: it appears and disappears in the "Favorites" mode. (explorer-ui decisions, Phase 3 revision; spec 11)
- [ ] **REG-5** `[emulator ok]` Folder speed: set "Default speed for this folder" to 1.5x and open a file in that folder that has no own speed. Expected: it plays at 1.5x. (spec 11, R2-1; spec 05, acceptance)
- [ ] **REG-6** `[emulator ok]` Open and resume: play a file for a minute, go back and open it again. Expected: it resumes near the last position. (spec 04, acceptance; spec 01, requirement 10)
- [ ] **REG-7** `[physical device required]` Seek drag: drag a finger slowly over the picture. Expected: the picture follows the finger and settles on the exact time when the finger rests. (spec 11, R3-7; spec 02, acceptance)
- [ ] **REG-8** `[emulator ok]` Volume and brightness: drag on the right side for volume and on the left side for brightness. Expected: both change at once, with no visible lag. (spec 02, acceptance)
- [ ] **REG-9** `[emulator ok]` Double tap: double tap the left and the right side. Expected: playback jumps back and forward. (spec 02, acceptance)
- [ ] **REG-10** `[emulator ok]` Long press: hold a finger on the picture. Expected: playback runs at the fast speed while the finger is held, and returns to the speed before when it is lifted. (spec 02, requirement 4; spec 11, R3-8)
- [ ] **REG-11** `[emulator ok]` Zoom: pinch to zoom in, drag the zoomed picture, change the Aspect (Fit, Crop, Stretch). Expected: all work, and the picture stays below the status bar. (spec 02, acceptance; spec 11, R3-1)
- [ ] **REG-12** `[emulator ok]` Subtitles and audio tracks: load the external `name.srt` (setup 3) and switch an audio track if the file has one. Expected: the subtitles show, Hebrew text reads right to left, and the audio track changes. (spec 03, acceptance)
- [ ] **REG-13** `[emulator ok]` Decoder switch: switch between hardware and software decoding during playback. Expected: playback resumes at the same position within 1 second. (spec 02, acceptance)
- [ ] **REG-14** `[emulator ok]` Repeat and A-B: set repeat, let a file end, then set an A-B loop. Expected: repeat works at the end of the file, and the loop plays between A and B. (spec 03, acceptance)
- [ ] **REG-15** `[emulator ok]` Lock: press the Lock button, then press Home and return. Expected: the screen is still locked, and only unlock works. (spec 02, acceptance)
- [ ] **REG-16** `[emulator ok]` Themes and Hebrew: switch all four themes with the list and the player open. Expected: everything recolors at once, including the system bars. In Hebrew the app is right to left. (spec 07, acceptance; spec 10, acceptance)
- [ ] **REG-17** `[physical device required]` Picture-in-Picture: enter and leave Picture-in-Picture 10 times, and use each action in the small window. Expected: the picture is there every time and nothing crashes. (spec 08, acceptance)

## Report back

Write the results in the table at the top of this file and send:

- Phone model and Android version: ____
- Number of failed items, per section (1 Install, 2 Quiet display, 3 Controls, 4 File options, 5 Phase 4, 6 Regression): ____
- Failed item IDs: ____
- What was seen instead (for each failed item): ____
- OVL-31: which corner had a margin of 0, and how the text looked there: ____
- FIL-20: did the subtitle file losing its automatic load bother you (yes or no)? ____
- FIL-21: did the app say "cancelled" although the file was renamed (yes or no)? ____
- Items not run, and why: ____
- BG-1, how long the sound played (minutes, and whether it stopped): ____
