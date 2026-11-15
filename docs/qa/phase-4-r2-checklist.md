# Phase 4 revision 2 device checklist

- Phase: 4, revision 2 (the owner's test of the Phase 4 APK: picture after returning to the player, new settings on by default, faster picture in the seek drag, folder speed after the update)
- Date: 2026-11-15
- APK to install on a phone: `artifacts\ArielPlayer-phase4-r2.apk` (copy it to the phone and open it, or `adb install -r artifacts\ArielPlayer-phase4-r2.apk`; it installs over the app on the phone)
- SHA-256 of the APK: 0E969A7A52CCA457F07E99B1FFEB306B8E6F898890AE928B0224E2037393CC14
- Developer build to an emulator or a phone with USB debugging: `dotnet build src\Player.App -f net10.0-android -t:Install`

## Phone under test

| Phone model | Android version | Result (PASS / FAIL, and the item IDs that failed) |
|---|---|---|
|  |  |  |
|  |  |  |

## How to read this list

- `[emulator ok]`: can be run on an emulator.
- `[physical device required]`: must be run on a phone.
- `[emulator ok, verified 2026-11-15]`: the orchestrator already checked the item on an emulator (Pixel 7 AVD, API 36, x86_64). It still needs its normal run on the phone.
- This list covers only what changed since `ArielPlayer-phase4.apk`. The items of `phase-4-checklist.md` that were not run yet still apply, mainly: audio focus (FOC-1 to FOC-4), media buttons and Bluetooth (BTN-1 to BTN-6), 30 minutes in the background (BG-1), force stop (BG-12).
- "Keep playing sound in the background" and "Picture-in-Picture when leaving the player" are now on in a new installation and after "Reset all to defaults". On a phone where the app was already installed the stored choice is kept: check both in Settings, "Playback defaults", section "Background".
- With both on: pressing Home while a video plays shows the small window. Turning the screen off keeps the sound.
- When the app returns from the background the video starts again at the same position: the sound stops for a moment and the picture appears about a second later, complete.
- A folder's default speed that was chosen in an older version applies to files with an own speed only after it is chosen once more (the owner saw this and it then worked).

## Setup

Put these files on the phone (internal storage, for example `Movies/`).

1. A film or an episode of 20 minutes or more (used by all R2 items).
2. A folder with at least two videos (used by R2-7).

## Revision 2

- [ ] **R2-1** `[physical device required]` `[emulator ok, verified 2026-11-15]`
  - Action: With "Keep playing sound in the background" on and "Picture-in-Picture when leaving the player" off, play the film, press Home, wait 10 seconds and open the app again.
  - Expected: The sound continues in the background. Back in the player the picture is black for about a second, then complete. There are no colored blocks or a partly black picture.
  - Source: (owner feedback 2026-11-15; spec 08, requirement 3)
- [ ] **R2-2** `[physical device required]` `[emulator ok, verified 2026-11-15]`
  - Action: Play the film, turn the screen off for 10 seconds and on again (unlock if needed).
  - Expected: As R2-1.
  - Source: (owner feedback 2026-11-15; spec 08, requirement 3)
- [ ] **R2-3** `[physical device required]`
  - Action: Repeat R2-1 five times in a row, also with a second film of another kind (for example an MKV and an MP4).
  - Expected: The picture is complete every time.
  - Source: (owner feedback 2026-11-15)
- [ ] **R2-4** `[physical device required]` `[emulator ok, verified 2026-11-15]`
  - Action: Pause the film, press Home and open the app again.
  - Expected: The film is still paused at the same time and shows its picture.
  - Source: (spec 08, requirement 3; spec 04)
- [ ] **R2-5** `[physical device required]` `[emulator ok, verified 2026-11-15]`
  - Action: With "Picture-in-Picture when leaving the player" on, play the film, press Home, then expand the small window.
  - Expected: The small window shows the video. After expanding, the picture is complete and playback did not stop.
  - Source: (spec 08, requirement 7)
- [ ] **R2-6** `[emulator ok]` `[emulator ok, verified 2026-11-15]`
  - Action: Remove the app's data or install on a phone without the app, open Settings, "Playback defaults".
  - Expected: Both switches of "Background" are on.
  - Source: (owner feedback 2026-11-15; spec 08)
- [ ] **R2-7** `[emulator ok]` `[emulator ok, verified 2026-11-15]`
  - Action: Set the speed of one file to 2.0x in the player and let it play a few seconds. Then open another file of the folder whose speed chip shows 1.0x.
  - Expected: The second file plays at normal speed, as its speed chip says.
  - Source: (spec 03; spec 11, R2-1)
- [ ] **R2-8** `[physical device required]` `[emulator ok, verified 2026-11-15]`
  - Action: Play the film and drag a finger slowly to the right over the picture.
  - Expected: The picture changes while the finger moves, faster than in the last version, and settles on the exact time about a quarter of a second after the finger rests.
  - Source: (owner feedback 2026-11-15; spec 11, R3-7)
- [ ] **R2-9** `[physical device required]`
  - Action: Play the film, open another app from the recent apps list, return. Then open the notification shade over the player and close it. Then open a sheet from the three-dot menu and close it.
  - Expected: After the other app the picture returns as in R2-1. The shade and the sheets do not interrupt the picture or the sound.
  - Source: (owner feedback 2026-11-15; regression)
- [ ] **R2-10** `[physical device required]` `[emulator ok, verified 2026-11-15]`
  - Action: Rotate the phone several times during playback.
  - Expected: The picture and the sound continue without a restart.
  - Source: (regression; spec 02)

## Report back

Write the results in the table at the top of this file and send:

- Phone model and Android version: ____
- Number of failed items: ____
- Failed item IDs: ____
- What was seen instead (for each failed item): ____
- For R2-1 to R2-3, if the picture is broken again: for how many seconds, and with which file (name ending and, if known, the resolution): ____
- Items not run, and why: ____
- BG-1 (from `phase-4-checklist.md`), how long the sound played (minutes, and whether it stopped): ____
