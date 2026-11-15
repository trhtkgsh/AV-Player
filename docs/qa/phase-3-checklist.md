# Phase 3 device checklist

- Phase: 3 (theme, video surface, gestures, screen lock, Hebrew interface, and the fixes for the owner's feedback on the Phase 2 build)
- Date: 2026-11-15
- APK to install on a phone: `artifacts\ArielPlayer-phase3.apk` (copy it to the phone and open it, or `adb install -r artifacts\ArielPlayer-phase3.apk`)
- Developer build to an emulator or a phone with USB debugging: `dotnet build src\Player.App -f net10.0-android -t:Install`

## Phone under test

| Phone model | Android version | Result (PASS / FAIL, and the item IDs that failed) |
|---|---|---|
|  |  |  |
|  |  |  |

## How to read this list

- `[emulator ok]`: can be run on an emulator.
- `[physical device required]`: must be run on a phone.
- `[emulator ok, verified 2026-11-15]`: the orchestrator already checked the item on an emulator (Pixel 5 AVD, API 34, x86_64). It still needs its normal run on the phone. For the physical items with this marker, the behaviour was seen on the emulator, but sound, brightness, sharpness and a real Hebrew phone need the device.
- The app follows the language of the phone by default. On a Hebrew phone it is now in Hebrew and right to left. The choice is in Settings, "Interface", "Language": "System default", "English", "עברית". The items below use the English labels. In Hebrew the same controls are in the mirrored place.
- The bottom bar of the player (seek bar, times, speed, previous, play or pause, next, decoder) is in the same place in every language.
- The player hides the status bar and the navigation bar. The first time, Android shows its own hint ("Viewing full screen"). Confirm it once. A swipe from the top or bottom edge shows the bars for a moment.
- The player top bar holds, in order: Back, the title with a small line for the clock and the battery, then Subtitles, Audio, Repeat, Aspect and Lock.
- Gestures in the player: a tap shows or hides the controls. A double tap on the left or right half skips. A vertical drag on the right half changes the volume, and on the left half the brightness. A horizontal drag seeks. A pinch zooms. A drag while zoomed moves the picture.
- Playback still pauses when the app goes to the background. Background playback and Picture-in-Picture come in Phase 4.
- Known limit: changing the aspect mode after a file has ended can show a wrong picture until playback starts again.

## Setup

Put these files on the phone (internal storage, for example `Movies/`).

1. A 1080p (1920x1080) H.264 video of at least 2 minutes, recorded by a phone camera or downloaded (used by FIX-2, ASP-1, GES-7, GES-8, GES-9, GES-10, CTL-4).
2. A 4:3 video (used by ASP-2).
3. A vertical (9:16) video (used by ASP-2).
4. A folder with two short videos and a subtitle file next to one of them (the Phase 2 files `ep2.mp4`, `ep2.srt`, `ep10.mp4`) (used by ASP-4, ASP-5, GES-13).
5. A phone whose system language is Hebrew (for LANG-1), or change the system language for that item (used by FIX-1, LANG-1, LANG-7).

The regression blocks need the files of the earlier checklists as well: see Setup in `phase-2-checklist.md` and `phase-1-checklist.md`.

## Phase 3 items

### Fixes for the Phase 2 feedback

- [ ] **FIX-1** `[physical device required]` `[emulator ok, verified 2026-11-15]`
  - Action: On a phone set to Hebrew, open a video and show the controls.
  - Expected: The play triangle points right. "Previous" is left of play and "Next" is right of it. The seek bar fills from left to right.
  - Source: (owner feedback 7; spec 10, req 5)
- [ ] **FIX-2** `[physical device required]`
  - Action: Open the 1080p video with aspect mode Fit and hold the phone in landscape. Watch the edges of the picture for the first five seconds without touching anything. Then pause and play.
  - Expected: The picture has its final size from the start. It does not grow slightly after the first seconds, or after pause and play.
  - Source: (owner feedback 6; agent report: player-engine, P3-01)
- [ ] **FIX-3** `[physical device required]`
  - Action: While a video plays, press Back.
  - Expected: The picture keeps its shape until the page closes. It is not stretched over the whole screen for a moment.
  - Source: (owner feedback 6; agent report: video-surface, P3-01)

### Language (spec 10)

- [ ] **LANG-1** `[emulator ok]`
  - Action: With the phone in Hebrew, open Settings, "Interface" ("ממשק"), and choose "System default" ("ברירת המחדל של המכשיר").
  - Expected: The whole app is in Hebrew and laid out right to left.
  - Source: (spec 10, req 2 and 4)
- [ ] **LANG-2** `[emulator ok]` `[emulator ok, verified 2026-11-15]`
  - Action: On the Interface page, choose "English".
  - Expected: The page changes to English and left to right at once, without leaving the page.
  - Source: (spec 10, req 3)
- [ ] **LANG-3** `[emulator ok]` `[emulator ok, verified 2026-11-15]`
  - Action: Go back to the settings home and then to the file list.
  - Expected: Both are in English and left to right. Folder and file names are unchanged.
  - Source: (spec 10, req 3 and 8)
- [ ] **LANG-4** `[emulator ok]` `[emulator ok, verified 2026-11-15]`
  - Action: On the Interface page, choose "עברית" and go back to the file list.
  - Expected: Every text is Hebrew. For example: "כל הקבצים", "2 פריטים", "חדש", "נצפה", "מיון", "חיפוש".
  - Source: (spec 10, req 1 and 6)
- [ ] **LANG-5** `[emulator ok]` `[emulator ok, verified 2026-11-15]`
  - Action: With Hebrew chosen, open a video and show the controls.
  - Expected: The top bar is mirrored. Back is on the right and points right. The bottom bar is exactly as in English.
  - Source: (spec 10, req 5)
- [ ] **LANG-6** `[emulator ok]` `[emulator ok, verified 2026-11-15]`
  - Action: With Hebrew chosen, open the Speed, Repeat, Audio, Subtitles and subtitle style sheets.
  - Expected: All labels are Hebrew. Numbers with units (for example "1.5x" and "+50 ms") are unchanged.
  - Source: (spec 10, req 6 and 8)
- [ ] **LANG-7** `[emulator ok]`
  - Action: On a Hebrew phone, choose "English". Close the app from the recent apps list and open it again.
  - Expected: The app opens in English at once. It does not show Hebrew first.
  - Source: (spec 10, req 2; agent report: app-skeleton, P3-51)
- [ ] **LANG-8** `[emulator ok]`
  - Action: Look at the three language options in both languages.
  - Expected: "English" and "עברית" are always written in their own script. Each stays next to its radio button.
  - Source: (spec 10, req 2)

### Theme (spec 07)

- [ ] **TH-1** `[emulator ok]` `[emulator ok, verified 2026-11-15]`
  - Action: Open Settings, "Theme", and choose "Light".
  - Expected: The page, the title bar, the status bar and the navigation bar turn light at once. The status bar icons are dark.
  - Source: (spec 07, req 2 and 7)
- [ ] **TH-2** `[emulator ok]` `[emulator ok, verified 2026-11-15]`
  - Action: Go back to the settings home and to the file list.
  - Expected: Every page is light. No part stays dark. The text is easy to read.
  - Source: (spec 07, req 2, 4 and 10)
- [ ] **TH-3** `[emulator ok]`
  - Action: Choose "Dark Slate", then "High Contrast", then "Dark OLED".
  - Expected: Each one recolors the app at once. Dark OLED has a pure black background.
  - Source: (spec 07, req 1 and 2)
- [ ] **TH-4** `[emulator ok]`
  - Action: Tap another color under "Accent color".
  - Expected: Switches, sliders, buttons and the progress bars in the file list take the new color at once.
  - Source: (spec 07, req 5)
- [ ] **TH-5** `[emulator ok]`
  - Action: Move the "Player controls opacity" slider to 0% and to 100%, and watch "Preview". Then open a video and show the controls.
  - Expected: The bars of the preview and of the player are fully transparent at 0% and solid at 100%.
  - Source: (spec 07, req 6)
- [ ] **TH-6** `[emulator ok]`
  - Action: Choose "Light". Close the app from the recent apps list and open it again.
  - Expected: The app opens light. It does not flash dark first.
  - Source: (spec 07, req 8)
- [ ] **TH-7** `[emulator ok]`
  - Action: With "Light" chosen, open "Sort" in a file list and then the three-dot menu.
  - Expected: The dialog and the menu are light, with readable text.
  - Source: (agent report: theming-engine, P2-21)
- [ ] **TH-8** `[emulator ok]`
  - Action: Choose "Light". Then open a video, show the controls, and open the Speed sheet.
  - Expected: The player controls and the sheet are readable. They follow the "Player controls opacity" setting. The picture is not affected by the theme.
  - Source: (spec 07, req 2 and 6)

### Player window

- [ ] **WIN-1** `[emulator ok]` `[emulator ok, verified 2026-11-15]`
  - Action: Open a video.
  - Expected: The status bar and the navigation bar are hidden. The picture can use the whole screen, including the area next to the camera hole in landscape.
  - Source: (spec 02, layout; spec 07, req 7)
- [ ] **WIN-2** `[emulator ok]`
  - Action: Swipe down from the top edge.
  - Expected: The bars appear for a moment and then hide again.
  - Source: (agent report: video-surface, P3-01)
- [ ] **WIN-3** `[emulator ok]`
  - Action: Press Back to leave the player.
  - Expected: The bars are back in the file list. The screen brightness is back to the system value.
  - Source: (spec 02, req 4; agent report: player-hud-ui, P3-30)
- [ ] **WIN-4** `[emulator ok]` `[emulator ok, verified 2026-11-15]`
  - Action: Open a settings sheet (for example Speed) and close it.
  - Expected: The player is full screen again after the sheet closes.
  - Source: (agent report: player-hud-ui, P3-30)

### Gestures (spec 02, requirement 4)

- [ ] **GES-1** `[emulator ok]` `[emulator ok, verified 2026-11-15]`
  - Action: Tap the picture once. Tap it again.
  - Expected: The controls appear, then hide.
  - Source: (spec 02, req 3)
- [ ] **GES-2** `[emulator ok]` `[emulator ok, verified 2026-11-15]`
  - Action: Double tap the right half of the picture.
  - Expected: Playback jumps forward by the skip length (10 seconds by default). An indicator shows "+10 s".
  - Source: (spec 02, req 4)
- [ ] **GES-3** `[emulator ok]` `[emulator ok, verified 2026-11-15]`
  - Action: Double tap the left half of the picture.
  - Expected: Playback jumps back by the skip length. The indicator shows "-10 s".
  - Source: (spec 02, req 4)
- [ ] **GES-4** `[emulator ok]`
  - Action: In Settings, "Interface", set "Skip length" to 30 seconds. Open a video and double tap the right half.
  - Expected: The jump is 30 seconds.
  - Source: (spec 06, acceptance, Phase 3 item)
- [ ] **GES-5** `[physical device required]` `[emulator ok, verified 2026-11-15]`
  - Action: Drag up and down on the right half of the picture.
  - Expected: An indicator shows "Volume" with a percentage from 0% to 200%. The sound gets louder and quieter. Above 100% it is louder than the normal maximum.
  - Source: (spec 02, req 4)
- [ ] **GES-6** `[physical device required]` `[emulator ok, verified 2026-11-15]`
  - Action: Drag up and down on the left half of the picture.
  - Expected: An indicator shows "Brightness" with a percentage. The screen gets brighter and darker.
  - Source: (spec 02, req 4)
- [ ] **GES-7** `[emulator ok]` `[emulator ok, verified 2026-11-15]`
  - Action: Drag horizontally on the picture and hold. Then release.
  - Expected: While dragging, an indicator such as "[+0:14]  12:30" shows the change and the target time, and playback continues. On release, playback jumps to the target.
  - Source: (spec 02, req 4)
- [ ] **GES-8** `[physical device required]` `[emulator ok, verified 2026-11-15]`
  - Action: Pinch out on the picture as far as it goes, then pinch in.
  - Expected: The picture zooms up to 400% and back to 100%. An indicator shows "Zoom" with the percentage.
  - Source: (spec 02, req 4)
- [ ] **GES-9** `[physical device required]`
  - Action: Zoom a 1080p video to 400% and look at small text or fine detail.
  - Expected: The detail stays sharp. It is not a blurred enlargement.
  - Source: (spec 02, constraints from the spike; agent report: video-surface)
- [ ] **GES-10** `[physical device required]` `[emulator ok, verified 2026-11-15]`
  - Action: While zoomed, drag the picture in every direction as far as it goes.
  - Expected: The picture moves with the finger and stops at its edges. No empty area appears.
  - Source: (spec 02, req 4)
- [ ] **GES-11** `[physical device required]`
  - Action: Do each gesture ten times: tap, double tap left and right, volume drag, brightness drag, horizontal drag, pinch.
  - Expected: Each one is recognised every time. A diagonal drag never changes from volume to seek or back. A pinch never causes a seek.
  - Source: (agent report: gestures, P3-10)
- [ ] **GES-12** `[physical device required]`
  - Action: Use the volume, brightness and seek drags quickly.
  - Expected: The indicator follows the finger without a visible delay.
  - Source: (spec 02, acceptance)
- [ ] **GES-13** `[emulator ok]`
  - Action: Change the brightness with a drag. Then leave the player.
  - Expected: In the file list the brightness is back to the system value.
  - Source: (agent report: player-hud-ui, P3-30)

### Aspect modes (spec 02, requirement 6)

- [ ] **ASP-1** `[emulator ok]` `[emulator ok, verified 2026-11-15]`
  - Action: In a 16:9 video, press the Aspect button four times.
  - Expected: An indicator shows "Stretch", "Crop", "100%", "Fit" in this order. Stretch fills the whole screen and distorts the picture. Crop fills the screen and cuts the sides, or the top and bottom. 100% shows one video pixel per screen pixel. Fit shows the whole picture with black bars.
  - Source: (spec 02, req 6)
- [ ] **ASP-2** `[physical device required]`
  - Action: Repeat ASP-1 with the 4:3 video and with the vertical video, in portrait and in landscape.
  - Expected: Each mode looks right in every case.
  - Source: (agent spec: video-surface, acceptance)
- [ ] **ASP-3** `[emulator ok]`
  - Action: Zoom in, then press the Aspect button.
  - Expected: The zoom goes back to 100%.
  - Source: (spec 02, req 6)
- [ ] **ASP-4** `[emulator ok]`
  - Action: Set the aspect mode to "Crop" in one video and leave it. Open another video. Then open the first video again.
  - Expected: The other video uses Fit. The first video opens in Crop.
  - Source: (spec 04, per-file overrides; agent report: playback-coordinator, P3-40)
- [ ] **ASP-5** `[emulator ok]`
  - Action: In Settings, "Playback defaults", set "Aspect" to "Stretch". Open a video that was never played. Then set it back to "Fit".
  - Expected: The new video opens in Stretch.
  - Source: (spec 05, fields)
- [ ] **ASP-6** `[emulator ok]` `[emulator ok, verified 2026-11-15]`
  - Action: Pause a video and press the Aspect button four times.
  - Expected: The paused picture changes shape each time and stays visible.
  - Source: (agent report: video-surface)
- [ ] **ASP-7** `[emulator ok]` `[emulator ok, verified 2026-11-15]`
  - Action: Switch the decoder (HW or SW chip) and repeat ASP-1.
  - Expected: The four modes look the same with both decoders.
  - Source: (agent report: player-engine, P3-01)

### Screen lock (spec 02, requirement 5)

- [ ] **LOCK-1** `[emulator ok]` `[emulator ok, verified 2026-11-15]`
  - Action: Press the Lock button.
  - Expected: The controls hide. An indicator shows "Locked".
  - Source: (spec 02, req 5)
- [ ] **LOCK-2** `[emulator ok]` `[emulator ok, verified 2026-11-15]`
  - Action: While locked, drag, double tap and pinch on the picture.
  - Expected: Nothing happens to volume, brightness, position or zoom.
  - Source: (spec 02, req 5)
- [ ] **LOCK-3** `[emulator ok]` `[emulator ok, verified 2026-11-15]`
  - Action: Tap the picture once.
  - Expected: Only an unlock button appears, for about three seconds.
  - Source: (spec 02, req 5)
- [ ] **LOCK-4** `[emulator ok]` `[emulator ok, verified 2026-11-15]`
  - Action: Press the system Back button while locked.
  - Expected: The player stays open. The unlock button appears.
  - Source: (spec 02, req 5)
- [ ] **LOCK-5** `[physical device required]`
  - Action: While locked, turn the screen off and on, or switch to another app and come back.
  - Expected: The player is still locked.
  - Source: (agent report: player-hud-ui, P3-32)
- [ ] **LOCK-6** `[emulator ok]` `[emulator ok, verified 2026-11-15]`
  - Action: Tap the unlock button.
  - Expected: The controls are back and gestures work again.
  - Source: (spec 02, req 5)

### Player controls

- [ ] **CTL-1** `[emulator ok]` `[emulator ok, verified 2026-11-15]`
  - Action: Show the controls and look under the title.
  - Expected: A small line shows the time and the battery percentage.
  - Source: (spec 02, req 3)
- [ ] **CTL-2** `[emulator ok]`
  - Action: In Settings, "Interface", turn off "Show the clock" and "Show the battery" one after the other. Then look at the player.
  - Expected: Each part disappears from the line.
  - Source: (spec 06, fields)
- [ ] **CTL-3** `[emulator ok]`
  - Action: Hold the phone in portrait and open a video with a long name.
  - Expected: All buttons of the top bar are visible. The title is cut with an ellipsis.
  - Source: (spec 02, req 8)
- [ ] **CTL-4** `[physical device required]`
  - Action: Play a video, press HOME, and open the app again. Do this 20 times.
  - Expected: The picture comes back every time, in the same aspect mode.
  - Source: (spec 02, acceptance)

## Regression: Phase 2

- Run the regression with the theme "Dark OLED" and the English interface, unless an item says otherwise.
- The Speed, Repeat, Audio and Subtitles buttons and the labels are unchanged in English. In Hebrew the labels are translated.
- UI-2 now refers to the small line under the title.

### Run these first (real phone only)

- [ ] **ARM-1** `[physical device required]`: Open a video and pause it. Press the speed chip and close the "Speed" sheet by tapping outside it. Repeat this 10 times. Then open the Subtitles sheet and press "Style...". -> Every sheet opens and closes. The app never shows "isn't responding".
- [ ] **ARM-2** `[physical device required]`: With the video still paused, press "Larger" twice in the style sheet and close it. Press the decoder chip twice (HW to SW and back). Then press Play. -> The picture restarts each time at the same position and the app does not close. Playback continues.

### Playback state (spec 04)

- [ ] **STATE-1** `[emulator ok]`: Play a video for about 20 seconds. Press Back to go to the file list. -> The row of that file shows a progress bar. Its NEW tag is gone.
- [ ] **STATE-2** `[physical device required]`: Play a video for 30 seconds. In the system settings, open Apps, Ariel Player, and press Force stop. Open the app and open the same file. -> Playback resumes within 5 seconds of the last position.
- [ ] **STATE-3** `[emulator ok]`: Play a short video to its end. Go back to the file list. Open the file again. -> The row shows "Watched". The file starts from the beginning.
- [ ] **STATE-4** `[emulator ok]`: In file A, set the speed to 1.5x. Go back. Open file B. Go back. Open file A again. -> File B plays at 1.0x. File A plays at 1.5x.
- [ ] **STATE-5** `[emulator ok]`: In file A, switch the decoder chip to SW. Go back and open file A again. Then open another file. -> The chip shows SW for file A. The other file shows HW.
- [ ] **STATE-6** `[emulator ok]`: In a video with subtitles, choose "Off" in the Subtitles sheet. Go back and open the file again. -> No subtitles appear. The Subtitles sheet shows the check mark on "Off".

### Playback defaults (spec 05)

- [ ] **DEF-1** `[emulator ok]`: Open Settings, then "Playback defaults". Raise "Speed" to 1.25x. Open a file that was never played. -> The file plays at 1.25x. The speed chip shows 1.25x.
- [ ] **DEF-2** `[emulator ok]`: With the default speed at 1.25x, open the file that got 1.5x in STATE-4. -> The file plays at 1.5x.
- [ ] **DEF-3** `[emulator ok]`: In "Playback defaults", set "Decoder" to "Software". Open a file that was never played. -> The decoder chip shows SW.
- [ ] **DEF-4** `[emulator ok]`: Turn off "Resume from the last position". Open a file that has a progress bar. -> The file starts from the beginning.
- [ ] **DEF-5** `[emulator ok]`: Turn off "Show subtitles". Open `ep2.mp4`. -> No subtitles appear. The Subtitles sheet still lists `ep2.srt` under "Files next to the video".
- [ ] **DEF-6** `[emulator ok]`: Type the language code (for example `he` or `en`) of the second audio track of the MKV file into "Preferred audio language". Open that MKV file for the first time. -> The second audio track plays. The Audio sheet shows the check mark on it.
- [ ] **DEF-7** `[emulator ok]`: Type the language code (for example `he` or `en`) of an embedded subtitle track of the MKV file into "Preferred subtitle language". Turn on "Show subtitles". Open the MKV file for the first time. -> That subtitle track is shown.
- [ ] **DEF-8** `[emulator ok]`: In "Playback defaults", open "Subtitle style". Press "Larger" several times and choose the yellow text color. Open `ep2.mp4`. -> The subtitles are larger and yellow.
- [ ] **DEF-9** `[emulator ok]`: Change several defaults. Close the app from the recent apps list and open it again. Open "Playback defaults". -> The page shows the changed values.
- [ ] **DEF-10** `[emulator ok]`: Press "Reset all to defaults" and confirm. -> Speed is 1.0x. Decoder is "Hardware". Repeat is "Off". "Resume from the last position" is on. Delays are 0 ms. "Show subtitles" is on.

### Interface settings (spec 06)

- [ ] **UI-1** `[emulator ok]`: Open Settings, then "Interface". Change "Hide the controls after" to a different value. Play a video and do not touch the screen. -> The controls hide after the chosen time.
- [ ] **UI-2** `[emulator ok]`: Turn off "Show the clock". Open a video. -> The clock in the small line under the title of the player is gone.
- [ ] **UI-3** `[emulator ok]`: Under "Details shown for each file", turn off "Duration" and turn on "Folder path". Go back to a file list. -> The rows show no duration. The rows show the folder path.
- [ ] **UI-4** `[emulator ok]`: Change "Mark files as new for" to the shortest value. Go back to the file list. -> Only files that were added to the phone within that time and were never played have the NEW tag. Older files have no NEW tag.
- [ ] **UI-5** `[emulator ok]`: In a file list, press "Sort". -> The dialog is dark like the app. The selected option is clearly visible.

### Speed (spec 03)

- [ ] **SPD-1** `[emulator ok]`: During playback, press the speed chip. -> The "Speed" sheet opens at the bottom. The picture keeps playing behind it. The player controls are hidden while the sheet is open.
- [ ] **SPD-2** `[emulator ok]`: Press "1.5x". Then tap outside the sheet. -> The sheet closes. Playback is faster. The speed chip shows 1.5x.
- [ ] **SPD-3** `[physical device required]`: Set the speed to 0.25x. Listen to speech. -> The speech is slow, with the normal pitch. The voice is not deeper.
- [ ] **SPD-4** `[physical device required]`: Set the speed to 4.0x. Listen to speech. -> The speech is fast, with the normal pitch. The voice is not higher.

### Repeat and A-B repeat (spec 03)

- [ ] **REP-1** `[emulator ok]`: Press the "Repeat" button. Choose "This file". Let the file end. -> The same file starts again from the beginning.
- [ ] **REP-2** `[emulator ok]`: Press "Repeat" and choose "Folder". Let the last file of the folder end. -> The first file of the folder starts.
- [ ] **REP-3** `[emulator ok]`: Press "Repeat" and choose "Off". Let a file that is not the last one end. -> The next file starts. After the last file, playback stops.
- [ ] **REP-4** `[emulator ok]`: At about 0:05 press "Set A". At about 0:15 press "Set B". -> The sheet shows the A-B range. Playback jumps back to A each time it reaches B.
- [ ] **REP-5** `[emulator ok]`: Press "Clear". -> The range shows "Not set". Playback continues past B.
- [ ] **REP-6** `[emulator ok]`: Press "Set A". Seek to an earlier position and press "Set B". -> A message says "B must be at least 1 second after A." No loop is set.
- [ ] **REP-7** `[emulator ok]`: Press "Repeat" and choose "This file". Close the app from the recent apps list. Open the app, open a different file and open the Repeat sheet. Afterwards, choose "Off" again. -> The Repeat sheet shows the check mark on "This file".

### Audio (spec 03)

- [ ] **AUD-1** `[emulator ok]`: Press the "Audio" button during a video. -> The "Audio track" sheet lists the audio tracks. The track that plays has the check mark.
- [ ] **AUD-2** `[physical device required]`: In the MKV file, choose the other audio track. Go back and open the file again later. -> The sound changes to that track. The same track plays when the file opens again.
- [ ] **AUD-3** `[physical device required]`: Press "+50 ms" under "Audio delay" ten times. -> The sound is clearly late compared to the picture. Pressing "Reset" brings it back in sync.

### Subtitles (spec 03)

- [ ] **SUB-1** `[emulator ok]`: Make sure "Show subtitles" is on in the playback defaults. Open `ep2.mp4`. -> The subtitles of `ep2.srt` appear without any action. The Subtitles sheet shows `ep2.srt` with a check mark.
- [ ] **SUB-2** `[emulator ok]`: Watch a Hebrew line of `ep2.srt`. -> The Hebrew text reads right to left. The period or question mark is at the left end of the line.
- [ ] **SUB-3** `[emulator ok]`: Make sure "Show subtitles" is on in the playback defaults. Open `ep10.mp4`. -> The Windows-1255 subtitles show readable Hebrew, not question marks or symbols.
- [ ] **SUB-4** `[emulator ok]`: Open the video with the `.ass` file. Press Subtitles, then "Open a subtitle file...". In the system file picker, pick the `.ass` file. -> The file name appears in the list with a check mark. The subtitles render with the style of the ASS file. Hebrew reads right to left.
- [ ] **SUB-5** `[emulator ok]`: After SUB-4, close the app from the recent apps list. Open the app and open the same video. -> The same subtitle file is loaded again.
- [ ] **SUB-6** `[emulator ok]`: In the MKV file, press Subtitles and choose an embedded track. -> That track is shown and has the check mark.
- [ ] **SUB-7** `[emulator ok]`: Press "+50 ms" under "Subtitle delay" twenty times. -> Each line appears about one second later than before. Pressing "Reset" restores the timing.
- [ ] **SUB-8** `[emulator ok]`: With `ep2.srt` shown, press the decoder chip to switch between HW and SW. -> The picture restarts at the same position. The subtitles are still shown. `ep2.srt` still has the check mark.
- [ ] **SUB-9** `[emulator ok]`: Set repeat to "This file". Let `ep2.mp4` end and start again. Open the Subtitles sheet. -> `ep2.srt` appears once in the track list, not twice.

### Subtitle style in the player (spec 03)

- [ ] **STY-1** `[emulator ok]`: Press Subtitles, then "Style...". Press "Larger" twice. -> The picture restarts once and continues from the same position. The text is larger.
- [ ] **STY-2** `[emulator ok]`: Pause the video. Open the style sheet and choose another text color. -> The picture stays visible, not black. The position stays the same within one second. The subtitle color changes.
- [ ] **STY-3** `[emulator ok]`: Press "Higher" five times. Then press "Lower" until the value is 0%. -> The text moves up, then down to the bottom edge of the picture. No part of the text is cut off.
- [ ] **STY-4** `[emulator ok]`: Change the size of the subtitles. Do not press "Save as default". Open another video with subtitles. -> The other video uses the default style, not the changed one.
- [ ] **STY-5** `[emulator ok]`: Change the size of the subtitles. Press "Save as default". Open another video with subtitles. -> The other video uses the new style.
- [ ] **STY-6** `[emulator ok]`: Change several style values. Press "Back to default". -> The controls and the subtitles go back to the default style.
- [ ] **STY-7** `[physical device required]`: Over a bright video, open the Speed, Repeat, Audio, Subtitles and style sheets one after another. Close each sheet by tapping outside it. -> The text of every sheet is easy to read. Each sheet closes when you tap outside it.

## Regression: Phase 1

Run the regression with the theme "Dark OLED" and the English interface, unless an item says otherwise.

Items that mention the progress bar or the watched mark being absent no longer apply: they are covered by STATE-1 and STATE-3.

- [ ] **MED-1** `[emulator ok]`: On an Android 13 or newer phone (API 33+), uninstall the app, install it again and open it. Accept the dialogs. -> The dialogs ask for audio, and for photos and videos (not for storage). After you grant them, the folder list appears.
- [ ] **MED-2** `[emulator ok]`: On an Android 8.0 to 12L phone or emulator (API 26-32), uninstall the app, install it again and open it. Accept the dialog. -> One storage permission dialog appears. After you grant it, the folder list appears.
- [ ] **MED-3** `[emulator ok]`: Uninstall the app, install it again, open it and deny the permission. -> An explanation view appears with a "Try again" button and an "Open app settings" button. The app does not crash.
- [ ] **MED-4** `[emulator ok]`: On the explanation view from MED-3, tap "Try again". -> The permission dialog appears again. The app does not crash.
- [ ] **MED-5** `[emulator ok]`: On the explanation view from MED-3, tap "Open app settings". -> The Android settings page of this app opens.
- [ ] **MED-6** `[physical device required]`: Start a stopwatch when you tap the folder row of the 500+ file folder. Stop it when the first rows are visible. -> The rows are visible within 2 seconds.
- [ ] **MED-7** `[emulator ok]`: Keep the item list of a folder open. With a file manager or the camera app, add a video to that folder. -> The new video appears in the list within about 1 second.
- [ ] **MED-8** `[emulator ok]`: Keep the item list of a folder open. With a file manager, delete a video from that folder. -> The video disappears from the list within about 1 second.
- [ ] **MED-9** `[emulator ok]`: Move a video to the trash in the phone's Gallery or Files app (API 30+). Go back to the app and pull to refresh the item list of its folder. -> The trashed video is not listed.
- [ ] **MED-10** `[emulator ok]`: Open the folder list and look at the folder that holds both a video and an audio file. -> That folder is one row in the folder list, not two.
- [ ] **THM-1** `[emulator ok]`: Open a folder with videos. -> Each video row shows its thumbnail. Thumbnails appear as the rows come on screen.
- [ ] **THM-2** `[physical device required]`: Scroll up and down through the 500+ file folder for about 30 seconds. -> No visible stutter. Thumbnails keep appearing on the rows that come on screen.
- [ ] **THM-3** `[emulator ok]` (developer check: needs adb): Run `adb logcat -s ArielThumbs`. Start the app, open a folder with videos that you already opened on the first launch, and watch the log. -> No `decode #` lines appear for items that already had a thumbnail on the first launch.
- [ ] **THM-4** `[emulator ok]`: Open the folder with the corrupt video and scroll through it. -> The other rows still get their thumbnails. The corrupt file does not stop them, and the app does not crash or freeze.
- [ ] **THM-5** `[emulator ok]`: Clear the app cache (phone Settings, Apps, this app, Storage, Clear cache). Open the app and the folder with videos again. -> The thumbnails are created again and appear.
- [ ] **THM-6** `[emulator ok]`: Look at the row of the audio file without album art. -> The row shows no thumbnail image.
- [ ] **THM-7** `[emulator ok]`: Look at the row of the audio file with embedded album art. -> The album art is shown as the row's thumbnail.
- [ ] **EXP-1** `[emulator ok]`: On the folder list, tap a folder row. -> The item list of that folder opens.
- [ ] **EXP-2** `[emulator ok]`: In an item list, tap a video. -> The player opens and the video starts.
- [ ] **EXP-3** `[emulator ok]`: On the folder list, tap "All files". -> A flat list of all items opens, with no folder rows.
- [ ] **EXP-4** `[emulator ok]`: On the "All files" list, press Back (the Back arrow or the hardware Back button). -> The folder list is shown again.
- [ ] **EXP-5** `[emulator ok]`: Open the folder with `ep2.mp4` and `ep10.mp4` and read the order of the names. -> `ep2` is listed before `ep10`.
- [ ] **EXP-6** `[emulator ok]`: In an item list, tap the search field and type part of a file name, one letter at a time. -> The list narrows while you type. You do not press a search key.
- [ ] **EXP-7** `[emulator ok]`: In an item list, pull down from the top of the list. -> The list refreshes and shows the current files.
- [ ] **EXP-8** `[emulator ok]`: Open the app on a device or emulator that has no video or audio files. -> The folder list shows the text "No media folders found".
- [ ] **PLY-1** `[physical device required]`: Open the 1080p H.264 file. If the decoder label shows SW, tap it so that it shows HW. -> The video plays smoothly with picture and sound. The decoder label shows HW. If it shows SW, write that in the report.
- [ ] **PLY-2** `[physical device required]`: Open the 1080p H.264 file. If the decoder label shows HW, tap it so that it shows SW. -> The video plays smoothly with picture and sound. The decoder label shows SW.
- [ ] **PLY-3** `[physical device required]`: Open the 1080p HEVC file. If the decoder label shows SW, tap it so that it shows HW. -> The video plays smoothly with picture and sound. The decoder label shows HW. If it shows SW, write that in the report.
- [ ] **PLY-4** `[physical device required]`: Open the 1080p HEVC file. If the decoder label shows HW, tap it so that it shows SW. -> The video plays smoothly with picture and sound. The decoder label shows SW.
- [ ] **PLY-5** `[physical device required]`: Play the 1080p H.264 file to about 30 seconds. Tap the decoder label once. -> Playback continues at the same position (about 30 seconds) within 1 second.
- [ ] **PLY-6** `[physical device required]`: Play a video, press Home, bring the app back and press Play. Repeat this 20 times. -> After each return the picture comes back at once and is not black. The app does not crash. Pressing Play resumes playback.
- [ ] **PLY-7** `[emulator ok]`: With a video playing, make 50 rapid seeks on the seek bar (tap or drag to different points). -> The app does not crash. The picture is not black after the last seek.
- [ ] **PLY-8** `[emulator ok]`: Open a video from the item list and press Back to leave the player. Repeat this 20 times in a row. -> The app does not crash. Each opened video shows a picture, not a black screen.
- [ ] **PLY-9** `[emulator ok]`: Open an audio file from the item list. -> The audio plays, and the position in the HUD moves.
- [ ] **PLY-10** `[emulator ok]`: Open the corrupt or truncated video from the item list. -> "Cannot play" appears once, then the app returns to the item list. The app does not crash.
- [ ] **SURF-1** `[emulator ok]`: Open the 16:9 video with the phone in portrait. -> The whole picture is visible in the correct proportions, not stretched and not cropped. Black bars appear only at the edges.
- [ ] **SURF-2** `[emulator ok]`: Turn the phone to landscape while the 16:9 video plays. -> The whole picture is visible in the correct proportions, not stretched and not cropped. Black bars appear only at the edges.
- [ ] **SURF-3** `[emulator ok]`: Open the 4:3 video with the phone in portrait. -> The whole picture is visible in the correct proportions, not stretched and not cropped. Black bars appear only at the edges.
- [ ] **SURF-4** `[emulator ok]`: Turn the phone to landscape while the 4:3 video plays. -> The whole picture is visible in the correct proportions, not stretched and not cropped. Black bars appear only at the edges.
- [ ] **SURF-5** `[emulator ok]`: Open the vertical video with the phone in portrait. -> The whole picture is visible in the correct proportions, not stretched and not cropped. Black bars appear only at the edges.
- [ ] **SURF-6** `[emulator ok]`: Turn the phone to landscape while the vertical video plays. -> The whole picture is visible in the correct proportions, not stretched and not cropped. Black bars appear only at the edges.
- [ ] **HUD-1** `[emulator ok]`: While a video plays with the HUD hidden, tap the picture once. -> The HUD appears with the seek bar and the symbols (back arrow, previous, play or pause, next) and the HW/SW label.
- [ ] **HUD-2** `[emulator ok]`: With the HUD shown, tap the picture once. -> The HUD hides.
- [ ] **HUD-3** `[emulator ok]`: Start playback, show the HUD and do not touch the screen. -> The HUD hides by itself after about 3 seconds.
- [ ] **HUD-4** `[emulator ok]`: Pause the video, show the HUD and wait 10 seconds without touching the screen. -> The HUD stays visible while the video is paused.
- [ ] **HUD-5** `[emulator ok]`: Drag the seek bar to another point and release it. -> While you drag, the target time is shown. The video jumps to that time on release, not during the drag.
- [ ] **HUD-6** `[emulator ok]`: While a video plays, tap the pause symbol (two bars). -> Playback pauses and the symbol changes to a play triangle. Tapping it resumes playback and the symbol changes back to two bars.
- [ ] **HUD-7** `[emulator ok]`: In the folder with `ep2.mp4` and `ep10.mp4`, play `ep2.mp4` and tap the next symbol. -> The next file of the folder (`ep10.mp4`) starts.
- [ ] **HUD-8** `[emulator ok]`: Play the second file of the folder for more than 3 seconds and tap the previous symbol. -> The current file restarts from the beginning. The previous file does not open.
- [ ] **HUD-9** `[emulator ok]`: Play the second file of the folder and tap the previous symbol within its first 3 seconds. -> The previous file of the folder starts.
- [ ] **HUD-10** `[physical device required]`: Tap the decoder label (HW or SW) to switch between HW and SW during playback. -> The label shows HW or SW and changes with each tap. The label shows the decoder that is in use.
- [ ] **HUD-11** `[physical device required]` (developer check: needs adb): Run `adb shell setprop log.tag.ArielEngine DEBUG`, restart the app, and play a file while `adb logcat -s ArielEngine` runs. Tap the decoder label (HW or SW) to switch between HW and SW. -> When the label shows HW, the log shows `using video decoder module "mediacodec"`. When the label shows SW, the log shows `using video decoder module "avcodec"`.
- [ ] **HUD-12** `[emulator ok]`: Show the HUD and look at the clock. -> The clock shows the current time.
- [ ] **HUD-13** `[emulator ok]`: While a video plays, press the hardware Back button. -> The player closes and the item list is shown.
- [ ] **HUD-14** `[emulator ok]`: Show the HUD and look at the controls. -> Back, previous, play or pause, and next are white symbols with no colored background. The decoder label is white text in a thin white outline. No control shows a word such as "Back" or "Next".
- [ ] **HUD-15** `[emulator ok]`: Show the HUD and tap the back arrow at the top left. -> The player closes and the item list is shown.
- [ ] **COORD-1** `[emulator ok]`: Let the first file of a folder play to its end. -> The next file of the folder starts by itself.
- [ ] **COORD-2** `[emulator ok]`: Let the last file of a folder play to its end. -> Playback stops at the end. It does not restart and does not move to another file.
- [ ] **COORD-3** `[emulator ok]`: After COORD-2, tap the play symbol. -> The last file starts again from the beginning.
- [ ] **APP-1** `[emulator ok]`: Start the app from the launcher. -> The app opens and shows the folder list. Grant the permission first if it is asked (MED-1 or MED-2).
- [ ] **APP-2** `[emulator ok]` (developer check: needs the Release build and adb or aapt): Build the Release configuration (`dotnet build src\Player.App -f net10.0-android -c Release`) and list the permissions of the built APK with `aapt dump permissions <apk>`. -> `android.permission.INTERNET` and `ACCESS_NETWORK_STATE` are not listed.
- [ ] **THEME-1** `[emulator ok]`: Open the folder list and then an item list. Look at the navigation bar and its back arrow. -> The dark theme is used on every page, including the navigation bar and its back arrow.
- [ ] **APP-3** `[physical device required]`: Copy `ArielPlayer-phase3.apk` to the phone and install it over the Phase 2 app, without uninstalling it first. If the phone refuses, write down the exact message. -> The app installs as an update and opens on the folder list. Write the phone model and the Android version in the table at the top.
- [ ] **APP-4** `[emulator ok]`: Find the app in the phone's app list. -> The icon is a white play triangle on a blue background, with the name "Ariel Player". It is not the purple ".NET" icon.
- [ ] **APP-5** `[emulator ok]`: Close the app completely (swipe it away from the recent apps) and start it from the launcher. Watch the first second. -> A black screen with the app symbol (a white play triangle in a blue circle) appears, then the folder list. No purple screen appears.
- [ ] **THEME-2** `[emulator ok]`: Open the folder list and an item list. Look at the Android status bar at the top (clock, battery, signal, notifications). -> The status bar has the same dark color as the app's top bar, with white icons. It is not purple.
- [ ] **THEME-3** `[emulator ok]`: Open an item list and tap the search field. -> The line under the search field and the cursor are blue, not purple.

## Report back

Write the results in the table at the top of this file and send:

- Phone model and Android version: ____
- Number of failed items: ____
- Failed item IDs: ____
- What was seen instead (for each failed item): ____
- Items not run, and why: ____
