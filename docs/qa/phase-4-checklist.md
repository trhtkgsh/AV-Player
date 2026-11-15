# Phase 4 device checklist

- Phase: 4 (background playback, notification, audio focus, media buttons, Picture-in-Picture), with the four fixes from the owner's test of 2026-11-15
- Date: 2026-11-15
- APK to install on a phone: `artifacts\ArielPlayer-phase4.apk` (copy it to the phone and open it, or `adb install -r artifacts\ArielPlayer-phase4.apk`; it installs over the app on the phone)
- SHA-256 of the APK: 3118441DAB07E4F92191199694EFBB0937F812D1F090DBDB2BF7851803BDD0FE
- Developer build to an emulator or a phone with USB debugging: `dotnet build src\Player.App -f net10.0-android -t:Install`

## Phone under test

| Phone model | Android version | Result (PASS / FAIL, and the item IDs that failed) |
|---|---|---|
|  |  |  |
|  |  |  |

## How to read this list

- `[emulator ok]`: can be run on an emulator.
- `[physical device required]`: must be run on a phone.
- `[emulator ok, verified 2026-11-15]`: the orchestrator already checked the item on an emulator (Pixel 7 AVD, API 36 and Pixel 5 AVD, API 34, x86_64). It still needs its normal run on the phone.
- The owner asked to test everything after Phase 4. Run this list first, then the regression section at the end.
- New settings: Settings, "Playback defaults", section "Background": "Keep playing sound in the background" and "Picture-in-Picture when leaving the player". Both are off after an update.
- The folder list has four buttons under the title: "Folders", "Browser", "Favorites", "All files". The "View" menu is gone.
- The player's top bar has a new Picture-in-Picture button between the Aspect button and the three-dot menu.
- While a file plays, Android shows a playback notification with previous, play or pause, next and close.
- The picture never lies under Android's status bar.
- During a seek drag the picture first jumps in steps (the nearest earlier key frame of the video) and settles on the exact time when the finger rests. On most videos a step is one to ten seconds.
- On Android 13 or later the playback notification is drawn by Android as a media control: play or pause, a seek bar, and a small close button.

## Setup

Put these files on the phone (internal storage, for example `Movies/`).

1. A folder with at least three videos (used by most items).
2. One video of 20 minutes or more, for example a film or an episode (used by FIX-7 to FIX-10, BG-1).
3. A wired headset with a button, or Bluetooth headphones, or both (used by FOC-4, BTN items).
4. A second app that plays sound, for example a music app (used by FOC-1, FOC-2).
5. A folder in which at least one file was played before and had its speed changed in the player's Speed sheet (used by FIX-1 to FIX-3).

## Fixes (owner's test of 2026-11-15)

- [ ] **FIX-1** `[emulator ok]` `[emulator ok, verified 2026-11-15]`
  - Action: In the folder of setup 5, open the three-dot options, choose "Default speed for this folder", then "1.5x". Open the file whose speed was changed before.
  - Expected: It plays at 1.5x.
  - Source: (owner feedback 2026-11-15; spec 11, R2-1)
- [ ] **FIX-2** `[emulator ok]` `[emulator ok, verified 2026-11-15]`
  - Action: In that file, open the Speed sheet and choose 2.0x. Go back and open it again, then open another file of the folder.
  - Expected: The first file plays at 2.0x (the newer choice wins), the other at 1.5x.
  - Source: (spec 11, R2-1)
- [ ] **FIX-3** `[emulator ok]` `[emulator ok, verified 2026-11-15]`
  - Action: Choose "Default speed for this folder", then "Use the general default". Open both files.
  - Expected: Both play at the general default speed.
  - Source: (spec 11, R2-1)
- [ ] **FIX-4** `[emulator ok]` `[emulator ok, verified 2026-11-15]`
  - Action: Look at the folder list. Tap "Browser", "Favorites", "Folders" one after the other.
  - Expected: Four buttons in one row under the title. Each tap changes the list at once. The current one is marked with the accent color and a line under it. There is no "View" entry.
  - Source: (owner feedback 2026-11-15; spec 11, R1-1)
- [ ] **FIX-5** `[emulator ok]` `[emulator ok, verified 2026-11-15]`
  - Action: Tap "All files", then the system Back button.
  - Expected: The list of all files opens. Back returns to the folder list.
  - Source: (spec 11, R1-1)
- [ ] **FIX-6** `[emulator ok]`
  - Action: Close the app from the recent apps list and open it again.
  - Expected: The folder list opens in the view that was chosen last.
  - Source: (spec 11, R1-1)
- [ ] **FIX-7** `[physical device required]` `[emulator ok, verified 2026-11-15]`
  - Action: Play the long video. Drag a finger slowly to the right over the picture, without lifting it.
  - Expected: Playback stops. The picture changes while the finger moves and shows scenes close to the time in the indicator (a little before it). It is not the picture from before the drag.
  - Source: (owner feedback 2026-11-15; spec 11, R3-7)
- [ ] **FIX-8** `[physical device required]` `[emulator ok, verified 2026-11-15]`
  - Action: Keep the finger still for one second during the drag.
  - Expected: The picture settles on the exact time shown in the indicator.
  - Source: (spec 11, R3-7)
- [ ] **FIX-9** `[physical device required]` `[emulator ok, verified 2026-11-15]`
  - Action: Drag to the left and to the right several times, then lift the finger.
  - Expected: The picture keeps following. After the lift, playback continues from the time in the indicator.
  - Source: (spec 11, R3-7)
- [ ] **FIX-10** `[physical device required]`
  - Action: Pause the video, drag to another time and lift the finger.
  - Expected: The video stays paused on the picture of the new time.
  - Source: (spec 11, R3-7)
- [ ] **FIX-11** `[emulator ok]`
  - Action: Play a video in portrait and in landscape. Tap to show and hide the controls. Change the Aspect to "Crop" and to "Stretch". Zoom in with two fingers and drag the picture up.
  - Expected: The picture starts below Android's status bar and never lies under it, in every aspect mode and when zoomed. It does not move when the status bar appears or disappears.
  - Source: (owner feedback 2026-11-15; spec 11, R3-1)
- [ ] **FIX-12** `[emulator ok]` `[emulator ok, verified 2026-11-15]`
  - Action: Open the app in Hebrew and look at the four buttons of the folder list.
  - Expected: "תיקיות", "סייר", "מועדפים", "כל הקבצים", from right to left, each on one line.
  - Source: (spec 10; spec 11, R1-1)

## Background audio and notification (spec 08)

- [ ] **BG-1** `[physical device required]`
  - Action: Turn on "Keep playing sound in the background". Play the long video, press Home, turn the screen off and wait 30 minutes with default battery settings.
  - Expected: The sound plays the whole time.
  - Source: (spec 08, requirement 3, acceptance)
- [ ] **BG-2** `[emulator ok]` `[emulator ok, verified 2026-11-15]`
  - Action: With the setting on, play a video and press Home. Open the app again from the launcher or by tapping the notification.
  - Expected: The sound continues in the background. Back in the app the player is shown, the picture returns within a moment and playback did not restart.
  - Source: (spec 08, requirement 3)
- [ ] **BG-3** `[emulator ok]` `[emulator ok, verified 2026-11-15]`
  - Action: Turn the setting off. Play a video and press Home. Open the app again.
  - Expected: Playback pauses when the app is left. The player shows the paused picture.
  - Source: (spec 08, requirement 3)
- [ ] **BG-4** `[emulator ok]`
  - Action: Turn on the setting for the first time (Android 13 or later).
  - Expected: Android asks to allow notifications. Playback works with either answer.
  - Source: (spec 08, requirement 6)
- [ ] **BG-5** `[emulator ok]` `[emulator ok, verified 2026-11-15]`
  - Action: Play a video and pull down the notification shade. On Android 13 or later, the close button is the small cross inside Android's media control.
  - Expected: A playback notification with the file name and previous, play or pause, next (and close on Android 12 or lower, or in the expanded view).
  - Source: (spec 08, requirement 1)
- [ ] **BG-6** `[emulator ok]`
  - Action: With background audio on, press Home during playback and use pause, play, next and previous in the notification.
  - Expected: Each one works. The notification shows the new file name after next.
  - Source: (spec 08, requirement 1)
- [ ] **BG-7** `[emulator ok]` `[emulator ok, verified 2026-11-15]`
  - Action: Close playback from the notification (close button, or swipe the notification away while paused if there is no button) while the app is in the background. On Android 13 or later, the close button is the small cross inside Android's media control. Open the app.
  - Expected: The sound stops and the notification goes away. The app shows the file list, not the player.
  - Source: (spec 08, requirement 2)
- [ ] **BG-8** `[emulator ok]` `[emulator ok, verified 2026-11-15]`
  - Action: Play a video, then press the system Back button in the player.
  - Expected: Playback stops and the notification goes away.
  - Source: (spec 08, requirement 2)
- [ ] **BG-9** `[physical device required]`
  - Action: With background audio on, play a video, press Home, then swipe the app away in the recent apps list. Open the app and the same file again.
  - Expected: The sound stops. The file continues from the position where it was.
  - Source: (spec 08, requirement 9, acceptance)
- [ ] **BG-10** `[physical device required]`
  - Action: With background audio on, pause from the notification with the app in the background and wait more than 10 minutes.
  - Expected: The notification goes away. Opening the app later shows the player, paused at the same position (or the file list if Android closed the app in the meantime; the position is still saved).
  - Source: (spec 08, requirement 2)
- [ ] **BG-11** `[emulator ok]`
  - Action: Switch the interface to Hebrew and play a video. Look at the notification on Android 12 or lower, or its expanded view.
  - Expected: The button texts are in Hebrew.
  - Source: (spec 10)
- [ ] **BG-12** `[physical device required]`
  - Action: With background audio on, play a video and press Home. In Android's settings open Apps, this app, and press "Force stop". Open the app and the same file.
  - Expected: The file continues close to the position where it was stopped (at most about 5 seconds earlier).
  - Source: (spec 08, acceptance; spec 04)

## Audio focus and headphones (spec 08)

- [ ] **FOC-1** `[physical device required]`
  - Action: Play a video, then start music in another app.
  - Expected: The video pauses and stays paused.
  - Source: (spec 08, requirement 4)
- [ ] **FOC-2** `[physical device required]`
  - Action: Play music in another app, then open a video in this app.
  - Expected: The other app stops. The video plays.
  - Source: (spec 08, requirement 4)
- [ ] **FOC-3** `[physical device required]`
  - Action: Play a video and receive a phone call, or let an alarm or a navigation voice sound. End the call.
  - Expected: The video pauses during the call and continues after it. For a short sound the volume may only drop for a moment.
  - Source: (spec 08, requirement 4)
- [ ] **FOC-4** `[physical device required]`
  - Action: Play with headphones (wired or Bluetooth), then unplug them or switch them off.
  - Expected: Playback pauses at once. Nothing plays from the phone's speaker.
  - Source: (spec 08, requirement 5; spec 09, requirement 7)

## Media buttons and Bluetooth (spec 09)

- [ ] **BTN-1** `[physical device required]`
  - Action: Play a video with a wired or Bluetooth headset. Press the headset button once. Press it once again.
  - Expected: Pause, then play.
  - Source: (spec 09, requirement 5)
- [ ] **BTN-2** `[physical device required]`
  - Action: Press the headset button twice quickly.
  - Expected: The next file plays.
  - Source: (spec 09, requirement 5)
- [ ] **BTN-3** `[physical device required]`
  - Action: Press the headset button three times quickly.
  - Expected: The previous file plays (or the file starts again when it is the first).
  - Source: (spec 09, requirement 5)
- [ ] **BTN-4** `[physical device required]`
  - Action: Use the play, pause, next and previous buttons of Bluetooth headphones, a speaker or a car.
  - Expected: Each one works, also with the screen off when background audio is on.
  - Source: (spec 09, requirement 6)
- [ ] **BTN-5** `[physical device required]`
  - Action: Look at a Bluetooth device that shows text (a car or a speaker with a display), or at Android's media controls in the quick settings.
  - Expected: The file name is shown without its extension, with the folder name. The position follows playback.
  - Source: (spec 09, requirement 3)
- [ ] **BTN-6** `[emulator ok]`
  - Action: During playback open Android's quick settings.
  - Expected: A media control for the app with the file name, play or pause, previous and next and a seek bar. Dragging the seek bar moves the video.
  - Source: (spec 09, requirements 2 and 4)
- [ ] **BTN-7** `[emulator ok]` `[emulator ok, verified 2026-11-15]`
  - Action: Developer check: `adb shell dumpsys media_session`.
  - Expected: A session of `com.arielplayer.app` that is active, with the state playing or paused, the position and the file name.
  - Source: (spec 09, acceptance)
- [ ] **BTN-8** `[emulator ok]` `[emulator ok, verified 2026-11-15]`
  - Action: Developer check: `adb shell dumpsys activity services com.arielplayer.app` during playback.
  - Expected: `isForeground=true` and the type media playback.
  - Source: (spec 08, acceptance)

## Picture-in-Picture (spec 08)

- [ ] **PIP-1** `[emulator ok]` `[emulator ok, verified 2026-11-15]`
  - Action: Play a video and press the Picture-in-Picture button in the top bar.
  - Expected: The video continues in a small window over the home screen. No controls of the app are drawn inside it.
  - Source: (spec 08, requirement 7)
- [ ] **PIP-2** `[emulator ok]` `[emulator ok, verified 2026-11-15]`
  - Action: Tap the small window. Use previous, play or pause and next.
  - Expected: Each one works. The middle symbol changes between play and pause.
  - Source: (spec 08, requirement 7)
- [ ] **PIP-3** `[emulator ok]` `[emulator ok, verified 2026-11-15]`
  - Action: Expand the small window to full screen.
  - Expected: The player returns, playing, with the controls hidden. A tap shows them.
  - Source: (spec 08, requirement 7)
- [ ] **PIP-4** `[emulator ok]` `[emulator ok, verified 2026-11-15]`
  - Action: Enter Picture-in-Picture and close the small window (drag it to the bottom of the screen or use its close button). Open the app.
  - Expected: The sound stops. The app shows the file list. The position was saved.
  - Source: (spec 08, requirement 8)
- [ ] **PIP-5** `[emulator ok]` `[emulator ok, verified 2026-11-15]`
  - Action: Turn on "Picture-in-Picture when leaving the player". Play a video and press Home.
  - Expected: The video continues in a small window. This also holds when "Keep playing sound in the background" is off.
  - Source: (spec 08, requirement 7)
- [ ] **PIP-6** `[emulator ok]` `[emulator ok, verified 2026-11-15]`
  - Action: With that setting on, pause the video and press Home. Then, from the file list, press Home.
  - Expected: No small window in both cases.
  - Source: (spec 08, requirement 7)
- [ ] **PIP-7** `[physical device required]`
  - Action: Play a vertical video and a wide video in Picture-in-Picture.
  - Expected: The window has the shape of the video.
  - Source: (spec 08, requirement 7)
- [ ] **PIP-8** `[physical device required]`
  - Action: Enter and leave Picture-in-Picture 10 times. Rotate the phone while the small window is shown.
  - Expected: The picture is there every time. Nothing crashes.
  - Source: (spec 08, acceptance)
- [ ] **PIP-9** `[physical device required]`
  - Action: With both settings on, play a video and press Home. Then turn the screen off and on.
  - Expected: The small window appears (Picture-in-Picture wins). With the screen off the sound continues. After the screen is on again the small window shows the picture.
  - Source: (spec 08, requirement 8)
- [ ] **PIP-10** `[emulator ok]`
  - Action: Play an audio file.
  - Expected: There is no Picture-in-Picture button.
  - Source: (spec 08, requirement 7)

## Regression

The earlier lists still apply. The owner asked to check everything. Run them after the items above, in this order.

| List | What to run | What changed |
|---|---|---|
| `phase-3-r2-checklist.md` | All items | EXP-9 (the "View" list) is replaced by FIX-4. EXP-10 to EXP-17 use the four buttons instead of the "View" list. PLR-3 and PLR-4: the picture now starts below the status bar. PLR-11 to PLR-13 are replaced by FIX-7 to FIX-10. SET-3 is replaced by FIX-2. |
| `phase-3-checklist.md` | The Phase 3 items and its regression blocks for Phase 2 and Phase 1 | The changes named at the top of `phase-3-r2-checklist.md` apply. One more change: with "Keep playing sound in the background" off, leaving the app pauses playback as before; with it on, the sound continues. |

- [ ] **REG-1** `[emulator ok]`
  - Action: Open a video and let it play to the end with Repeat off.
  - Expected: As before: the player stays on the last picture or goes to the next file by the repeat setting. The notification stays until the player is closed.
  - Source: (spec 03; spec 08, requirement 2)
- [ ] **REG-2** `[emulator ok]`
  - Action: In the player open the three-dot menu and each sheet, change the subtitle style, switch the decoder.
  - Expected: All work as before. Playback does not stop when a sheet opens.
  - Source: (spec 03)
- [ ] **REG-3** `[emulator ok]`
  - Action: Lock the screen with the player's Lock button, press Home and return.
  - Expected: Still locked, as before.
  - Source: (spec 02, requirement 5)
- [ ] **REG-4** `[emulator ok]`
  - Action: Double tap left and right, drag for volume and brightness, pinch, hold a finger for the fast speed.
  - Expected: All work as before.
  - Source: (spec 02, requirement 4; spec 11, R3-8)
- [ ] **REG-5** `[emulator ok]`
  - Action: Seek with the seek bar while playing and while paused.
  - Expected: Playback continues from the new time. While paused the picture of the new time is shown.
  - Source: (spec 02)

## Report back

Write the results in the table at the top of this file and send:

- Phone model and Android version: ____
- Number of failed items: ____
- Failed item IDs: ____
- What was seen instead (for each failed item): ____
- Items not run, and why: ____
- BG-1, how long the sound played (minutes, and whether it stopped): ____
