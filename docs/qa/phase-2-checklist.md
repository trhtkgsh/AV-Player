# Phase 2 device checklist

- Phase: 2 (per-file playback state, playback defaults, interface settings, speed, repeat, A-B repeat, audio and subtitles)
- Date: 2026-11-15
- APK to install on a phone: `artifacts\ArielPlayer-phase2.apk` (copy it to the phone and open it, or `adb install -r artifacts\ArielPlayer-phase2.apk`)
- Developer build to an emulator or a phone with USB debugging: `dotnet build src\Player.App -f net10.0-android -t:Install`

## Phone under test

| Phone model | Android version | Result (PASS / FAIL, and the item IDs that failed) |
|---|---|---|
|  |  |  |
|  |  |  |

## How to read this list

- Each item has one action, the expected result and its source.
- `[emulator ok]`: can be run on an emulator.
- `[physical device required]`: must be run on a phone.
- `[emulator ok, verified 2026-11-15]`: the orchestrator already checked the item on an emulator (Pixel 5 AVD, API 34, x86_64; the Release APK was also installed over the Phase 1 APK there, and its x86_64 half was run on a Pixel 7 AVD, API 36). It still needs its normal run on the phone when the normal marker says physical.
- Phase 2 behaviour (temporary): when the app goes to the background, playback still pauses. Background playback comes in Phase 4.
- These settings exist but are hidden until their phase: aspect mode, skip length, battery indicator (Phase 3), background audio, Picture-in-Picture (Phase 4). The theme choice comes in Phase 3. Skip length is tested in Phase 3.
- Android does not show `.ass` and `.ssa` files to the app on its own. They do not appear under "Files next to the video". Load them with "Open a subtitle file...".
- A subtitle style change or a decoder switch restarts the picture. While paused, the position can move forward by up to half a second.
- Where to find things: the Settings entry is in the three-dot menu of the file list. In the player, the top bar has the Subtitles, Audio and Repeat buttons. The speed chip (for example "1.0x") is at the bottom left. The decoder chip (HW or SW) is at the bottom right.

## Setup

Put these files on the phone (internal storage, for example `Movies/`).

1. A folder with two short videos `ep2.mp4` and `ep10.mp4` (used by STATE-3, STATE-4, STATE-5, DEF-1, DEF-2, DEF-3, DEF-4, DEF-5, UI-3, UI-4, REP-1, REP-2, REP-3, REP-7, SUB-1, SUB-3, SUB-9, STY-4, STY-5).
2. `ep2.srt` next to `ep2.mp4`, saved as UTF-8, with Hebrew lines that end with a period or a question mark, and one English line (used by STATE-6, DEF-5, DEF-8, SUB-1, SUB-2, SUB-7, SUB-8, SUB-9, STY-1, STY-2, STY-3, STY-4, STY-5, STY-6).
3. `ep10.he.srt` next to `ep10.mp4`, saved in the Windows-1255 encoding, with Hebrew lines (used by SUB-3, STY-4, STY-5).
4. A video with an `.ass` subtitle file that has Hebrew text (any folder) (used by SUB-4, SUB-5).
5. An MKV video with two audio tracks in different languages and at least one embedded subtitle track (used by AUD-1, AUD-2, DEF-6, DEF-7, SUB-6).
6. A video longer than 2 minutes with speech or music (used by STATE-1, STATE-2, UI-1, SPD-1, SPD-2, SPD-3, SPD-4, REP-4, REP-5, REP-6, AUD-3, STY-7).
7. Accessories: wired or Bluetooth headphones are not needed. The phone speaker is enough for the pitch and delay items (used by SPD-3, SPD-4, AUD-3).

## Run these first (real phone only)

These two items check a problem that was seen only on an emulator that translates ARM code. Run them before the rest. If one fails, stop and report it.

- [ ] **ARM-1** `[physical device required]`
  - Action: Open a video and pause it. Press the speed chip and close the "Speed" sheet by tapping outside it. Repeat this 10 times. Then open the Subtitles sheet and press "Style...".
  - Expected: Every sheet opens and closes. The app never shows "isn't responding".
  - Source: (agent report: orchestrator, emulator run 2026-11-15, API 36 arm64 translation)
- [ ] **ARM-2** `[physical device required]`
  - Action: With the video still paused, press "Larger" twice in the style sheet and close it. Press the decoder chip twice (HW to SW and back). Then press Play.
  - Expected: The picture restarts each time at the same position and the app does not close. Playback continues.
  - Source: (agent report: orchestrator, emulator run 2026-11-15, API 36 arm64 translation)

## Playback state (spec 04)

- [ ] **STATE-1** `[emulator ok]` `[emulator ok, verified 2026-11-15]`
  - Action: Play a video for about 20 seconds. Press Back to go to the file list.
  - Expected: The row of that file shows a progress bar. Its NEW tag is gone.
  - Source: (spec 04, requirements; spec 01, progress bar)
- [ ] **STATE-2** `[physical device required]` `[emulator ok, verified 2026-11-15]`
  - Action: Play a video for 30 seconds. In the system settings, open Apps, Ariel Player, and press Force stop. Open the app and open the same file.
  - Expected: Playback resumes within 5 seconds of the last position.
  - Source: (spec 04, acceptance 3)
- [ ] **STATE-3** `[emulator ok]` `[emulator ok, verified 2026-11-15]`
  - Action: Play a short video to its end. Go back to the file list. Open the file again.
  - Expected: The row shows "Watched". The file starts from the beginning.
  - Source: (spec 04, finished rule; spec 05, resolver)
- [ ] **STATE-4** `[emulator ok]` `[emulator ok, verified 2026-11-15]`
  - Action: In file A, set the speed to 1.5x. Go back. Open file B. Go back. Open file A again.
  - Expected: File B plays at 1.0x. File A plays at 1.5x.
  - Source: (spec 04, per-file overrides; spec 05, acceptance 3)
- [ ] **STATE-5** `[emulator ok]` `[emulator ok, verified 2026-11-15]`
  - Action: In file A, switch the decoder chip to SW. Go back and open file A again. Then open another file.
  - Expected: The chip shows SW for file A. The other file shows HW.
  - Source: (spec 04, per-file overrides)
- [ ] **STATE-6** `[emulator ok]`
  - Action: In a video with subtitles, choose "Off" in the Subtitles sheet. Go back and open the file again.
  - Expected: No subtitles appear. The Subtitles sheet shows the check mark on "Off".
  - Source: (spec 04, per-file overrides)

## Playback defaults (spec 05)

- [ ] **DEF-1** `[emulator ok]`
  - Action: Open Settings, then "Playback defaults". Raise "Speed" to 1.25x. Open a file that was never played.
  - Expected: The file plays at 1.25x. The speed chip shows 1.25x.
  - Source: (spec 05, acceptance 3)
- [ ] **DEF-2** `[emulator ok]`
  - Action: With the default speed at 1.25x, open the file that got 1.5x in STATE-4.
  - Expected: The file plays at 1.5x.
  - Source: (spec 05, acceptance 3)
- [ ] **DEF-3** `[emulator ok]`
  - Action: In "Playback defaults", set "Decoder" to "Software". Open a file that was never played.
  - Expected: The decoder chip shows SW.
  - Source: (spec 05, fields)
- [ ] **DEF-4** `[emulator ok]`
  - Action: Turn off "Resume from the last position". Open a file that has a progress bar.
  - Expected: The file starts from the beginning.
  - Source: (spec 05, resolver)
- [ ] **DEF-5** `[emulator ok]`
  - Action: Turn off "Show subtitles". Open `ep2.mp4`.
  - Expected: No subtitles appear. The Subtitles sheet still lists `ep2.srt` under "Files next to the video".
  - Source: (spec 05, fields; spec 03, subtitles)
- [ ] **DEF-6** `[emulator ok]`
  - Action: Type the language code (for example `he` or `en`) of the second audio track of the MKV file into "Preferred audio language". Open that MKV file for the first time.
  - Expected: The second audio track plays. The Audio sheet shows the check mark on it.
  - Source: (spec 05, fields; spec 03, preferred language)
- [ ] **DEF-7** `[emulator ok]`
  - Action: Type the language code (for example `he` or `en`) of an embedded subtitle track of the MKV file into "Preferred subtitle language". Turn on "Show subtitles". Open the MKV file for the first time.
  - Expected: That subtitle track is shown.
  - Source: (spec 05, fields; spec 03, preferred language)
- [ ] **DEF-8** `[emulator ok]` `[emulator ok, verified 2026-11-15]`
  - Action: In "Playback defaults", open "Subtitle style". Press "Larger" several times and choose the yellow text color. Open `ep2.mp4`.
  - Expected: The subtitles are larger and yellow.
  - Source: (spec 05, fields; spec 03, subtitle style)
- [ ] **DEF-9** `[emulator ok]`
  - Action: Change several defaults. Close the app from the recent apps list and open it again. Open "Playback defaults".
  - Expected: The page shows the changed values.
  - Source: (spec 05, persistence)
- [ ] **DEF-10** `[emulator ok]`
  - Action: Press "Reset all to defaults" and confirm.
  - Expected: Speed is 1.0x. Decoder is "Hardware". Repeat is "Off". "Resume from the last position" is on. Delays are 0 ms. "Show subtitles" is on.
  - Source: (spec 05, defaults)

## Interface settings (spec 06)

- [ ] **UI-1** `[emulator ok]`
  - Action: Open Settings, then "Interface". Change "Hide the controls after" to a different value. Play a video and do not touch the screen.
  - Expected: The controls hide after the chosen time.
  - Source: (spec 06, acceptance 2)
- [ ] **UI-2** `[emulator ok]`
  - Action: Turn off "Show the clock". Open a video.
  - Expected: The clock in the top bar of the player is gone.
  - Source: (spec 06, fields)
- [ ] **UI-3** `[emulator ok]`
  - Action: Under "Details shown for each file", turn off "Duration" and turn on "Folder path". Go back to a file list.
  - Expected: The rows show no duration. The rows show the folder path.
  - Source: (spec 06, acceptance 2)
- [ ] **UI-4** `[emulator ok]`
  - Action: Change "Mark files as new for" to the shortest value. Go back to the file list.
  - Expected: Only files that were added to the phone within that time and were never played have the NEW tag. Older files have no NEW tag.
  - Source: (spec 06, fields)
- [ ] **UI-5** `[emulator ok]` `[emulator ok, verified 2026-11-15]`
  - Action: In a file list, press "Sort".
  - Expected: The dialog is dark like the app. The selected option is clearly visible.
  - Source: (agent report: theming-engine, P2-21)

## Speed (spec 03)

- [ ] **SPD-1** `[emulator ok]` `[emulator ok, verified 2026-11-15]`
  - Action: During playback, press the speed chip.
  - Expected: The "Speed" sheet opens at the bottom. The picture keeps playing behind it. The player controls are hidden while the sheet is open.
  - Source: (spec 03, speed; agent report: playback-settings-ui)
- [ ] **SPD-2** `[emulator ok]`
  - Action: Press "1.5x". Then tap outside the sheet.
  - Expected: The sheet closes. Playback is faster. The speed chip shows 1.5x.
  - Source: (spec 03, speed)
- [ ] **SPD-3** `[physical device required]`
  - Action: Set the speed to 0.25x. Listen to speech.
  - Expected: The speech is slow, with the normal pitch. The voice is not deeper.
  - Source: (spec 03, acceptance 2)
- [ ] **SPD-4** `[physical device required]`
  - Action: Set the speed to 4.0x. Listen to speech.
  - Expected: The speech is fast, with the normal pitch. The voice is not higher.
  - Source: (spec 03, acceptance 2)

## Repeat and A-B repeat (spec 03)

- [ ] **REP-1** `[emulator ok]` `[emulator ok, verified 2026-11-15]`
  - Action: Press the "Repeat" button. Choose "This file". Let the file end.
  - Expected: The same file starts again from the beginning.
  - Source: (spec 03, repeat modes)
- [ ] **REP-2** `[emulator ok]`
  - Action: Press "Repeat" and choose "Folder". Let the last file of the folder end.
  - Expected: The first file of the folder starts.
  - Source: (spec 03, repeat modes)
- [ ] **REP-3** `[emulator ok]` `[emulator ok, verified 2026-11-15]`
  - Action: Press "Repeat" and choose "Off". Let a file that is not the last one end.
  - Expected: The next file starts. After the last file, playback stops.
  - Source: (spec 03, repeat modes)
- [ ] **REP-4** `[emulator ok]`
  - Action: At about 0:05 press "Set A". At about 0:15 press "Set B".
  - Expected: The sheet shows the A-B range. Playback jumps back to A each time it reaches B.
  - Source: (spec 03, A-B repeat)
- [ ] **REP-5** `[emulator ok]`
  - Action: Press "Clear".
  - Expected: The range shows "Not set". Playback continues past B.
  - Source: (spec 03, A-B repeat)
- [ ] **REP-6** `[emulator ok]`
  - Action: Press "Set A". Seek to an earlier position and press "Set B".
  - Expected: A message says "B must be at least 1 second after A." No loop is set.
  - Source: (spec 03, A-B validation)
- [ ] **REP-7** `[emulator ok]`
  - Action: Press "Repeat" and choose "This file". Close the app from the recent apps list. Open the app, open a different file and open the Repeat sheet. Afterwards, choose "Off" again.
  - Expected: The Repeat sheet shows the check mark on "This file".
  - Source: (spec 03, req 3; agent report: playback-coordinator)

## Audio (spec 03)

- [ ] **AUD-1** `[emulator ok]` `[emulator ok, verified 2026-11-15]`
  - Action: Press the "Audio" button during a video.
  - Expected: The "Audio track" sheet lists the audio tracks. The track that plays has the check mark.
  - Source: (spec 03, audio tracks)
- [ ] **AUD-2** `[physical device required]`
  - Action: In the MKV file, choose the other audio track. Go back and open the file again later.
  - Expected: The sound changes to that track. The same track plays when the file opens again.
  - Source: (spec 03, audio tracks; spec 04, per-file overrides)
- [ ] **AUD-3** `[physical device required]`
  - Action: Press "+50 ms" under "Audio delay" ten times.
  - Expected: The sound is clearly late compared to the picture. Pressing "Reset" brings it back in sync.
  - Source: (spec 03, acceptance 2)

## Subtitles (spec 03)

- [ ] **SUB-1** `[emulator ok]` `[emulator ok, verified 2026-11-15]`
  - Action: Make sure "Show subtitles" is on in the playback defaults. Open `ep2.mp4`.
  - Expected: The subtitles of `ep2.srt` appear without any action. The Subtitles sheet shows `ep2.srt` with a check mark.
  - Source: (spec 03, sidecar; spec 03, acceptance 2)
- [ ] **SUB-2** `[emulator ok]` `[emulator ok, verified 2026-11-15]`
  - Action: Watch a Hebrew line of `ep2.srt`.
  - Expected: The Hebrew text reads right to left. The period or question mark is at the left end of the line.
  - Source: (spec 03, acceptance 2)
- [ ] **SUB-3** `[emulator ok]` `[emulator ok, verified 2026-11-15]`
  - Action: Make sure "Show subtitles" is on in the playback defaults. Open `ep10.mp4`.
  - Expected: The Windows-1255 subtitles show readable Hebrew, not question marks or symbols.
  - Source: (spec 03, acceptance 2; agent report: subtitles-audio)
- [ ] **SUB-4** `[emulator ok]` `[emulator ok, verified 2026-11-15]`
  - Action: Open the video with the `.ass` file. Press Subtitles, then "Open a subtitle file...". In the system file picker, pick the `.ass` file.
  - Expected: The file name appears in the list with a check mark. The subtitles render with the style of the ASS file. Hebrew reads right to left.
  - Source: (spec 03, acceptance 2)
- [ ] **SUB-5** `[emulator ok]`
  - Action: After SUB-4, close the app from the recent apps list. Open the app and open the same video.
  - Expected: The same subtitle file is loaded again.
  - Source: (spec 04, per-file overrides; agent report: subtitles-audio)
- [ ] **SUB-6** `[emulator ok]`
  - Action: In the MKV file, press Subtitles and choose an embedded track.
  - Expected: That track is shown and has the check mark.
  - Source: (spec 03, subtitle tracks)
- [ ] **SUB-7** `[emulator ok]`
  - Action: Press "+50 ms" under "Subtitle delay" twenty times.
  - Expected: Each line appears about one second later than before. Pressing "Reset" restores the timing.
  - Source: (spec 03, acceptance 2)
- [ ] **SUB-8** `[emulator ok]` `[emulator ok, verified 2026-11-15]`
  - Action: With `ep2.srt` shown, press the decoder chip to switch between HW and SW.
  - Expected: The picture restarts at the same position. The subtitles are still shown. `ep2.srt` still has the check mark.
  - Source: (agent report: player-engine, P2-30)
- [ ] **SUB-9** `[emulator ok]` `[emulator ok, verified 2026-11-15]`
  - Action: Set repeat to "This file". Let `ep2.mp4` end and start again. Open the Subtitles sheet.
  - Expected: `ep2.srt` appears once in the track list, not twice.
  - Source: (agent report: player-engine, P2-30)

## Subtitle style in the player (spec 03)

- [ ] **STY-1** `[emulator ok]` `[emulator ok, verified 2026-11-15]`
  - Action: Press Subtitles, then "Style...". Press "Larger" twice.
  - Expected: The picture restarts once and continues from the same position. The text is larger.
  - Source: (spec 03, subtitle style; agent report: player-engine, P2-27)
- [ ] **STY-2** `[emulator ok]` `[emulator ok, verified 2026-11-15]`
  - Action: Pause the video. Open the style sheet and choose another text color.
  - Expected: The picture stays visible, not black. The position stays the same within one second. The subtitle color changes.
  - Source: (agent report: player-engine, P2-33)
- [ ] **STY-3** `[emulator ok]` `[emulator ok, verified 2026-11-15]`
  - Action: Press "Higher" five times. Then press "Lower" until the value is 0%.
  - Expected: The text moves up, then down to the bottom edge of the picture. No part of the text is cut off.
  - Source: (spec 03, subtitle style)
- [ ] **STY-4** `[emulator ok]` `[emulator ok, verified 2026-11-15]`
  - Action: Change the size of the subtitles. Do not press "Save as default". Open another video with subtitles.
  - Expected: The other video uses the default style, not the changed one.
  - Source: (spec 05, scope of the defaults; agent report: playback-settings-ui)
- [ ] **STY-5** `[emulator ok]`
  - Action: Change the size of the subtitles. Press "Save as default". Open another video with subtitles.
  - Expected: The other video uses the new style.
  - Source: (spec 05, scope of the defaults)
- [ ] **STY-6** `[emulator ok]`
  - Action: Change several style values. Press "Back to default".
  - Expected: The controls and the subtitles go back to the default style.
  - Source: (agent report: playback-settings-ui)
- [ ] **STY-7** `[physical device required]`
  - Action: Over a bright video, open the Speed, Repeat, Audio, Subtitles and style sheets one after another. Close each sheet by tapping outside it.
  - Expected: The text of every sheet is easy to read. Each sheet closes when you tap outside it.
  - Source: (agent report: playback-settings-ui, P2-32)

## Regression: Phase 1

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
- [ ] **APP-3** `[physical device required]`: Copy `ArielPlayer-phase2.apk` to the phone and install it over the Phase 1 app, without uninstalling it first. If the phone refuses, write down the exact message. -> The app installs as an update and opens on the folder list. Write the phone model and the Android version in the table at the top.
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
- For ARM-1 and ARM-2, also write how many times you opened a sheet before the problem appeared.
