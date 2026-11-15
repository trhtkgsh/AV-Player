# Phase 1 device checklist

- Phase: 1 (file explorer, player and HUD basics, app shell, theme)
- Date: 2026-11-14, updated 2026-11-15
- APK to install on a phone: `artifacts\ArielPlayer-phase1-r2.apk` (copy it to the phone and open it, or `adb install -r artifacts\ArielPlayer-phase1-r2.apk`)
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
- `[emulator ok, verified 2026-11-14]`: the orchestrator already checked this on an emulator (Pixel 5 AVD, API 34). It still needs its normal run on the phone when the normal marker says physical.
- `[emulator ok, verified 2026-11-15]`: the orchestrator checked the item on emulators with API 34 and API 36.
- Phase 1 behaviour (temporary): when the app goes to the background, playback pauses. When the app comes back, the picture is restored and you press Play.
- Progress bar and "watched" mark are not in this checklist: they need a saved playback position, which comes in Phase 2.

## Setup

Put these files on the phone (internal storage, for example `Movies/` and `Music/`). Use an emulator or a spare phone where a step needs a device with no media or an older Android version.

1. A 1080p H.264 video, 16:9 (used by PLY-1, PLY-2, PLY-5, PLY-6, PLY-7, HUD-10, HUD-11).
2. A 1080p HEVC (H.265) video (used by PLY-3, PLY-4).
3. A 4:3 video (used by SURF-3, SURF-4).
4. A vertical video, 9:16 (used by SURF-5, SURF-6).
5. A folder with at least two short videos named `ep2.mp4` and `ep10.mp4` (used by EXP-5, HUD-7 to HUD-9, COORD-1, COORD-2, COORD-3).
6. Two audio files: one without album art, one with embedded album art (used by THM-6, THM-7, PLY-9).
7. A folder that contains at least one video and one audio file (used by MED-10).
8. A corrupt or truncated video file, for example a video cut in half, in a folder with other videos (used by THM-4, PLY-10).
9. A folder with 500+ files, video and audio mixed (used by MED-6, THM-2).
10. A device or emulator with no video or audio files at all (used by EXP-8).
11. A phone or emulator running Android 8.0 to 12L (API 26-32) (used by MED-2).
12. A video you can move to the trash in the phone's Gallery or Files app on API 30+ (used by MED-9).
13. A computer with the Android SDK (`adb`, `aapt`) and the Release build (used by APP-2, THM-3, HUD-11).

Accessories: none needed in Phase 1 (no wired or Bluetooth headset).

## File explorer (spec 01)

### Permission and library

- [ ] **MED-1** `[emulator ok]` `[emulator ok, verified 2026-11-14]`
  - Action: On an Android 13 or newer phone (API 33+), uninstall the app, install it again and open it. Accept the dialogs.
  - Expected: The dialogs ask for audio, and for photos and videos (not for storage). After you grant them, the folder list appears.
  - Source: (agent report: media-library; spec 01, req 2)
- [ ] **MED-2** `[emulator ok]`
  - Action: On an Android 8.0 to 12L phone or emulator (API 26-32), uninstall the app, install it again and open it. Accept the dialog.
  - Expected: One storage permission dialog appears. After you grant it, the folder list appears.
  - Source: (agent report: media-library; spec 01, req 2)
- [ ] **MED-3** `[emulator ok]`
  - Action: Uninstall the app, install it again, open it and deny the permission.
  - Expected: An explanation view appears with a "Try again" button and an "Open app settings" button. The app does not crash.
  - Source: (agent report: media-library; spec 01, req 2)
- [ ] **MED-4** `[emulator ok]`
  - Action: On the explanation view from MED-3, tap "Try again".
  - Expected: The permission dialog appears again. The app does not crash.
  - Source: (spec 01, req 2)
- [ ] **MED-5** `[emulator ok]`
  - Action: On the explanation view from MED-3, tap "Open app settings".
  - Expected: The Android settings page of this app opens.
  - Source: (spec 01, req 2)
- [ ] **MED-6** `[physical device required]`
  - Action: Start a stopwatch when you tap the folder row of the 500+ file folder. Stop it when the first rows are visible.
  - Expected: The rows are visible within 2 seconds.
  - Source: (agent report: media-library)
- [ ] **MED-7** `[emulator ok]`
  - Action: Keep the item list of a folder open. With a file manager or the camera app, add a video to that folder.
  - Expected: The new video appears in the list within about 1 second.
  - Source: (agent report: media-library; spec 01, req 11)
- [ ] **MED-8** `[emulator ok]`
  - Action: Keep the item list of a folder open. With a file manager, delete a video from that folder.
  - Expected: The video disappears from the list within about 1 second.
  - Source: (agent report: media-library; spec 01, req 11)
- [ ] **MED-9** `[emulator ok]`
  - Action: Move a video to the trash in the phone's Gallery or Files app (API 30+). Go back to the app and pull to refresh the item list of its folder.
  - Expected: The trashed video is not listed.
  - Source: (agent report: media-library)
- [ ] **MED-10** `[emulator ok]`
  - Action: Open the folder list and look at the folder that holds both a video and an audio file.
  - Expected: That folder is one row in the folder list, not two.
  - Source: (agent report: media-library)

### Thumbnails

- [ ] **THM-1** `[emulator ok]` `[emulator ok, verified 2026-11-14]`
  - Action: Open a folder with videos.
  - Expected: Each video row shows its thumbnail. Thumbnails appear as the rows come on screen.
  - Source: (spec 01, acceptance 2; agent report: thumbnail-cache)
- [ ] **THM-2** `[physical device required]`
  - Action: Scroll up and down through the 500+ file folder for about 30 seconds.
  - Expected: No visible stutter. Thumbnails keep appearing on the rows that come on screen.
  - Source: (spec 01, acceptance 2; agent report: thumbnail-cache)
- [ ] **THM-3** `[emulator ok]` (developer check: needs adb)
  - Action: Run `adb logcat -s ArielThumbs`. Start the app, open a folder with videos that you already opened on the first launch, and watch the log.
  - Expected: No `decode #` lines appear for items that already had a thumbnail on the first launch.
  - Source: (spec 01, acceptance 2; agent report: thumbnail-cache)
- [ ] **THM-4** `[emulator ok]`
  - Action: Open the folder with the corrupt video and scroll through it.
  - Expected: The other rows still get their thumbnails. The corrupt file does not stop them, and the app does not crash or freeze.
  - Source: (agent report: thumbnail-cache)
- [ ] **THM-5** `[emulator ok]`
  - Action: Clear the app cache (phone Settings, Apps, this app, Storage, Clear cache). Open the app and the folder with videos again.
  - Expected: The thumbnails are created again and appear.
  - Source: (agent report: thumbnail-cache)
- [ ] **THM-6** `[emulator ok]`
  - Action: Look at the row of the audio file without album art.
  - Expected: The row shows no thumbnail image.
  - Source: (agent report: thumbnail-cache)
- [ ] **THM-7** `[emulator ok]`
  - Action: Look at the row of the audio file with embedded album art.
  - Expected: The album art is shown as the row's thumbnail.
  - Source: (agent report: thumbnail-cache)

### Explorer

- [ ] **EXP-1** `[emulator ok]` `[emulator ok, verified 2026-11-14]`
  - Action: On the folder list, tap a folder row.
  - Expected: The item list of that folder opens.
  - Source: (agent report: explorer-ui; spec 01, req 3)
- [ ] **EXP-2** `[emulator ok]` `[emulator ok, verified 2026-11-14]`
  - Action: In an item list, tap a video.
  - Expected: The player opens and the video starts.
  - Source: (agent report: explorer-ui; spec 01, req 10)
- [ ] **EXP-3** `[emulator ok]`
  - Action: On the folder list, tap "All files".
  - Expected: A flat list of all items opens, with no folder rows.
  - Source: (agent report: explorer-ui; spec 01, req 3)
- [ ] **EXP-4** `[emulator ok]`
  - Action: On the "All files" list, press Back (the Back arrow or the hardware Back button).
  - Expected: The folder list is shown again.
  - Source: (agent report: explorer-ui)
- [ ] **EXP-5** `[emulator ok]` `[emulator ok, verified 2026-11-14]`
  - Action: Open the folder with `ep2.mp4` and `ep10.mp4` and read the order of the names.
  - Expected: `ep2` is listed before `ep10`.
  - Source: (agent report: explorer-ui; spec 01, req 8)
- [ ] **EXP-6** `[emulator ok]`
  - Action: In an item list, tap the search field and type part of a file name, one letter at a time.
  - Expected: The list narrows while you type. You do not press a search key.
  - Source: (agent report: explorer-ui; spec 01, req 9)
- [ ] **EXP-7** `[emulator ok]`
  - Action: In an item list, pull down from the top of the list.
  - Expected: The list refreshes and shows the current files.
  - Source: (agent report: explorer-ui; spec 01, req 11)
- [ ] **EXP-8** `[emulator ok]`
  - Action: Open the app on a device or emulator that has no video or audio files.
  - Expected: The folder list shows the text "No media folders found".
  - Source: (agent report: explorer-ui)

#### Not testable by hand

- Pending files (media that another app is still writing) are not hidden by a reliable manual procedure. They are covered by code review.

## Player and HUD (spec 02)

### Playback

- [ ] **PLY-1** `[physical device required]`
  - Action: Open the 1080p H.264 file. If the decoder label shows SW, tap it so that it shows HW.
  - Expected: The video plays smoothly with picture and sound. The decoder label shows HW. If it shows SW, write that in the report.
  - Source: (spec 02, acceptance 2; spec 02, req 2; agent report: player-engine)
- [ ] **PLY-2** `[physical device required]`
  - Action: Open the 1080p H.264 file. If the decoder label shows HW, tap it so that it shows SW.
  - Expected: The video plays smoothly with picture and sound. The decoder label shows SW.
  - Source: (spec 02, acceptance 2; spec 02, req 2; agent report: player-engine)
- [ ] **PLY-3** `[physical device required]`
  - Action: Open the 1080p HEVC file. If the decoder label shows SW, tap it so that it shows HW.
  - Expected: The video plays smoothly with picture and sound. The decoder label shows HW. If it shows SW, write that in the report.
  - Source: (spec 02, acceptance 2; spec 02, req 2; agent report: player-engine)
- [ ] **PLY-4** `[physical device required]`
  - Action: Open the 1080p HEVC file. If the decoder label shows HW, tap it so that it shows SW.
  - Expected: The video plays smoothly with picture and sound. The decoder label shows SW.
  - Source: (spec 02, acceptance 2; spec 02, req 2; agent report: player-engine)
- [ ] **PLY-5** `[physical device required]` `[emulator ok, verified 2026-11-14]`
  - Action: Play the 1080p H.264 file to about 30 seconds. Tap the decoder label once.
  - Expected: Playback continues at the same position (about 30 seconds) within 1 second.
  - Source: (spec 02, acceptance 2; spec 02, req 2; agent report: player-engine)
- [ ] **PLY-6** `[physical device required]` `[emulator ok, verified 2026-11-14]`
  - Action: Play a video, press Home, bring the app back and press Play. Repeat this 20 times.
  - Expected: After each return the picture comes back at once and is not black. The app does not crash. Pressing Play resumes playback.
  - Source: (spec 02, acceptance 4; spec 02, req 7; agent report: player-engine)
  - Note: The emulator check covered 6 cycles.
- [ ] **PLY-7** `[emulator ok]`
  - Action: With a video playing, make 50 rapid seeks on the seek bar (tap or drag to different points).
  - Expected: The app does not crash. The picture is not black after the last seek.
  - Source: (agent report: player-engine)
- [ ] **PLY-8** `[emulator ok]`
  - Action: Open a video from the item list and press Back to leave the player. Repeat this 20 times in a row.
  - Expected: The app does not crash. Each opened video shows a picture, not a black screen.
  - Source: (agent report: player-engine)
- [ ] **PLY-9** `[emulator ok]` `[emulator ok, verified 2026-11-14]`
  - Action: Open an audio file from the item list.
  - Expected: The audio plays, and the position in the HUD moves.
  - Source: (spec 02, req 1; spec 01, req 1)
- [ ] **PLY-10** `[emulator ok]`
  - Action: Open the corrupt or truncated video from the item list.
  - Expected: "Cannot play" appears once, then the app returns to the item list. The app does not crash.
  - Source: (spec 02, req 9; agent report: player-hud-ui)

### Picture (Fit only in Phase 1)

- [ ] **SURF-1** `[emulator ok]`
  - Action: Open the 16:9 video with the phone in portrait.
  - Expected: The whole picture is visible in the correct proportions, not stretched and not cropped. Black bars appear only at the edges.
  - Source: (agent report: video-surface; spec 02, req 6)
- [ ] **SURF-2** `[emulator ok]`
  - Action: Turn the phone to landscape while the 16:9 video plays.
  - Expected: The whole picture is visible in the correct proportions, not stretched and not cropped. Black bars appear only at the edges.
  - Source: (agent report: video-surface; spec 02, req 6)
- [ ] **SURF-3** `[emulator ok]`
  - Action: Open the 4:3 video with the phone in portrait.
  - Expected: The whole picture is visible in the correct proportions, not stretched and not cropped. Black bars appear only at the edges.
  - Source: (agent report: video-surface; spec 02, req 6)
- [ ] **SURF-4** `[emulator ok]`
  - Action: Turn the phone to landscape while the 4:3 video plays.
  - Expected: The whole picture is visible in the correct proportions, not stretched and not cropped. Black bars appear only at the edges.
  - Source: (agent report: video-surface; spec 02, req 6)
- [ ] **SURF-5** `[emulator ok]` `[emulator ok, verified 2026-11-14]`
  - Action: Open the vertical video with the phone in portrait.
  - Expected: The whole picture is visible in the correct proportions, not stretched and not cropped. Black bars appear only at the edges.
  - Source: (agent report: video-surface; spec 02, req 6)
- [ ] **SURF-6** `[emulator ok]`
  - Action: Turn the phone to landscape while the vertical video plays.
  - Expected: The whole picture is visible in the correct proportions, not stretched and not cropped. Black bars appear only at the edges.
  - Source: (agent report: video-surface; spec 02, req 6)

### HUD

- [ ] **HUD-1** `[emulator ok]` `[emulator ok, verified 2026-11-14]`
  - Action: While a video plays with the HUD hidden, tap the picture once.
  - Expected: The HUD appears with the seek bar and the symbols (back arrow, previous, play or pause, next) and the HW/SW label.
  - Source: (spec 02, req 3; agent report: player-hud-ui)
- [ ] **HUD-2** `[emulator ok]` `[emulator ok, verified 2026-11-14]`
  - Action: With the HUD shown, tap the picture once.
  - Expected: The HUD hides.
  - Source: (spec 02, req 3; agent report: player-hud-ui)
- [ ] **HUD-3** `[emulator ok]` `[emulator ok, verified 2026-11-14]`
  - Action: Start playback, show the HUD and do not touch the screen.
  - Expected: The HUD hides by itself after about 3 seconds.
  - Source: (spec 02, req 3; agent report: player-hud-ui)
- [ ] **HUD-4** `[emulator ok]`
  - Action: Pause the video, show the HUD and wait 10 seconds without touching the screen.
  - Expected: The HUD stays visible while the video is paused.
  - Source: (agent report: player-hud-ui)
- [ ] **HUD-5** `[emulator ok]` `[emulator ok, verified 2026-11-14]`
  - Action: Drag the seek bar to another point and release it.
  - Expected: While you drag, the target time is shown. The video jumps to that time on release, not during the drag.
  - Source: (spec 02, req 3; agent report: player-hud-ui)
- [ ] **HUD-6** `[emulator ok]` `[emulator ok, verified 2026-11-14]` `[emulator ok, verified 2026-11-15]`
  - Action: While a video plays, tap the pause symbol (two bars).
  - Expected: Playback pauses and the symbol changes to a play triangle. Tapping it resumes playback and the symbol changes back to two bars.
  - Source: (spec 02, req 3; agent report: player-hud-ui)
- [ ] **HUD-7** `[emulator ok]`
  - Action: In the folder with `ep2.mp4` and `ep10.mp4`, play `ep2.mp4` and tap the next symbol.
  - Expected: The next file of the folder (`ep10.mp4`) starts.
  - Source: (agent report: player-hud-ui; agent report: playback-coordinator)
- [ ] **HUD-8** `[emulator ok]`
  - Action: Play the second file of the folder for more than 3 seconds and tap the previous symbol.
  - Expected: The current file restarts from the beginning. The previous file does not open.
  - Source: (agent report: playback-coordinator)
- [ ] **HUD-9** `[emulator ok]`
  - Action: Play the second file of the folder and tap the previous symbol within its first 3 seconds.
  - Expected: The previous file of the folder starts.
  - Source: (agent report: playback-coordinator)
- [ ] **HUD-10** `[physical device required]` `[emulator ok, verified 2026-11-14]`
  - Action: Tap the decoder label (HW or SW) to switch between HW and SW during playback.
  - Expected: The label shows HW or SW and changes with each tap. The label shows the decoder that is in use.
  - Source: (spec 02, req 2; agent report: player-hud-ui)
- [ ] **HUD-11** `[physical device required]` (developer check: needs adb)
  - Action: Run `adb shell setprop log.tag.ArielEngine DEBUG`, restart the app, and play a file while `adb logcat -s ArielEngine` runs. Tap the decoder label (HW or SW) to switch between HW and SW.
  - Expected: When the label shows HW, the log shows `using video decoder module "mediacodec"`. When the label shows SW, the log shows `using video decoder module "avcodec"`.
  - Source: (spec 02, req 2; agent report: player-engine, step 4)
- [ ] **HUD-12** `[emulator ok]`
  - Action: Show the HUD and look at the clock.
  - Expected: The clock shows the current time.
  - Source: (agent report: player-hud-ui; spec 02, req 3)
- [ ] **HUD-13** `[emulator ok]` `[emulator ok, verified 2026-11-14]`
  - Action: While a video plays, press the hardware Back button.
  - Expected: The player closes and the item list is shown.
  - Source: (agent report: player-hud-ui)
- [ ] **HUD-14** `[emulator ok]` `[emulator ok, verified 2026-11-15]`
  - Action: Show the HUD and look at the controls.
  - Expected: Back, previous, play or pause, and next are white symbols with no colored background. The decoder label is white text in a thin white outline. No control shows a word such as "Back" or "Next".
  - Source: (owner feedback 2026-11-15; task P1-29)
- [ ] **HUD-15** `[emulator ok]` `[emulator ok, verified 2026-11-15]`
  - Action: Show the HUD and tap the back arrow at the top left.
  - Expected: The player closes and the item list is shown.
  - Source: (task P1-29)

### Playback order

- [ ] **COORD-1** `[emulator ok]` `[emulator ok, verified 2026-11-14]`
  - Action: Let the first file of a folder play to its end.
  - Expected: The next file of the folder starts by itself.
  - Source: (agent report: playback-coordinator)
- [ ] **COORD-2** `[emulator ok]` `[emulator ok, verified 2026-11-14]`
  - Action: Let the last file of a folder play to its end.
  - Expected: Playback stops at the end. It does not restart and does not move to another file.
  - Source: (agent report: playback-coordinator)
- [ ] **COORD-3** `[emulator ok]`
  - Action: After COORD-2, tap the play symbol.
  - Expected: The last file starts again from the beginning.
  - Source: (agent report: playback-coordinator)

## App shell and theme

- [ ] **APP-1** `[emulator ok]` `[emulator ok, verified 2026-11-14]`
  - Action: Start the app from the launcher.
  - Expected: The app opens and shows the folder list. Grant the permission first if it is asked (MED-1 or MED-2).
  - Source: (agent report: app-skeleton; spec 01, req 3)
- [ ] **APP-2** `[emulator ok]` (developer check: needs the Release build and adb or aapt)
  - Action: Build the Release configuration (`dotnet build src\Player.App -f net10.0-android -c Release`) and list the permissions of the built APK with `aapt dump permissions <apk>`.
  - Expected: `android.permission.INTERNET` and `ACCESS_NETWORK_STATE` are not listed.
  - Source: (agent report: app-skeleton)
- [ ] **THEME-1** `[emulator ok]`
  - Action: Open the folder list and then an item list. Look at the navigation bar and its back arrow.
  - Expected: The dark theme is used on every page, including the navigation bar and its back arrow.
  - Source: (agent report: theming-engine)
- [ ] **APP-3** `[physical device required]`
  - Action: Copy `ArielPlayer-phase1-r2.apk` to the phone and install it. If the phone refuses, write down the exact message.
  - Expected: The app installs. Write the phone model and the Android version in the table at the top.
  - Source: (owner feedback 2026-11-15; task P1-30)
- [ ] **APP-4** `[emulator ok]` `[emulator ok, verified 2026-11-15]`
  - Action: Find the app in the phone's app list.
  - Expected: The icon is a white play triangle on a blue background, with the name "Ariel Player". It is not the purple ".NET" icon.
  - Source: (owner feedback 2026-11-15; task P1-30)
- [ ] **APP-5** `[emulator ok]` `[emulator ok, verified 2026-11-15]`
  - Action: Close the app completely (swipe it away from the recent apps) and start it from the launcher. Watch the first second.
  - Expected: A black screen with the app symbol (a white play triangle in a blue circle) appears, then the folder list. No purple screen appears.
  - Source: (owner feedback 2026-11-15; task P1-30)
- [ ] **THEME-2** `[emulator ok]` `[emulator ok, verified 2026-11-15]`
  - Action: Open the folder list and an item list. Look at the Android status bar at the top (clock, battery, signal, notifications).
  - Expected: The status bar has the same dark color as the app's top bar, with white icons. It is not purple.
  - Source: (owner feedback 2026-11-15; spec 07, req 7; task P1-30)
- [ ] **THEME-3** `[emulator ok]`
  - Action: Open an item list and tap the search field.
  - Expected: The line under the search field and the cursor are blue, not purple.
  - Source: (task P1-30)

## Regression

None. Phase 1 is the first phase, so there are no earlier phase items.

## Report back

Write the results in the table at the top of this file and send:

- Number of failed items: ____
- Failed item IDs: ____
- What was seen instead (for each failed item): ____
- Items not run, and why: ____
