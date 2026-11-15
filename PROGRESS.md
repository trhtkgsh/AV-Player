# PROGRESS

Tracking file required by docs/MASTER.md section 8. Updated by the orchestrator only.

## Current phase

Phase 5, revision 2 (`docs/specs/13-owner-requests-r7.md`): built on 2026-11-15, waits for the owner's
device test. APK: `artifacts/AVPlayer-phase5-r2.apk` (Release, built from main at e61152d, 109,413,152 bytes,
three ABIs, SHA-256 CABD3CCE0B1CE98FFF2F86317C3019896FF86B621A8736B0EEC36C5244A6DC4E, label "AV Player",
application id and signing certificate unchanged, no network permission). Checklist:
`docs/qa/phase-5-r2-checklist.md` (78 items). It follows the
owner's report on the Phase 5 APK (table "Owner report on 2026-11-15 20:21" below).

Before that: Phase 5 with the quiet display (`docs/specs/12-playback-overlay.md`): built on 2026-11-15, waits for the
owner's device test. APK: `artifacts/ArielPlayer-phase5.apk` (Release, built from main at 2e84fe5,
108,652,422 bytes, arm64-v8a, armeabi-v7a and x86_64,
SHA-256 BEADD66B82991289F0DECC8BAB9088DFAC41227798260EB51FD28BBBAE7E19DA, same signing certificate; new
permission `WRITE_EXTERNAL_STORAGE` up to API 28 only; no network permission). Checklist:
`docs/qa/phase-5-checklist.md`. Task log and verification: section "Phase 5 and the quiet display" below.
On 2026-11-15 19:10 the owner reported on
`ArielPlayer-phase4-r2.apk`: "Everything is fine", asked for the quiet display and said to complete it and
continue to Phase 5. He did not say which items were run; audio focus, media buttons, Bluetooth, BG-1 and
BG-12 were not mentioned, so they stay in the Phase 5 list.

Before that: Phase 4, revision 2 was built on 2026-11-15.
On 2026-11-15 18:15 the owner reported on `ArielPlayer-phase4.apk` (Samsung Galaxy A55, Android 17): three
faults and one wish (table "Owner feedback on 2026-11-15 (Phase 4 APK)" below), "works" for the second point of
the report, and the three Phase 4 decisions approved with the remark that the new settings must be on. He did
not say which items of the list were run; audio focus, media buttons and Bluetooth were not mentioned.

Revision APK: `artifacts/ArielPlayer-phase4-r2.apk` (Release, built from main at 0d6abdd on 2026-11-15,
108,659,488 bytes, arm64-v8a, armeabi-v7a and x86_64,
SHA-256 0E969A7A52CCA457F07E99B1FFEB306B8E6F898890AE928B0224E2037393CC14, same signing certificate and
permissions as the Phase 4 APK, no network permission). Checklist: `docs/qa/phase-4-r2-checklist.md` (10 items
for what changed); the items of `docs/qa/phase-4-checklist.md` that were not run still apply.

Earlier: Phase 4 with the third revision was built on 2026-11-15. On 2026-11-15 16:18 the owner reported on `ArielPlayer-phase3-r2.apk` (Samsung Galaxy A55,
Android 17, One UI 9): four items to fix (table "Owner feedback on 2026-11-15 (revision 2 APK)" below), the
three readings of the requests approved, the long-press speed and the language default confirmed. He did not
say which other items of the list were run. His instruction: "continue with the fixes and go straight to
Phase 4, then we will check everything". So the gate of revision 2 is not closed by a full device pass: the
four items are fixed, Phase 4 is built on top, and one device test covers both (owner decision, recorded below).

Phase 4 APK: `artifacts/ArielPlayer-phase4.apk` (Release, built from main at a661014 on 2026-11-15,
108,316,550 bytes, arm64-v8a, armeabi-v7a and x86_64,
SHA-256 3118441DAB07E4F92191199694EFBB0937F812D1F090DBDB2BF7851803BDD0FE, same signing certificate; installs
over the app on the phone and keeps its data). New permissions: `FOREGROUND_SERVICE`,
`FOREGROUND_SERVICE_MEDIA_PLAYBACK` and `POST_NOTIFICATIONS` (the playback service and its notification).
No network permission. Checklist: `docs/qa/phase-4-checklist.md` (51 items, then the regression lists). What
was run and what was not is in "Phase 4 verification" below.

Earlier: Phase 3, revision 2 was built on 2026-11-15 for the owner's eleven requests.
The owner tested `ArielPlayer-phase3.apk` on the phone and wrote "so far I have found no faults", with eleven
new requests and the instruction to sort them by the order of the phases. He did not say that the whole
checklist was run, so the Phase 3 gate is recorded as "no faults reported", not as a full checklist pass. The
requests are sorted in `docs/specs/11-owner-requests-r3.md` (Phase 1 scope: explorer; Phase 2 scope: stores and
settings; Phase 3 scope: player and gestures; Phase 5: file options; none for Phase 4) and were built in that
order. The table is in "Owner feedback on 2026-11-15 (Phase 3 APK)" below.

Revision APK: `artifacts/ArielPlayer-phase3-r2.apk` (Release, built from main at 253c918 on 2026-11-15,
108,258,080 bytes, arm64-v8a, armeabi-v7a and x86_64,
SHA-256 86574F14AC9E629558D9DC3CCE88CA9FBCA35D42BCBE5AA5C1FDF433F41707E7, same signing certificate and the same
permissions as the Phase 3 APK, no network permission; installs over the Phase 3 app and keeps its data).
Checklist: `docs/qa/phase-3-r2-checklist.md` (43 items for what changed; the regression blocks of
`docs/qa/phase-3-checklist.md` still apply). What was run and what was not is in "Phase 3 revision verification"
below.

Phase 3 APK (the build the owner tested): `artifacts/ArielPlayer-phase3.apk` (Release, built from main at 78f6efb on 2026-11-15, 107.6 MB,
arm64-v8a, armeabi-v7a and x86_64, SHA-256 E105DF2C7F03F6A219B47186FE55CE543BB4221FA215264A523558E3F2A5D028,
same signing certificate as the earlier APKs; installs over the Phase 2 app and keeps its data). Permissions:
the media read permissions and, new, `BATTERY_STATS` (declared because MAUI's battery API requires the
declaration; Android does not grant it to an ordinary app and shows no dialog for it). No network permission.
Checklist: `docs/qa/phase-3-checklist.md`. What was run and what was not is in "Phase 3 verification" below.

The Phase 2 gate is passed: on 2026-11-15 the owner ran
`docs/qa/phase-2-checklist.md` with `ArielPlayer-phase2.apk` on a Samsung Galaxy A55 (Android version not given):
0 failed items, nothing skipped, ARM-1 and ARM-2 without any problem. So the hang of the arm64 build on the
emulator's ARM translation (SPIKE finding 20) does not occur on that phone. The owner sent three items with the
report; they are in "Owner feedback on 2026-11-15 (Phase 2 APK)" below and are handled before the Phase 3 work.
The Phase 1 gate is passed: on 2026-11-15 the owner reported that
`ArielPlayer-phase1-r2.apk` installs and works on the phone and that every item of `docs/qa/phase-1-checklist.md`
was run and passed. The owner did not name the phone model or the Android version.
The roster was approved on 2026-11-14 and the HARD GATE was passed then.

Phase 2 APK: `artifacts/ArielPlayer-phase2.apk` (Release, built from main at aaa5f15 on 2026-11-15, 107 MB,
arm64-v8a, armeabi-v7a and x86_64, SHA-256 28ABA7B8FBAD1EDCA1E333586E791E5FDC5C9AE8FF153DC772E74F2D84B0B1F3,
same signing certificate as the Phase 1 APK, no network permission). Checklist: `docs/qa/phase-2-checklist.md`.
What was run and what was not is in "Phase 2 verification" below. One open risk: the arm64 half stops
responding on the emulator's ARM translation when the video is paused; the cause points to the translation
layer, and only a real phone can settle it (checklist items ARM-1 and ARM-2).

Phase 1 APK: `artifacts/ArielPlayer-phase1-r2.apk` (Release, built from main at acf3f1c on 2026-11-15, 105 MB,
arm64-v8a, armeabi-v7a and x86_64, SHA-256 DEDAD755AB8A3AF9C178BFFF4BA01B7C34DA12AA612594600CE7E4242789B838). It replaces
`ArielPlayer-phase1.apk` (2826f17), which the owner could not install. The same file was run on two emulators: API 36 as
x86_64 and as arm64 (through the image's ARM translation), API 34 as x86_64. Checked: install, launcher icon, splash,
status bar color, explorer, thumbnails, playback, HUD symbols, pause and play, previous, next, back, decoder switch
(mediacodec to avcodec on arm64). The armeabi-v7a part has not run anywhere.

## Owner feedback on 2026-11-15 (first APK)

| # | Feedback | What was found | What was done | State |
|---|---|---|---|---|
| 1 | The app cannot be installed on the phone. | The owner gave no error text and no phone model. The first APK is valid: v2 and v3 signatures verify, minSdk 26, zip aligned, installs on API 34 and API 36. It contained only arm64-v8a and x86_64, so a phone that runs a 32-bit Android refuses it as not compatible. | P1-30 adds armeabi-v7a. The arm64 part was run for the first time (on the emulators' ARM translation) and works. | Closed: r2 installs on the owner's phone (reported 2026-11-15). The cause was not established: the added 32-bit ABI fits the result, but the owner sent no error text and no phone model, so it is not proven. |
| 2 | The player buttons look like ordinary buttons. They must be white symbols with no background. | The HUD used text buttons with the accent fill. | P1-29: vector symbols for back, previous, play or pause, next; the decoder is an outlined HW/SW label. | Done, checked on API 34 and API 36 in the Release build. |
| 3 | The app needs its own icon, on the launcher and on the splash screen. | Both were the purple ".NET" template images. | P1-30: a white play triangle on a blue gradient (launcher), the same symbol in a blue circle on black (splash). | Done, checked on API 34 and API 36. |
| 4 | The Android status bar must have the color of the app design. | It was purple: the template colors in `colors.xml`. | P1-30: `colorPrimary` and `colorPrimaryDark` are the Dark OLED surface color, `colorAccent` the accent blue. | Done for the one theme of Phase 1, checked on API 34 and API 36. Following a theme change at runtime stays a Phase 3 task (ARCHITECTURE.md section 5). |

## Owner feedback on 2026-11-15 (Phase 2 APK, Samsung Galaxy A55)

| # | Feedback | What was found | What was done | State |
|---|---|---|---|---|
| 5 | Add a Hebrew interface as an option (an addition, not a replacement). | Every text of the app was an English literal in XAML or in a ViewModel. Spec 06 listed language selection as out of scope: the owner's request changes that. | Spec 10, the localization module and the L10N tasks: every text in English and Hebrew, a "Language" choice on the Interface page (System default, English, עברית), right-to-left layout in Hebrew, change at once. Orchestrator's decision: the default is "System default", so on the owner's Hebrew phone the app opens in Hebrew; English is one tap away and stays complete. | Done, checked on API 34 and API 36 in the Release build. Waiting for the owner (checklist LANG-1 to LANG-8). |
| 6 | In "fit to screen" the video shows at about 99% of the screen and becomes 100% after pause or play. On leaving the player the picture is stretched over the whole screen for about a second. | The engine read the size of the video track only on track events and on pause or play, and LibVLC raises no event when the decoder corrects the size. On leaving, the engine's "no format" report made the surface fill its container while the last picture was still on it. | P3-01, P3-05: the format is read again while the first pictures are shown; the surface keeps its shape when the engine stops. | Fixed in code. The 99% picture could not be reproduced on the emulators, so only the owner's phone can confirm it (checklist FIX-2, FIX-3). |
| 7 | The play, next and previous buttons are reversed: the play triangle points left and next/previous swapped places. | The phone's language is Hebrew, so Android lays the app out right to left and MAUI mirrors the transport row and its vector symbols. Media transport controls must not be mirrored. | P3-02: the bottom bar of the player is left to right in every language; symbols are not mirrored, except the back arrow. | Done, checked on the emulator with a Hebrew app locale. Waiting for the owner (checklist FIX-1). |

## Owner feedback on 2026-11-15 (Phase 3 APK): eleven requests, sorted by phase

The owner's order is kept in the first column. "Phase" is the phase whose module the request changes (spec 11).

| # | Request | Phase | Spec 11 | What was done | State |
|---|---|---|---|---|---|
| 7 | A list or grid (icons) view in the explorer. | 1 | R1-2 | "Grid" or "List" in the title bar of the folder list and the file list. Tiles with a large thumbnail (files) or a folder symbol (folders); two columns in portrait, up to four in landscape. | Done, emulator API 34 and 36. |
| 11 | Explorer modes: all media folders, file browser, favorites; a setting to include audio-only folders; favorites are added from folder options, like the file options. | 1 | R1-1, R1-3, R1-4 | "View" opens Media folders, File browser, Favorites, All files. The file browser shows the media folders as a directory tree, one level at a time; Back goes up. Folder options open from a three-dot button on every folder: add to or remove from favorites, default speed. The switch "Show folders with audio only" is on the Interface page. | Done, emulator. File options (rename, delete) stay in Phase 5 and will use the same button. |
| 8 | A default speed per folder. | 2 | R2-1 | New store (`IFolderSettingsStore`, agent `folder-settings-store`). Order: the file's own speed, the folder's, the general default. | Done, emulator (1.5x folder, other folder 1.0x). |
| 9 | Font size for folder and file names. | 2 | R2-2 | "Size of names" on the Interface page, 80% to 160%. | Done, emulator. |
| 10 (setting) | A setting for the long-press speed. | 2 | R2-3 | "Speed while holding the picture" on the Interface page, 0.25x to 4.0x, default 2.0x. | Done, emulator. |
| 1 | The status bar and the navigation bar appear and disappear with the player controls. | 3 | R3-1 | `IVideoSurface.SetSystemBarsVisible`, the HUD keeps clear of the bars with `SystemBarInsets`; the picture does not move; the bars are see-through with white symbols. | Done, measured on API 34 and 36 in landscape with a 4:3 video. |
| 2 | The video name on its own line. | 3 | R3-2 | First line of the top bar. | Done. |
| 3 | Speed and decoder move to the top. | 3 | R3-3, R3-5 | The speed chip is in the top bar; the decoder is an item of the top-bar menu (orchestrator's reading of this request together with request 5). | Done. To confirm with the owner. |
| 4 | No exit button: the system has one. | 3 | R3-4 | The Back button of the player is removed. | Done. |
| 5 | Subtitles, audio track, decoder and repeat in a menu in the top bar, next to Lock. | 3 | R3-5 | A three-dot menu: Subtitles, Audio track, Repeat, "Decoder: HW" or "SW". | Done, emulator, English and Hebrew. |
| 6 | The seek gesture shows a still picture of the target in real time, without playing. | 3 | R3-7 | The drag pauses, seeks while the finger moves (only the newest target is sent), and continues on release. | Done, emulator. Speed on long files with far-apart key frames is not measured: only the phone can tell. |
| 10 (gesture) | Holding a finger on the video plays faster; the earlier speed returns when the finger lifts. | 3 | R3-8 | `GestureKind.LongPress` after half a second; the speed is not saved for the file. | Done, emulator. |

## Owner feedback on 2026-11-15 (revision 2 APK, Samsung Galaxy A55, Android 17, One UI 9)

| # | Feedback | What was found | What was done | State |
|---|---|---|---|---|
| 8 | The default speed of a folder does not work. | Not reproduced as such: on the emulator with the same APK a folder's speed applied to never-played files, to played files and after "Next". It did not apply to a file whose speed had once been chosen in the player: the file's own speed won (the rule of spec 11, R2-1). That is the likely cause on the phone; it is not confirmed, the owner gave no steps. | R4-10: the newest choice wins. Choosing a folder's default speed clears the own speed of every file of that folder. | Fixed for that cause, verified on the emulator (FIX-1 to FIX-3). If it still fails on the phone, the steps are needed. |
| 9 | All the kinds of folder display must be direct buttons, not a menu. | The four modes were in a "View" menu. | R4-10: a row of four buttons under the title: "Folders", "Browser", "Favorites", "All files". | Done, emulator, English and Hebrew (FIX-4, FIX-5, FIX-12). |
| 10 | The still picture in the seek drag must be the picture of the target time, not of the start; PLR-11 to PLR-13: the picture does not change in the drag. | Reproduced with a 2 minute 1080p file whose key frames are 10 s apart: the picture stayed on the start frame during the whole drag. Two causes (SPIKE findings 29 and 30): the drag sent a seek for every finger movement and LibVLC drops the picture of a seek when the next one arrives; and LibVLCSharp does not pass LibVLC's "fast seek" argument, so the seek kind was chance. My earlier check used 12 to 20 second files with a key frame every second or two, where it happened to work. | R4-01, R4-20, R4-21: a paused seek is a fast seek at once and the exact seek when the finger rests; the engine completes a paused seek when its picture is shown; the drag sends a new target only 300 ms of video apart. | Fixed, verified on the emulator with that file in the Release build (FIX-7 to FIX-9). The phone decides how smooth it is on real films. |
| 11 | The video may reach Android's top bar but must not pass it. | In revision 2 the picture used the whole window, also under the status bar (my reading of "the picture never moves"). | R4-02: the picture area starts below the status bar, shown or hidden, also when zoomed. | Done; measured in landscape on API 34 (picture rows 66 to 1079 with the controls hidden, shown and hidden). Crop, Stretch and zoom were not run. |

## Owner feedback on 2026-11-15 (Phase 4 APK, Samsung Galaxy A55, Android 17)

| # | Feedback | What was found | What was done | State |
|---|---|---|---|---|
| 12 | The folder's default speed is shown as the folder's speed, but the video plays at the app's default speed. After setting the folder's speed again it was fine. | This is the rule of R4-10 meeting a setting from the older version: the folder's speed was chosen before the update, when choosing it did not yet clear the own speeds of the files; the files kept their own speed (for example 1.0x from the Phase 2 tests) until the folder's speed was chosen once more. One more fault was found by reading while looking for it: the engine set LibVLC's rate only when it was not 1.0, and LibVLC keeps the rate of the file before, so a 1.0x file after a faster one would play at the faster rate while showing 1.0x (SPIKE finding 37). | No migration: a folder speed chosen in the older version needs choosing once more; a speed chosen from now on applies at once. R5-01: the rate is set for every file. | The owner's case is explained and already resolved on his phone. The rate fault is fixed (measured on the emulator: a 1.0x file after a 2.0x file plays 5.2 s in 5 s). |
| 13 | Background playing works only when it is turned on by hand: with a video playing, leaving the app or turning the screen off stops it. The new settings must of course be on. Picture-in-Picture is fine. | Both settings were off by default (my decision in Phase 4). | R5-01: `BackgroundAudio` and `PictureInPictureOnLeave` are on by default. A choice that is already stored on a phone is kept, so on the owner's phone the switches stay as he left them. | Done, verified on the emulator with a fresh installation (both switches on; Home while playing opens the small window; screen off keeps the position running). |
| 14 | After returning to the player during playback the picture is black, and parts become colored with motion, for some seconds until it settles. | Not reproduced: on the emulators the picture came back clean with MP4 and MKV. The description fits a hardware decoder that began between two key frames. The code allowed two ways to that (SPIKE finding 36): the video track was selected again in mid-stream and then resynchronized by a seek; and a surface loss that was followed at once by a new surface could pass unseen by the engine, leaving the decoder on a dead surface. | R5-01, R5-02: the picture comes back by starting the input again at the position (as a file is opened); the surface is released in order, with the video output stopped before the views are detached. | Changed on the two suspected paths; the cause is not proven, so only the phone can confirm it (R2-1 to R2-3). The sound now stops for a moment at the return. |
| 15 | If possible, the picture in the seek drag should change faster. | The engine waited 150 ms after LibVLC had the data before it took the next target, and 350 ms of rest before the exact picture. | R5-01: 60 ms and 250 ms. | Done. The rest of the time is LibVLC's own seek and the decoder; how much faster it feels on the phone is not measured. |

## Owner request on 2026-11-15 19:10 (Phase 4 revision 2 APK: "everything is fine")

| # | Request | Reading and decision | Task | State |
|---|---|---|---|---|
| 16 | While playing on the full screen without the controls, show, each optional in the player settings: a thin line along the whole screen with the progress "in the color that was set"; the clock; the battery percent; elapsed, total and remaining time (choose the combination). Position: a corner of the screen, not of the video, flush with 0 gap, plus a choice of the margin from the edge; size; color, white by default. | Spec 12. Orchestrator's readings, to be confirmed by the owner: the line uses the theme's progress color; one corner holds all the texts in one line; the margin default is 0; the line's edge (bottom or top) is a setting; the time combination is a setting with six choices, default "elapsed / total". | R6-01, R6-10, R6-20, R6-T1 | in work |
| 17 | Elapsed and remaining time in the playback controls too. | The seek row shows elapsed at the left and `-remaining / total` at the right. | R6-20 | in work |

## Owner report on 2026-11-15 20:21 (Phase 5 APK): answers and fourteen requests (spec 13)

Answers: with rounded corners the text is hidden at margin 0 and the margin setting solves it, as intended;
subtitles work, and after a rename the automatic load stops while loading by hand works; rename showed no
"cancelled"; background playback with the times is fine. Which items of the list were run was not stated.

| # | Request | Reading and decision | Task |
|---|---|---|---|
| 18 | File actions: rename, delete, move, copy. | Move and copy to a folder that already holds media; no "new folder". Spec 13 item 1. | R7-02, R7-30, R7-41 |
| 19 | A long press on a file or folder starts selection mode; taps select more; the same actions on the selection. | A selected folder stands for its media files; rename only for one file. Item 3. | R7-41 |
| 20 | A setting so that Home keeps playing without the small window. | The two switches already allowed it (window off, background sound on); they become one choice with three values. Item 13. | R7-50 |
| 21 | Rename the app to "AV Player" (GitHub repository AV-Player). | Visible name only; the application id stays, so the update keeps the data. No git remote is set and nothing is pushed. Item 14. | R7-60 |
| 22 | Green icon, smaller play symbol. | Green #2E7D32, symbol at about 60%. Item 15. | R7-60 |
| 23 | Smaller items in list and grid (up to 3 in a row), minding the text size. | Setting "Item size": Large, Small. Item 5. | R7-01, R7-10, R7-40 |
| 24 | In the explorer nothing but the file name is needed. | Details off by default and switched off once on a phone with the old default; the setting stays; NEW, Watched and the progress bar stay. Item 6. | R7-01, R7-10, R7-40 |
| 25 | Thickness of the progress line, thinner and thicker. | 1 to 12 units. Item 7. | R7-10, R7-20 |
| 26 | Split the texts of the quiet display so they cover the video as little as possible. | A corner for each of times, clock, battery. Item 8. | R7-10, R7-20 |
| 27 | Ask before going back to the start (pressed by mistake instead of play/pause). | A question on Previous when it would restart the file. Item 9. | R7-20 |
| 28 | The player does not keep the screen on; it must while the video plays on screen, also in the small window. | Fault. Item 10. | R7-20 |
| 29 | A double tap in the middle pauses or resumes. | Middle third of the width. Item 11. | R7-20 |
| 30 | The bottom part of the controls is too high. | About two thirds of the height. Item 12. | R7-20 |
| 31 | Deleting a file deletes its subtitles too. | Also on move and copy. Item 2. | R7-30 |

## Gates

| Phase | Build | Tests | Reviewer | Owner device test | Status |
|---|---|---|---|---|---|
| 0 Spike | green (spike) | n/a | n/a | pending | go on emulator, waiting for owner device test (docs/SPIKE.md) |
| 1 | green (Debug and Release) | 349 pass (307 Core, 42 Data) | every merged task approved, except P1-27 (tests only, Sonnet, checked by the orchestrator) | passed on 2026-11-15 (owner report: all checklist items pass) | PASSED |
| 2 | green (Debug and Release) | 900 pass (662 Core, 238 Data) | every merged task approved; engine and subtitle service had a gate review and a second round | passed on 2026-11-15 (owner report: Samsung Galaxy A55, 0 failed items, ARM-1 and ARM-2 pass) | PASSED |
| 3 | green (Debug and Release) | 1340 pass (989 Core, 351 Data) | every merged task approved; engine, surface, gestures, theming, localization and coordinator had a gate review | 2026-11-15: "no faults found so far" (full checklist run not stated); eleven requests, see spec 11 | superseded by revision 2 |
| 3, revision 2 | green (Debug and Release) | 1634 pass (1104 Core, 530 Data) | every merged product task approved; R3-T2 (tests only, Sonnet) checked by the orchestrator | 2026-11-15: four items reported (fixed in revision 3), three readings approved; a full run of the list was not stated | not closed on its own: tested together with Phase 4 by the owner's decision |
| 4 (with revision 3) | green (Debug and Release) | 1728 pass (1198 Core, 530 Data) | every merged product task approved; engine, surface, media service, session and Picture-in-Picture had a gate review (two or three rounds each); P4-T1 (tests only, Sonnet) checked by the orchestrator | 2026-11-15: three faults and one wish (fixed in revision 2); a full run of the list was not stated; focus, buttons and Bluetooth not reported | superseded by revision 2 |
| 4, revision 2 | green (Debug and Release) | 1728 pass (1198 Core, 530 Data) | R5-01, R5-02 gate review: approve | 2026-11-15 19:10: "Everything is fine" (which items were run was not stated; focus, buttons and Bluetooth not reported) | PASSED by the owner's report |
| 5 (with the quiet display, spec 12) | green (Debug and Release) | 1892 pass (1310 Core, 582 Data) | R6-10 approved; R6-20, P5-10, P5-20 changes required, fixed by the agents, the fixes not reviewed again; tests-only tasks checked by the orchestrator; P5-50 not reviewed (one registration line) | 2026-11-15 20:21: no fault in rename, subtitles, background playback; one fault (the screen turns off) and thirteen requests; a full run of the list was not stated | superseded by revision 2 (spec 13) |

## Section 9 checklist

| Step | Item | Status |
|---|---|---|
| 1 | Read MASTER.md | done |
| 2 | Repo structure, copilot-instructions, PROGRESS.md | done |
| 3 | Phase 0 spike and docs/SPIKE.md | done on emulator, owner device test pending |
| 4 | docs/ARCHITECTURE.md and frozen contracts | done, contracts frozen 2026-11-14 |
| 5 | docs/specs/01-09 and docs/agents/<id>.md | done: 9 specs, 23 agent specs, 23 `.github/agents/<id>.agent.md` with model set |
| 6 | Roster shown to owner, approval | approved 2026-11-14 |
| 7 | Start Phase 1 | in progress |

## Delegation mechanism

The custom agents in `.github/agents` are available in the `task` tool (they appeared after the session was
resumed on 2026-11-14 evening; P1-03 to P1-07 were first launched through `general-purpose` with the roster
model set explicitly, before that). Tasks are delegated to the custom agent by name, without a model override.
The brief is the agent's spec file plus the contracts and acceptance criteria. Fable-marked agents are done by
the orchestrator.

## Phase 1 task log

| # | Agent | Model | Branch | Attempts | Result |
|---|---|---|---|---|---|
| P1-01 | app-skeleton | Haiku 5.5 | task/p1-01-app-skeleton | 1 | merged. Reviewer: approve. Build and tests green. Brief error by orchestrator: manifest check must use the Release build. |
| P1-02 | core-contracts | Fable 5.2 (orchestrator) | task/p1-02-core-contracts | 1 | merged. Player.Core builds with 0 warnings and no references. Reviewer pass deferred to the phase gate. |
| P1-03 | app-skeleton | Haiku 5.5 | task/p1-03-app-skeleton | 1 (first run interrupted by a session stop, resumed from its partial work) | merged. Reviewer: approve. |
| P1-04 | media-library | Sonnet 5.5 | task/p1-04-media-library | 1 | merged. Follow-up commit declares READ_MEDIA_VISUAL_USER_SELECTED (orchestrator decision). Not reviewed yet (gate). |
| P1-05 | thumbnail-cache | Sonnet 5.5 | task/p1-05-thumbnail-cache | 1 | merged. 477 lines (over the guideline). Not reviewed yet (gate). |
| P1-06 | playback-coordinator | Sonnet 5.5 | task/p1-06-playback-coordinator | 1 | merged. 452 lines (over the guideline). Not reviewed yet (gate). |
| P1-07 | theming-engine | Sonnet 5.5 | task/p1-07-theming-engine | 1 | merged. Not reviewed yet (gate). |
| P1-08 | player-engine | Fable 5.2 (orchestrator) | task/p1-08-player-engine | 1 | merged. 730 lines (far over the guideline: queue, engine and binding do not compile separately). Reviewer: 2 rounds, 3 blocking findings, all fixed; the last fix was not re-reviewed. Never run on a device. |
| P1-09 | video-surface | Fable 5.2 (orchestrator) | task/p1-09-video-surface | 1 | merged. Geometry for all aspect modes and zoom/pan is in Core. Not reviewed, never run on a device. |
| P1-11 | explorer-ui | Haiku 5.5 | task/p1-11-explorer-logic | 1 | merged. Reviewer: approve (logic traced by hand, no tests yet). |
| P1-12 | player-hud-ui | Haiku 5.5 | task/p1-12-player-hud | 2 | merged. Reviewer: changes-required (unguarded close and error paths), approve after the fixes. |
| P1-13 | explorer-ui | Haiku 5.5 | task/p1-13-explorer-vm | 1 | merged. Reviewer: approve with non-blocking findings, fixed in P1-14 and P1-20. 613 lines. |
| P1-14 | explorer-ui | Haiku 5.5 | task/p1-14-explorer-pages | 1 | merged. Reviewer: approve with non-blocking findings (P1-20). |
| P1-15 | test-writer | Haiku 5.5 | task/p1-15-tests-core-a | 1 | merged. 91 tests pass. Reviewer: approve. 926 lines. |
| P1-16 | test-writer | Haiku 5.5, then Sonnet 5.5 | task/p1-16-tests-core-b | 3 on Haiku (all died on a model stream error with nothing written: the full task twice, then the small P1-16a). Escalated to Sonnet 5.5 for P1-16a, see the escalation log. | P1-16a running on Sonnet |
| P1-17 | app-skeleton | Haiku 5.5 | task/p1-17-wiring | 1 | merged. Reviewer: approve. Release manifest has no network permission. |
| P1-18 | app-skeleton | Haiku 5.5 | task/p1-18-shell-route | 1 | merged. One-line fix of an orchestrator design error (root route `explorer` shadowed `explorer/items`). Gate reviewer: approve. |
| P1-19 | player-engine | Fable 5.2 (orchestrator) | task/p1-19-engine-fixes | 1 | merged. Two defects found on the first emulator run (SPIKE.md findings 11 and 12). Gate reviewer: approve with non-blocking findings (P1-24). |
| P1-20 | explorer-ui | Haiku 5.5 | task/p1-20-explorer-polish | 1 | merged. Reviewer: approve. |
| P1-21 | theming-engine | Sonnet 5.5 | task/p1-21-shell-style | 1 | merged. Gate reviewer: approve. Verified on the emulator. |
| P1-22 | device-qa | Haiku 5.5 | task/p1-22-device-qa | 1 | merged. 60 items. The agent had no shell tool (orchestrator's definition error, fixed); the orchestrator created the folder and committed for it. |
| P1-23 | media-library | Sonnet 5.5 | task/p1-23-library-permissions | 1 | merged. Fixes the gate reviewer's changes-required (API 34 partial access re-requested on every load). Reviewer: approve. Not run on a device. |
| P1-24 | player-engine, video-surface | Fable 5.2 (orchestrator) | task/p1-24-engine-surface-fixes | 1 | merged. Gate review fixes, plus a paused black frame after backgrounding found while testing them (SPIKE.md finding 12). Emulator: 16 of 16 cycles. Not re-reviewed. |
| P1-25 | playback-coordinator | Sonnet 5.5 | task/p1-25-coordinator-fixes | 1 | merged. 11 fixes for the gate reviewer's findings. Reviewer: approve. Next, Prev (restart and previous item) checked on the emulator. |
| P1-16a/b | test-writer | Sonnet 5.5 (escalated) | task/p1-16-tests-core-b | 1 each | merged. Surface geometry, theming, info formatter, sorter and resolver gaps. No product bug found. |
| P1-26 | test-writer | Sonnet 5.5 | task/p1-26-tests-coordinator | 1 | merged. 77 coordinator test cases with fakes, 5 runs in a row green. No product bug found. Core total: 307. |
| P1-27 | test-writer | Sonnet 5.5 | task/p1-27-tests-data | 6 launches, 3 died on the model stream error (the first one silently, overnight) | merged. Split into three launches of one test file each, with a commit after each. 42 tests against a real SQLite file: `DbConnectionProvider` 12, `SettingsRepository` 13, `MediaMetadataMigration` and `MediaMetadataRepository` 17. No product bug found. No separate reviewer pass: the orchestrator read the tests and ran the whole suite. |
| P1-28 | player-engine, video-surface | Fable 5.2 (orchestrator) | task/p1-28-reveal-fixes | 1 | merged. Reviewer of P1-24: changes-required (a seek during a frame reveal), approve after the fix. Emulator: 10 of 10 and 6 of 6 paused background cycles, 10 rapid open/close cycles. |
| publish | orchestrator | - | main at 2826f17 | 3 | Release APK. Attempt 1 (`-r android-x64`) built an APK without LibVLC. Attempt 2 (`-p:RuntimeIdentifiers`) failed to build. Attempt 3 (no RID) is correct and verified on the emulator. See ARCHITECTURE.md section 5. |
| P1-29 | player-hud-ui | Haiku 5.5 | task/p1-29-hud-icons | 1 | merged. `HudIconButton` (a `Path` in a tappable grid) and the HUD layout. Reviewer: approve. Non-blocking, open: a null `IconColor` throws; the dim also runs when the command cannot execute; the decoder label's description hides "HW"/"SW" from TalkBack. |
| P1-30 | app-skeleton | Haiku 5.5 | task/p1-30-icon-splash-abi | 2 | merged. Icon and splash SVG, `colors.xml`, Release `RuntimeIdentifiers` with android-arm. Reviewer: changes-required (the comment in `colors.xml` was missing), fixed. |
| P1-31 | theming-engine | - | - | 0 | not started, moved to Phase 3. Runtime system bar colors are not needed while one theme exists; the API 35 finding is in `docs/agents/theming-engine.md` step 3. |
| publish r2 | orchestrator | - | main at acf3f1c | 1 | Release APK with three ABIs, 105 MB. Checked: `libvlc.so` in each ABI, no network permission, v2 and v3 signatures, runs on API 34 and API 36. |
| P1-32 | device-qa | Haiku 5.5 | task/p1-32-qa-update | 1 | merged. Checklist: 67 items (7 new: HUD-14, HUD-15, APP-3 to APP-5, THEME-2, THEME-3), wording matches the symbols, APK install line. The session again had no shell tool, so the orchestrator made the commit. Text only, read by the orchestrator, no reviewer pass. |

Gate review (2026-11-14, three reviewer runs on main at a21d967): media-library changes-required then approve (P1-23); thumbnail-cache approve; playback-coordinator approve; core-contracts approve (matches docs/CONTRACTS.md member by member); video-surface approve; player-engine fixes approve; theming-engine approve; shell route approve.

Parallel tasks run in git worktrees under `D:\Projects\Ariel Video Player.wt\<task>` so that each keeps its own branch.

## First run of the product (emulator, Pixel 5 AVD, API 34, 2026-11-14)

Verified by the orchestrator, evidence in `docs/evidence/phase1/`: start, permission dialogs, folder list, item list
with lazy thumbnails and natural sort, open a video, HUD show/hide/auto-hide, decoder HW -> SW -> HW with the position
kept, pause/play, slider seek, end of file -> next file, end of the last file, 6 background/foreground cycles with the
picture restored and hardware decoding kept, vertical video, audio file, 8 rapid open/close cycles, Back.

Defects found and fixed: shell route (P1-18), native library load thread and late picture after backgrounding (P1-19).
Not verified: anything on a physical device, HEVC, 4K, 500+ files, API levels other than 34, a Release build on a device.

Phase 1 tasks not finished: the owner's device test.

The stream error is not tied to the model: it hit test-writer on Haiku three times and on Sonnet four times (7 of
13 launches), and no other agent. Its cause is unknown. What worked: one test file per launch, launched in sync mode,
with a commit at the end, so a dead launch loses a few minutes and is simply repeated. A background launch that dies
does not always notify the orchestrator.

Lessons recorded for later briefs:
- An agent that builds with a relative path can build in the main checkout and corrupt its `obj` folder (a crash at startup that a clean build removed). Every brief now gives full paths for build commands.
- Reviewers that build at the same time need separate worktrees.
- A namespace must never have the name of a type that other code uses by its simple name. `Player.Core.Logic.PlaybackState`, `Player.Data.GlobalDefaults` and `Player.Data.UiSettingsStore` (same name as its class) each broke or would have broken sibling code with CS0118. Rule: `Player.Data.<Area>Storage`, `Player.Core.Logic.StateRecording`, `Player.App.Features.DefaultsSettings`, `Player.App.Features.InterfaceSettings`.
- The System.Text.Json source generator sets an `init` property, or a record constructor parameter, that is missing in the JSON to `default(T)`, not to the record's initializer. Checked with a scratch program on 2026-11-15: `{}` gave speed 0 and a null style. Stores never serialize the Core records: they use a private DTO with nullable fields and merge it onto `new T()`.
- Shell routes are flat names (`settings-ui`, `player-speed`). A registered route that starts with another route's name was resolved wrongly in Phase 1 (P1-18).

## Phase 2 task log

Phase 2 scope decisions by the orchestrator:
- Settings whose feature does not exist yet are not shown on the settings pages and keep their stored values: aspect (Phase 3), double-tap skip length and battery (Phase 3), background audio and Picture-in-Picture (Phase 4). The theme row of the settings home arrives in Phase 3.
- Contract change after the freeze (docs/CONTRACTS.md): `IPlayerEngine` gained `TracksChanged`, `CurrentAudioTrackId`, `CurrentSubtitleTrackId`, `AudioDelayMs`, `SubtitleDelayMs`, `SubtitleStyle`.
- `SubtitleService` applies its policy by itself when a file opens (it follows the engine); nobody calls it. External subtitle files are copied as UTF-8 into app data, and the per-file state stores the `file://` URI of the copy.

| Task | Agent | Model | Attempts | Result |
|---|---|---|---|---|
| P2-01 | playback-state-store | Sonnet 5.5 | 3 rounds | merged. `PlaybackStateRecorder`. Reviewer: approve; all traced interleavings correct. Fixed on the way: a failed load can no longer overwrite saved overrides, one pending retry per file, no redundant queued saves. |
| P2-02 | playback-state-store | Sonnet 5.5 | 3 rounds | merged. `playback_state` table, repository, DI. Reviewer: approve. Enum overrides are stored as names and read tolerantly. |
| P2-03 | global-defaults-store | Haiku 5.5 | 3 rounds | merged. Store. Reviewer: changes-required (missing JSON fields lost their defaults, a fault of the brief), approve after the DTO fix. |
| P2-04 | ui-settings | Haiku 5.5 | 2 rounds | merged. Store. Reviewer: changes-required (same JSON finding), approve after the fix. |
| P2-06 | core-contracts, player-engine | Fable 5.2 (orchestrator) | 1 | merged. Contract additions above; `SetSubtitleStyleAsync` restarts the input at the current position; an external subtitle no longer overrides a track chosen after it. Not yet run on an emulator. Open: the bottom margin of the subtitle style is not applied, and the freetype options are unverified. |
| P2-07 | test-writer | Sonnet 5.5 | 1 | merged with P2-06. `FakePlayerEngine` implements the new members. |
| P2-08 | subtitles-audio | Sonnet 5.5 | 1 | merged. `LanguageCodes`, `SidecarMatcher`, `PreferredTrackSelector`. |
| P2-09 | subtitles-audio | Sonnet 5.5 | 1 | merged, not yet run. `SubtitleService`, `SubtitleFileStore`, `SubtitleTextEncoding` (Windows-1255 and other legacy code pages to UTF-8). Gate review and emulator check pending. |
| P2-18 | theming-engine | Sonnet 5.5 | 1 | merged. Styles for RadioButton, CheckBox, Picker, Editor and keyed styles for settings rows and HUD panels. |
| core fix | core-contracts | Fable 5.2 (orchestrator) | 1 | `MediaFileId.ToKey` is culture-invariant (reviewer of P2-02). |
| P2-T4 | test-writer | Sonnet 5.5 | 1 | merged. 94 tests for `UiSettingsStore`. No product bug. |
| P2-T5 | test-writer | Sonnet 5.5 | 1 | merged. 174 tests for the subtitle logic. No product bug. |
| P2-05 | app-skeleton | Haiku 5.5 | 1 | merged. Real stores and the recorder replace the Phase 1 placeholders. Reviewer: approve. Emulator: a file played to 6.7 s, force-stopped and reopened resumed near 6 s, with a progress bar in the list and no NEW tag. |
| P2-10 | app-skeleton | Haiku 5.5 | 1 | merged. Phase 2 services and pages registered (8 lines, checked by the orchestrator). |
| P2-11, P2-22 | global-defaults-store | Haiku 5.5 | 4 rounds, 1 | merged. Playback defaults page and default subtitle style page. Reviewer: changes-required twice (quick taps dropped by `[RelayCommand]` on async methods; a reset that lost its error), then approve. |
| P2-12, P2-23 | ui-settings | Haiku 5.5 | 1, 1 | merged. Interface settings page, settings home, review follow-ups. Reviewer: approve. |
| P2-13a, P2-13b | playback-settings-ui | Haiku 5.5 | 2, 1 | merged. Speed, repeat and A-B, audio sheets. Reviewer: approve. The modal sheet is see-through over the video (SPIKE finding 18). |
| P2-13c, P2-32, P2-35 | playback-settings-ui | Haiku 5.5 | 3 rounds | merged. Subtitle sheet and subtitle style sheet. Reviewer: changes-required (a style could be applied twice), approve after P2-32 (one apply under a gate, a single navigation to the style sheet, A-B draft fixes, a backdrop that makes all five sheets readable over the picture), approve of P2-35 ("Save as default" waits for a running apply). Open, not blocking: after a failed apply "Save as default" refuses until the next successful apply. |
| P2-16, P2-20 | player-hud-ui | Haiku 5.5 | 1, 1 | merged. HUD entry points (three icons, speed label), per-file decoder choice, the HUD hides under a sheet. Reviewer: approve. |
| P2-17 | explorer-ui | Haiku 5.5 | 2 rounds | merged. Settings entry, folder list follows the new-tag age. Reviewer: changes-required (a null value made every settings event reload), approve after the fix. |
| P2-19 | playback-state-store | Sonnet 5.5 | 1 | merged. A failed save is retried on a timer and by `FlushAsync` (found by the test writer). |
| P2-21 | theming-engine | Sonnet 5.5 | 1 | merged. Android light/dark mode follows the app theme: popups, dialogs and radio buttons are dark. Gate reviewer: approve. |
| P2-26 | core-contracts, subtitles-audio | Fable (orchestrator), Sonnet 5.5 | 1 | merged. Contract: `FindSidecarsAsync` returns `SubtitleFile` records with names. |
| P2-27 | player-engine | Fable 5.2 (orchestrator) | 2 builds | merged. Subtitle style as LibVLC instance options (SPIKE findings 14 to 16): before this no style was applied at all. Text direction automatic. Margin in video pixels. Emulator: all options visible, live style change restarts once and keeps the position. |
| P2-28 | subtitles-audio | Sonnet 5.5 | 1 | merged. Legacy code page from the file name and from the bytes (a Windows-1255 file showed Latin garbage on an English device). Emulator: Hebrew shown. |
| P2-30, P2-31 | core-contracts, player-engine, subtitles-audio | Fable (orchestrator), Sonnet 5.5 | 1, 1 | merged. Gate review of the engine: changes-required (an external subtitle chosen by its id was lost on a restart; a style change while paused left a black picture). Gate review of the subtitle service: changes-required (a slow load could reach the next file; a failed restore stopped the policy; replay added the file twice; one bad byte turned UTF-8 into garbage; I/O on the caller's thread). All fixed. Contract: `AddSubtitleFileAsync` takes a display name, `ExternalSubtitlePath` added. |
| P2-33 | player-engine | Fable 5.2 (orchestrator) | 1 | merged. Found in the integration run: a restart of a paused file lost its position (0 after the second restart) and kept the old picture (SPIKE finding 19). The reveal added in P2-30 never ran, because LibVLC reports such an input as Playing and then Paused. Fixed with a clock flag; verified with engine traces. |
| P2-36 | player-engine | Fable 5.2 (orchestrator) | 1 | merged. Second review round of the branch: approve, no blocking finding. Four of its notes fixed (a stale event after a restart, a file added twice while opening, a missing copy still reported, Stop). Open, not blocking: a subtitle load that crosses an open of the next file can land on the new file (needs the engine to know the item: contract change); the name match of the external track assumes the store's 16-hex names. |
| P2-34, P2-37 | device-qa | Haiku 5.5 | 2 rounds, 1 | merged. `docs/qa/phase-2-checklist.md`: 53 Phase 2 items and the 67 Phase 1 items as a regression block. The agent stopped once, correctly, on two spec conflicts (skip length hidden in Phase 2; who writes global defaults); specs 03 and 06 were amended. |
| P2-T1, T2, T3, T6, T7, T8 | test-writer | Sonnet 5.5 | 1 each | merged. Recorder 59 + 11 retry tests, state repository 42, defaults store 60, text encoding and theme brightness 94, fake engine contract and tolerant UTF-8 17. No product bug. One finding: a failed save was not retried without a new event (fixed in P2-19). Total on main: 900 tests (662 Core, 238 Data). |

Findings on the emulator in Phase 2 that changed the plan: SPIKE findings 14 to 20. Not possible on the platform: an `.ass` or `.ssa` file next to the video is not listed by MediaStore on API 34 for an app with only the media permissions, so it is opened with the file picker.

## Phase 2 verification (2026-11-15)

Run by the orchestrator, on emulators only. No physical device was used.

| Target | Build | Result |
|---|---|---|
| Pixel 5 AVD, API 34, x86_64 | Debug (integration of all Phase 2 branches) | Pass: sidecar `.srt` loads by itself; Hebrew right to left with the punctuation on the left; Windows-1255 decoded; external track shown by its file name; style change while playing and while paused (position kept, new picture drawn); margin from 0% to 30%; decoder switch while paused keeps the external track; repeat "This file" keeps one subtitle track; `.ass` through the system file picker; per-file speed and decoder; resume after force-stop; a watched file starts at 0. |
| Pixel 5 AVD, API 34, x86_64 | Release APK, installed over the Phase 1 r2 APK | Pass: the update installs and keeps the app data; all five sheets; "Save as default"; settings home, playback defaults, interface settings; the clock switch reaches the HUD; default decoder Software applies to a new file; resume after force-stop. |
| Pixel 7 AVD, API 36, x86_64 | Release APK | Pass, two runs: pause, subtitle sheet, style sheet, style change, two decoder switches, back. |
| Pixel 7 AVD, API 36, arm64 through the image's ARM translation | Release APK | Playback, subtitles and three paused style changes worked once. Then it failed in two ways, see below. |
| armeabi-v7a | - | Never run (no image runs it). |

The arm64 failures on the ARM translation (SPIKE finding 20):
- The app stops responding when sheets are opened while the video is paused: 8 of 10 paused runs, also with the Speed sheet alone (4th opening), so it does not depend on the subtitle code. The ANR dump shows the .NET threads stopped for a garbage collection and never resumed.
- Two crashes (SIGSEGV) inside the translator's JNI trampoline (`berberis::ToHostJNIEnv`), on a LibVLC audio thread.
- The same APK's x86_64 half on the same emulator passes the same steps. The Phase 1 APK's arm64 half, paused and sent four rotation requests, did not fail, but it has much less UI to open while paused, so it is a weak control.
- Assessment: the translator does not run a signal handler of a thread that is blocked in a system call, and the .NET runtime stops its threads with signals, so a paused player (LibVLC threads blocked for good) cannot finish a garbage collection there. A real ARM kernel delivers the signal. This is a reasoned explanation, not a proof: it has not been run on a real arm64 device. The checklist starts with ARM-1 and ARM-2 for this.

Not verified at all in Phase 2: pitch at 0.25x and 4.0x and the audio delay by ear (the emulator runs without audio), a file with several audio tracks or embedded subtitle tracks (no such test file), the preferred language defaults on a real file, A-B repeat on the emulator (unit tests only), repeat "Folder" wrap-around on the emulator (unit tests only).

## Phase 3 task log (started 2026-11-15)

Scope decisions by the orchestrator:
- Owner request: a Hebrew interface as an option. Spec 10 (`docs/specs/10-localization.md`), a new agent `localization` (Sonnet 5.5; spec and `.github/agents/localization.agent.md` written; the agent type is not available to the `task` tool until the session restarts, so it runs through `general-purpose` with the roster model set). Texts live with their module: one `IStringSource` class per module, keys `<Module>.<Name>`. `UiSettings.Language`: System (default, follows the device), English, Hebrew. The language changes at once; the direction is set on the window.
- The transport bar of the player is never mirrored (left to right in every language); symbols are not mirrored except the back arrow.
- The window of the player (immersive bars, brightness) belongs to video-surface: `IVideoSurface.SetImmersive`.
- LibVLC is told to fill the surface exactly (aspect ratio = surface size), so the surface view alone decides the shape of the picture. This makes Stretch work without a contract change to the engine.
- The coordinator applies the resolved aspect mode when it opens an item (`IVideoSurface.SetAspect`).
- Top bar of the player: Back, title with a status line (clock and battery), Subtitles, Audio, Repeat, Aspect, Lock.
- The battery level uses MAUI's `Battery` API; the HUD module declares the permission it requires (`BatteryStats`) with an assembly attribute in its own path (ARCHITECTURE section 2).
- The first page is created only after the saved theme and language are applied (no flash of the wrong theme or language).

| Task | Agent | Model | Attempts | Result |
|---|---|---|---|---|
| P3-01, P3-05 | player-engine, video-surface | Fable 5.2 (orchestrator) | 1, 1 | merged. Owner feedback 6. The video format is read again while the first pictures are shown (LibVLC raises no event when the decoder corrects the track size; before, the next read came only with a pause or a play). The surface keeps its shape when the engine stops, so the last picture is not stretched while the page closes. `SetImmersive`. LibVLC fills the surface. Gate review: approve; its notes fixed in P3-05 (no forced aspect while the format of a new file is unknown, the aspect is sent only when it changes, audio files skip the format checks). The 99% picture could not be reproduced on the emulators (1080p file, portrait and landscape, HW and SW): the fix follows from the code path, not from a reproduction. |
| P3-02 | player-hud-ui | Haiku 5.5 | 1 | merged. Owner feedback 7. Bottom bar left to right, `HudIconButton.MirrorInRightToLeft`. Reviewer: approve. Emulator with a Hebrew app locale: transport row in place, back arrow mirrored. |
| P3-03 | core-contracts | Fable 5.2 (orchestrator) | 1 | merged. Contracts: `SetImmersive`, `AppLanguage`, `UiSettings.Language`, `LocalizedText`, `ILocalizer`, `IStringSource`. Spec 10, agent spec. |
| P3-10 | gestures | Sonnet 5.5 | 2 rounds | merged. `GestureClassifier` (pure), overlay view, `GestureSource`. Gate review: approve; follow-ups done (reset on detach, off-axis delta zero, subscriber exceptions caught). |
| P3-T1 | test-writer | Sonnet 5.5 | 2 rounds | merged. 125 classifier tests. No product bug. |
| P3-20a, b, c, P3-21 | theming-engine | Sonnet 5.5 | 2 launches (the first died on a stream error with nothing written), then 4 steps | merged. Accent palette, token resolver, `ThemeSettingsNormalizer`, `ThemeSettingsRepository`, runtime swap, system bars, shared styles, theme page (route `settings-theme`). Gate review: changes-required (accent-colored text on the background fails the contrast rule in Light), fixed in P3-20c, second round: approve. P3-21: on API 36 the strip behind the status bar stayed black in Light (the `AppBarLayout` background is a static theme color); every app bar is now colored with the theme, also for pushed pages. Verified on the API 36 emulator. |
| P3-30, P3-31, P3-32 | player-hud-ui | Haiku 5.5 | 3 rounds | merged. Gesture handling with OSD (tap, double tap, volume, brightness, seek, zoom, pan), immersive window, new top bar, aspect button, screen lock, battery, texts. Reviewer: changes-required (the lock was released when the app went to the background), approve after P3-32. |
| P3-40, P3-41 | playback-coordinator | Sonnet 5.5 | 1, 1 | merged. Aspect mode applied on open; not for a cancelled open. Gate review: approve. |
| P3-50, P3-51, P3-52 | app-skeleton | Haiku 5.5 | 2 rounds, 1, 1 | merged. Gestures and localization wired; the first page is created after the saved theme and language are applied (at most 5 s); the activity applies the system bar colors. Reviewer: approve for each. |
| L10N-01, L10N-02, L10N-03 | localization | Sonnet 5.5 | 3 rounds | merged. `StringCatalog`, `Localizer`, `{loc:T}`, common texts. Gate review: changes-required (a quick sequence of language changes could end on the wrong one), fixed in L10N-02, approve. L10N-03: language names keep their side in either direction (direction marks), subscribers isolated, a missing Hebrew text falls back to English. Approve. |
| L10N-T1, L10N-T2 | test-writer | Sonnet 5.5 | 1, 2 rounds | merged. 69 catalog tests; 22 tests that read the string source files and the XAML and C# of the app (every key used exists, no duplicates, sorted, both languages present, same placeholders, counted texts in pairs). |
| L10N-11 | explorer-ui | Haiku 5.5 | 2 rounds | merged. Explorer texts. Reviewer: approve. |
| L10N-13, L10N-16 | playback-settings-ui | Haiku 5.5 | 3 rounds | merged. Sheet texts. Reviewer: approve; Hebrew corrections applied. |
| L10N-14 | global-defaults-store | Haiku 5.5 | 3 rounds | merged. Defaults pages texts, default aspect row shown. Reviewer: approve; Hebrew corrections applied. |
| L10N-15, L10N-17 | ui-settings | Haiku 5.5 | 2 launches (the first died on a stream error after its first commit), 4 steps, 1 | merged. Language stored and chosen on the Interface page, skip length, battery switch, Theme row, texts. Reviewer: approve. L10N-17 (found by the test writer): a stored field of the wrong JSON kind lost the whole row; each field now falls back alone. Reviewer: approve. |
| P3-T2, T3, T4, T5 | test-writer | Sonnet 5.5 | 1 each | merged. Theming (resolver rules, accent palette, normalizer, repository: 187), coordinator aspect and catalog null texts (36), UI settings language (41, one product bug found: L10N-17), catalog English fallback. Total on main: 1340 tests (989 Core, 351 Data). |
| P3-60 | device-qa | Haiku 5.5 | 2 rounds | merged. `docs/qa/phase-3-checklist.md`: 53 Phase 3 items, 53 Phase 2 and 67 Phase 1 items as regression blocks. |

Emulator findings so far (API 34, x86_64): the Hebrew layout works (right to left, texts, counted texts); immersive player; volume, brightness and seek gestures with their OSD; Stretch, Crop and 100% while playing and while paused, with the hardware and the software decoder; pinch zoom works. Known limit: an aspect change after the file has ended shows a wrong picture until playback starts again (nothing renders a new frame).
Android shows its own "Viewing full screen" hint the first time the player hides the system bars.

## Phase 3 verification (2026-11-15)

Run by the orchestrator, on emulators only (x86_64). No physical device was used.

| Target | Build | Result |
|---|---|---|
| Pixel 5 AVD, API 34 | Debug (integration of the Phase 3 branches) | Pass: Hebrew layout and texts with the device seen as Hebrew; live language change on the Interface page (the page, the settings home and the explorer follow); player top bar mirrored and bottom bar unchanged in Hebrew; immersive player; tap, double tap (10 s forward and back), volume, brightness and seek gestures with their indicators; pinch zoom and pan (the picture stops at its edge); Stretch, Crop and 100% while playing and while paused with the hardware and the software decoder; lock (taps, drags and Back blocked, unlock button, still locked after HOME and return); sheets in Hebrew; theme page and the Light theme on every page. |
| Pixel 5 AVD, API 34 | Release APK, installed over the Phase 2 APK | Pass: the update keeps the data; language switch at once (the `{loc:T}` bindings work in the trimmed build); theme page in Hebrew, Dark Slate applied; player with the volume indicator, Stretch while paused. |
| Pixel 7 AVD, API 36 | Release APK | Pass: Light theme, explorer, immersive player, aspect, a file with two audio tracks and two embedded subtitle tracks (both listed and selected in the sheets: first run with such a file), brightness indicator, double tap. Found: the strip behind the status bar stayed black in Light (fixed in P3-21, verified with a Debug build on the same emulator, then published again). |
| arm64-v8a, armeabi-v7a | - | Not run. The emulator's ARM translation cannot run the paused player (SPIKE finding 20); the Phase 2 build ran on the owner's arm64 phone. |

Not verified at all in Phase 3: the owner's 99% picture on real hardware (not reproducible on the emulators: the fix follows from the code path); the stretch on exit as the owner saw it (the emulator showed a shifted, not a stretched, last picture before the fix and an unchanged one after it); sound above 100% and real brightness (the emulator has no audio and no backlight); sharpness at 400%; gesture recognition by a hand (10 of 10); a real Hebrew phone; landscape with a camera hole; the lock across a real screen-off.

## Phase 3 revision task log (2026-11-15, owner requests, spec 11)

Scope decisions by the orchestrator:
- The requests are sorted by the phase whose module they change and were built in that order: contracts first, then stores and library, then the player, then the explorer.
- "Move the speed and the decoder up" together with "decoder in the menu": the speed is a chip in the top bar, the decoder is an item of the top-bar menu.
- Folder options open from a three-dot button on the folder's row or tile, not from a long press (MAUI has no long-press recognizer for list rows without platform code). File options join the same pattern in Phase 5.
- "File browser" is the tree of the media folders by their path, one level at a time. It is not a browser of every directory of the storage: the app reads MediaStore only.
- A new agent, `folder-settings-store` (Haiku 5.5), owns the per-folder store. Like `localization`, it is registered in `.github/agents` but not available to the `task` tool until the session restarts, so it ran through `general-purpose` with the roster model.
- The device checklist of this round is its own file (`phase-3-r2-checklist.md`), so the owner runs only what changed.

| Task | Agent | Model | Result |
|---|---|---|---|
| R3-01, R3-02 | core-contracts, video-surface | Fable 5.2 (orchestrator) | merged. Contracts (see `docs/CONTRACTS.md` change log): `ExplorerViewMode` Tree and Favorites, `ExplorerLayout`, `GestureKind.LongPress`, `MediaFolder.AudioOnly`, `FolderSettings`, `IFolderSettingsStore`, `EdgeInsets`, four `UiSettings` fields, the resolver's folder argument, `IVideoSurface.SetSystemBarsVisible` and `SystemBarInsets`. Player mode: the window is edge to edge, the bars are shown and hidden inside it. Reviewer: approve; follow-ups in R3-02. |
| R3-10 | gestures | Sonnet 5.5 | merged. Long press after 500 ms without movement. Reviewer: approve. |
| R3-11, R3-15 | ui-settings | Haiku 5.5 | merged. Four stored fields, three rows on the Interface page; every stored field falls back alone; Hebrew texts. Reviewer: approve. |
| R3-12 | folder-settings-store | Haiku 5.5 (through `general-purpose`) | merged. `FolderSettingsStore` on the settings table, `AddFolderSettings()`; a follow-up keeps the loaded map. Reviewed and approved. |
| R3-13 | media-library | Sonnet 5.5 | merged. `MediaFolder.AudioOnly`. Reviewer: approve. |
| R3-14 | playback-coordinator | Sonnet 5.5 | merged. The folder's default speed is read on open. Reviewer: approve. |
| R3-50 | app-skeleton | Haiku 5.5 | merged. `AddFolderSettings()` in `MauiProgram.cs`. Reviewer: approve. |
| R3-20, R3-21 | player-hud-ui | Haiku 5.5 | merged. Title line, second line with status, speed chip, Aspect, menu, Lock; no Back button; bottom bar with seek row and transport; menu; bars follow the controls; seek preview; long-press speed. Reviewed and approved. |
| R3-22 | player-hud-ui | Haiku 5.5 | merged. The player page ignores the safe area (found by the orchestrator on API 36, see below). Reviewer: approve. |
| R3-30, R3-31, R3-32 | explorer-ui | Haiku 5.5 | merged. `FolderTree`, view modes, folder options, favorites, list and grid, name size, audio-only filter. Reviewer: changes-required (the file browser stayed inside a directory that had disappeared), approve after R3-31; R3-32 closes one more note (the audio-only filter uses the newest settings). |
| R3-03, R3-04 | video-surface | Fable 5.2 (orchestrator) | merged. Up to API 34: the bars are see-through with white symbols in player mode and the theme's look returns afterwards; the bar padding MAUI left on the pages under the player is cleared. Gate review: changes-required (the activity puts the theme look back on resume), approve after R3-04 (player mode is applied again when the window gets the focus). |
| R3-T0, R3-T1, R3-T2 | test-writer | Sonnet 5.5 | merged. Resolver with folder settings and fakes; long press, `AudioOnly`, coordinator folder speed, folder store, UI settings fields; `FolderTree` (45 cases). No product bug. Total on main: 1634 tests (1104 Core, 530 Data). |
| R3-60 | device-qa | Haiku 5.5 | merged. `docs/qa/phase-3-r2-checklist.md`: 15 explorer, 7 settings and 21 player items. |

Faults found by the orchestrator's own emulator runs in this round, all fixed before the APK:
- The picture shrank whenever the controls and the bars appeared (API 36, landscape, 4:3 video: 1080 px to 954 px). MAUI pads the children of the page's root layout by the safe area. The first check, in portrait with a 16:9 video, could not show it. Fixed in R3-22. SPIKE finding 27.
- Up to API 34 the bars were painted in the theme color over the top and bottom of the picture, and in the Light theme their symbols were dark on the black player. Fixed in R3-03.
- Up to API 34 the file list kept an empty band above the navigation bar after the player was closed (list height 1945 px before, 1879 px after). Introduced in this round by the edge-to-edge switch of R3-01; never in an APK the owner has. Fixed in R3-03. SPIKE finding 28.
- The folder symbol of the grid tile was not drawn and the options button had a border. Fixed in R3-31.

## Phase 3 revision verification (2026-11-15)

Run by the orchestrator, on emulators only (x86_64). No physical device was used.

| Target | Build | Result |
|---|---|---|
| Pixel 5 AVD, API 34 | Debug (integration builds) | Pass: view sheet with the four modes; file browser (levels, Back goes up, a directory that disappears sends the browser to its parent); folder options, favorite star, Favorites mode; folder speed; list and grid for folders and files, four columns in landscape; audio-only switch in every mode; Interface page rows; explorer and player in Hebrew (menu, folder options, top bar mirrored, bottom bar unchanged); the lock hides the controls and the bars. Light theme: white symbols in the player, also after Home and return; the light bars with dark symbols are back after Back. |
| Pixel 5 AVD, API 34 | Release APK, installed over the Phase 3 APK | Pass, on the final build: the update keeps the data (a played file keeps its state). Folder speed 1.5x on a new file of that folder; file browser; long press shows "Speed 2.0x"; the seek drag shows the target frame; menu with the four items. Landscape, 4:3 video: the picture is 1080 px high with the controls hidden, shown and hidden again; the bars lie over it, see-through. The file list has its full height after the player, in portrait and landscape, on every repeat. On the first Release build of this round: the "All files" mode of the Phase 3 app is kept, favorites, Favorites mode, decoder switch from the menu, playback continues after a seek drag. |
| Pixel 7 AVD, API 36 | Debug (integration builds) | Pass: the picture keeps its size after R3-22; Hebrew player in landscape; lock and unlock in landscape. |
| Pixel 7 AVD, API 36 | Release APK | Pass, on the final build: the picture keeps its size in landscape with the controls hidden, shown and hidden; each sheet opens from the menu (a file with two audio and two subtitle tracks); a seek drag while paused stays paused on the target; Light theme with white symbols in the player and the light bars back afterwards; file list height unchanged after the player; Interface page in Hebrew with the three new rows. On the build before the last surface fix: folder grid, file grid, player with controls in portrait and landscape, the Speed sheet over the unmoved picture. |
| arm64-v8a, armeabi-v7a | - | Not run (SPIKE finding 20; the Phase 2 and Phase 3 builds ran on the owner's arm64 phone). |

Not verified at all in this round: the seek preview on a long file with far-apart key frames (the test files are 12 to 20 seconds); the long press by a real finger (a still finger moves a little; the slop decides); a real camera hole in landscape (the emulators report no cutout); the bars on Samsung's One UI; grid thumbnails on a folder with hundreds of files; folder options and favorites with a real library; the Hebrew texts on a Hebrew phone; items EXP-13, EXP-15, EXP-18, EXP-21, EXP-23, SET-3 to SET-7, PLR-5, PLR-9, PLR-13, PLR-15 to PLR-18 of the checklist, which were not run as written.

## Phase 4 and revision 3 task log (2026-11-15)

Scope decisions by the orchestrator (details in spec 08 and spec 09, "Decisions made while building Phase 4"):
- The service is a thin Android service; a controller singleton decides on the UI thread and sends its commands in one ordered chain.
- "Foreground" for `IAppLifecycle` is "started", so Picture-in-Picture counts as foreground.
- The service is not stopped when a file ends in the background (it could not start again from there).
- From API 33 the close button of the media control is a custom action of the session.
- Picture-in-Picture wins over "pause on leave" when only its setting is on.
- A folder's default speed clears the own speeds of the folder's files (owner feedback 8).
- No new agent. `media-service` is the orchestrator's own code (Fable), as are the engine and surface changes.

| Task | Agent | Model | Result |
|---|---|---|---|
| R4-01, R4-03 | player-engine | Fable 5.2 (orchestrator) | merged. Owner feedback 10. Seeks name their kind (fast or exact) through the native function; a paused seek is fast at once, exact after 350 ms, and completes when its picture is shown. Gate review: approve; follow-up: no picture wait without a surface. |
| R4-02, R4-03 | video-surface | Fable 5.2 (orchestrator) | merged. Owner feedback 11. The picture area starts below the status bar. Gate review: approve; follow-ups: pan limits use the same area, the inset is cleared with player mode. |
| R4-10 | explorer-ui | Haiku 5.5 | merged. Owner feedback 8 and 9. Four direct view buttons; a folder's speed clears the own speeds of its files. Reviewer: approve. |
| R4-20, R4-21 | player-hud-ui | Haiku 5.5 | merged. Paced seek drag; Picture-in-Picture button; controls hidden in the small window; the page leaves itself after a close from outside. Reviewer: changes-required (a finished older drag could disturb a newer one; a stale cancellation token lost the first target of a new drag; a failed leave left Back dead), approve after R4-21. |
| P4-01, P4-02, P4-03 | media-service | Fable 5.2 (orchestrator) | merged. Playback service, notification, audio focus, headphones receiver, background audio. Gate review, three rounds: changes-required (the picture stayed switched off for the next file after a close in the background), fixed in P4-02 with ten more notes; changes-required again (my new rule stopped the service at every file end in the background, which would have broken lists and repeat from API 31), fixed in P4-03; approve. |
| P4-10, P4-11, P4-12 | media-session-bt | Sonnet 5.5 | merged. Media session, state mapper, publish gate, multi-press detector, close action. Gate review: changes-required (a press group could be lost when the timer fired a moment early, turning a later single press into "next"), approve after P4-11; P4-12 approve. |
| P4-20 to P4-23 | pip | Sonnet 5.5 | merged. Picture-in-Picture controller. Gate review, three rounds: changes-required (the close rule was inverted; the decision depended on a global that can be empty), again changes-required (the order "mode change, then stop" closed nothing), approve after P4-22; P4-23: an expanded window followed by Home is not a dismissal. |
| P4-30 | global-defaults-store | Haiku 5.5 | merged. The two settings on the defaults page, the notification permission request. Reviewer: approve. |
| P4-50 | app-skeleton | Haiku 5.5 | merged. Wiring, activity forwarding, lifecycle = started, placeholder removed. Reviewer: approve. |
| P4-T1 | test-writer | Sonnet 5.5 | merged. 94 tests for the session logic. No product bug (the detector fault above needs an early timer, which the fake clock does not produce). Total on main: 1728 tests (1198 Core, 530 Data). |
| P4-60 | device-qa | Haiku 5.5 | merged. `docs/qa/phase-4-checklist.md`: 12 fix items, 12 background, 4 focus, 8 buttons, 10 Picture-in-Picture, 5 regression items, and the list of earlier lists to run. |

## Phase 4 verification (2026-11-15)

Run by the orchestrator, on emulators only (x86_64). No physical device, no headset, no Bluetooth, no second audio app and no sound (the emulators run without audio) were used.

| Target | Build | Result |
|---|---|---|
| Pixel 7 AVD, API 36 | Debug (main) | Pass: the app starts with the three modules; while playing the service is in the foreground with the type media playback, the session is active with state, position, title and folder, the notification has four actions. Background audio off: Home pauses. On: the position advances after Home, the media key pauses and plays, the picture is back after the return. Picture-in-Picture from the button: small window, pause and play from it, expand keeps playing, dismissing closes playback and the app shows the file list. "Picture-in-Picture when leaving": Home while playing opens the small window; Home from the file list and Home while paused do not. System media control: title, folder, artwork, seek bar, play, and the close cross; play and close from it work with the app in the background, and the app returns to the file list. Folder speed against a file with its own speed (FIX-1 to FIX-3). Seek drag on the 10-second-key-frame file: the picture follows by key frames and settles on the exact time. |
| Pixel 7 AVD, API 36 | Release APK, installed over the revision 2 APK | Pass: the update keeps the data; four view buttons; service and session while playing; seek drag (key frame at once, exact frame after a rest, playback continues from the target); Picture-in-Picture and expand; Home pauses with both settings off. With background audio on (Release, fresh install): the position advances in the background; after the return the picture is back within about a second and matches the position (compared by the burned-in time code, also after a pause and play by media key in the background). |
| Pixel 5 AVD, API 34 | Release APK | Pass: service and session; landscape 4:3 video: the picture is rows 66 to 1079 with the controls hidden, shown and hidden (it starts below the status bar and does not move); file list height unchanged after the player; Back stops the service; Picture-in-Picture and expand; the four view buttons in English and Hebrew, each mode. |
| arm64-v8a, armeabi-v7a | - | Not run. |

Not verified at all: sound in the background (only the position was seen to advance); 30 minutes with the screen off; battery managers of One UI; audio focus (another app, a call, ducking); headphones unplugged; headset and Bluetooth buttons, single, double and triple press; a car or speaker display; the notification on Android 12 or lower; the permission dialog; next and previous from the notification and from the small window; Picture-in-Picture with a vertical video, rotation, ten rounds, the screen turning off in it, the dismissal order on One UI; closing by swiping the app away and the saved position after it; force stop; the ten-minute stop; the picture below the status bar in Crop, Stretch and zoom; a paused seek drag (FIX-10); the seek drag on real films and with the software decoder; whether the fast-seek argument behaves the same in the arm64 and 32-bit libraries (the reviewer's reading says a library that ignores it is not harmed).

## Phase 4 revision 2 (2026-11-15, owner feedback 12 to 15)

| Task | Agent | Model | Result |
|---|---|---|---|
| R5-01, R5-02 | player-engine, core-contracts | Fable 5.2 (orchestrator) | merged. The picture returns by a restart of the input; ordered surface release with a bounded wait on the UI thread (accepted exception to the "never block the UI thread" rule: Android keeps the surface valid only until the callback returns); the rate is set for every input; seek preview 60 ms and 250 ms; both background settings on by default. Gate review: approve; its note fixed in R5-02 (a restart while paused uses the published position). |
| R5-60 | device-qa | Haiku 5.5 | merged. `docs/qa/phase-4-r2-checklist.md`, 10 items. |

Verification, emulator only (Pixel 7 AVD, API 36, x86_64; Debug build, then the Release APK on a fresh installation): screen off for 10 s and on while playing: the position runs on, the input restarts at the position, the picture is complete from its first frame (0.7 to 1.6 s after the return, with MP4 and MKV, key frames 10 s apart); Home and return with Picture-in-Picture off: the same; Home and return while paused: same position, the picture is there (it moves on by about 0.2 s); Picture-in-Picture enter and expand: no surface loss, no restart; rotation: no restart; a 1.0x file after a 2.0x file plays at 1.0x; seek drag as before; the defaults page shows both switches on; Home with the defaults opens the small window. Not run: API 34; the update over the Phase 4 APK (the stored choice is kept by the store's merge, read in the code); sound; everything on a phone. The owner's broken picture itself was never seen here.

## Phase 5 and the quiet display (2026-11-15, owner requests 16 and 17)

| Task | Agent | Result |
|---|---|---|
| R6-01 | core-contracts (orchestrator) | merged. `UiSettings.Overlay`, three enums, `PlaybackOverlayText`, spec 12. |
| R6-10 | ui-settings | merged. Store fields (the color as 8 hex digits, because an existing test forbids "42" in the row) and the group "While the controls are hidden". Review: approve. "Theme accent" as a text color left out. |
| R6-20, R6-21 | player-hud-ui | merged. The display and `-remaining / total` in the seek row. Review: changes required (clock aligned to the minute, fill by scale, no work while hidden), fixed. The clock is `HH:mm` like the HUD clock, not the phone's 12/24 h setting. The display stays on while the screen is locked. |
| P5-10, P5-11 | file-ops | merged. Rename and trash through MediaStore, consent through the activity result registry, the saved state follows a rename and is deleted on trash. Review: changes required (result after a committed write, UI-thread call, duplicate check), fixed. |
| P5-20 to P5-22 | explorer-ui | merged. ⋮ on file rows and tiles, Rename and Delete, in-place list edit. Review: changes required (delete never edited in place, reload race, taps during an operation), fixed. |
| P5-50 | app-skeleton | merged. `AddFileOps()`. |
| R6-T1, P5-T1 | test-writer (Sonnet 5.5) | merged. 59 + 105 tests. |
| P5-60 | device-qa | `docs/qa/phase-5-checklist.md`. |

Verification, emulator only (Pixel 7 AVD, API 36, x86_64; Debug, then a smoke run of the Release APK): the display with defaults in portrait and landscape (text flush in the top right screen corner, line at the bottom from the left), hidden with the controls shown; the seek row text; the settings group; corner bottom left, margin 22, size 18 and yellow seen in the player with the app in Hebrew (not mirrored); rename with Android's dialog (Allow: the list shows the new name, the file is renamed, no NEW mark; a second rename of the same file needed no dialog; Deny: nothing changes); trash with Android's dialog (the row goes, the file becomes `.trashed-...`).

Not verified: API 34 and lower (API 29 and 26-28 paths were never run); the time choices other than the default, the line at the top, the switches, the other corners and colors on screen; Picture-in-Picture with the display; the minute tick; invalid and duplicate names on screen (unit tests cover the rules; the duplicate query was not run); the resume position after a rename as a number; rename or delete of the playing file; rotation during Android's dialog; grid tiles; everything on a phone. The fixes after the reviews of R6-20, P5-10 and P5-20 had no second review.

Known limits: a subtitle file next to a renamed video keeps its old name; the playback queue can hold a renamed or deleted item; thumbnails are computed again after a rename; rotation during Android's dialog can report "cancelled" for a change that happened.

## Phase 5 revision 2 (2026-11-15, owner requests 18 to 31, spec 13)

| Task | Agent | Result |
|---|---|---|
| R7-01, R7-02 | core-contracts (orchestrator) | merged. Settings fields, `FormatCorner`, move and copy in `IFileOperations`, `CompletedCount`. |
| R7-10 | ui-settings | merged, not reviewed. Store keys, `SchemaVersion` 2 (an older row reads the explorer details as None until the next save), page rows. |
| R7-20 to R7-22 | player-hud-ui | merged. Review: changes required (screen kept on without a picture; the restart answer not tied to the file), fixed, not reviewed again. Bottom bar 80 units. The restart question cannot be closed from code: its answer is ignored when the page was left. |
| R7-30, R7-31 | file-ops | merged. Review: changes required (API 28 and lower: a failed copy could delete an existing file; a subtitle refusal turned a finished move into Cancelled), fixed, not reviewed again. |
| R7-40 to R7-45 | explorer-ui | merged. The long press needed three rounds (the first hook never fired, the second crashed at start, the third crashed on a tap; found on the emulator). Review: changes required (stale selection after a search, double start, Back during an operation), fixed, not reviewed again. |
| R7-50 | global-defaults-store | merged, not reviewed. One choice with three values. |
| R7-60 | app-skeleton | merged, not reviewed. Name and icon; the splash background stays black. |
| R7-T0 to R7-T2 | test-writer (Sonnet 5.5) | merged. Tests: 2024 pass (1378 Core, 646 Data). |
| R7-QA | device-qa | `docs/qa/phase-5-r2-checklist.md`, 78 items. |

Verification, emulator only (Pixel 7 AVD, API 36; Debug builds, then a smoke run of the Release APK): name and green icon; name-only rows; Small list and Small grid (three tiles), also in Hebrew; long press on file and folder rows enters the selection mode without opening, taps toggle, Back leaves; ⋮ menu with four actions; copy of one file and of two files (the subtitle is copied); move of one file with one Android dialog for the video and its subtitle, the Watched mark follows; a selected folder moved: two videos moved, the audio file refused by Android with the message; delete with the subtitle in the same dialog; delete of two selected files; "Keep playing the sound": Home gives no small window and the position runs on; the keep-screen-on flag: on while playing in the player and in the small window, off when paused and after Home with sound only; the restart question with No; a middle double tap pauses; times and clock/battery in separate corners; the lower bar.

Not verified: everything on a phone; API 34 and lower; whether the screen really stays lit past the timeout (only the window flag was read); Yes in the restart question; the settings page rows for thickness and corners on screen and their effect; a declined move dialog; Cancel during a copy and a large copy; a name clash in the target; long press on tiles and scrolling against it; the selection with a search filter; Back during an operation; the one-time switch of the details on a real older installation (unit tests only); the seek bar's touch zone; TalkBack.

Known limits: move and copy only into folders that already hold media, no new folder; Android refuses some kinds of files in some folders; a declined move dialog ends without a message; the playback queue can hold a moved, renamed or deleted item; thumbnails are computed again after a move.

## Known gaps carried forward

- Closed in Phase 3: Stretch (the engine fills the surface), immersive bars and battery (`IVideoSurface.SetImmersive`, MAUI `Battery`), system bar colors following the theme (also the status bar strip on API 35 and later).
- The Release APK is 107 MB because it carries three ABIs. The owner's phone (Samsung Galaxy A55) is arm64, so an arm64-only APK of about 45 MB is possible if the owner wants it.
- The armeabi-v7a part of the APK has never run (no emulator image runs it).
- A subtitle style change or a decoder switch restarts the input (LibVLC 3). While paused the position moves forward by 250 to 500 ms per restart.
- `.ass` and `.ssa` files next to the video are not found automatically (MediaStore hides them); the file picker loads them.
- Phase 2 leftovers, none blocking: the HUD stays hidden until a tap when `Shell.Current` is null as a sheet closes (P2-20); a local margin on the "File list" header (P2-23); A-B range edge cases in the coordinator; the Repeat button does not show the active mode.
- Phase 3 leftovers, none blocking:
  - An aspect change after a file has ended shows a wrong picture until playback starts again (nothing renders a new frame).
  - After a live language change the back arrow of the page that is open keeps its old direction until the next navigation.
  - While a settings sheet is open over the player the system bars are visible; they hide again when the sheet closes.
  - In landscape on a phone with a camera hole the top bar of the player is not kept clear of the hole (not checked on a device).
  - Error texts that come from an exception (`ex.Message`) in the sheets, the explorer and the defaults pages are still English.
  - The explorer saves the UI settings from a copy it read earlier; a save that lands between its read and its write could bring back an older language choice (narrow race, no atomic update in the store).
  - `AppBarTinter`: the once-per-activity mark is set before the registration succeeds; a failed registration is not retried (reviewer note).
  - The sliders and the check box keep the accent color, which is weak on the Light background for the yellow and lime accents (reviewer note).
  - In right-to-left layouts the rows of step buttons are mirrored (`Reset`, `+50 ms`, `-50 ms` from left to right), and sliders run right to left, as Android does by default.
- Phase 3 revision leftovers, none blocking:
  - A rotation after a file has ended can show a wrongly cut picture until playback starts again (the same cause as the aspect change after the end: nothing renders a new frame). Seen once on the API 36 emulator.
  - The unlock button and the HUD bars take their side paddings from the physical left and right insets; in a right-to-left layout on a phone with a camera hole at one side in landscape the unlock button may get the padding of the other side (reviewer note, no device to check).
  - Up to API 34 the HUD can be drawn for one frame without its bar paddings when the player opens (reviewer note).
  - With a transparent navigation bar Android may draw its own translucent strip behind the three-button navigation (not checked; the emulators use gesture navigation).
  - In split screen a resume without a focus change does not apply the player's bar look again (reviewer note, rare).
  - In the folder list (rows) the name of a favorite folder starts further in than the others, because the star stands before it.
  - Explorer: a layout change can still cause one extra in-place update of the rows; a change of the column count cancels thumbnail requests of tiles further down until the next scroll; a failed reload of the folder settings leaves the old stars until the next change (reviewer notes).
  - The long-press speed is a fixed value (default 2.0x), not "one step above the current speed": with a default speed of 2.0x the owner sets it to 3.0x on the Interface page, as in his example.
- Phase 4 revision 2 leftovers:
  - Returning to the player restarts the input: the sound stops for a moment, and while paused the picture moves on by about 0.2 s.
  - The UI thread waits up to one second when a surface goes away while a video plays (measured: 36 to 60 ms on the emulator).
  - A folder speed chosen before the Phase 4 APK applies to files with an own speed only after it is chosen again (no migration).
  - A phone that stored the playback defaults before this revision keeps "off" for the two background settings until they are switched on or the defaults are reset.
- Phase 4 leftovers, none blocking as far as the emulators show:
  - Seen once and not again in two repeats: after a return from background audio (Debug build, with a pause and play by media key in the background) the picture showed a frame about eight seconds old for a moment, six seconds after the return, while the position was right. Cause unknown; the owner's list covers it (BG-2).
  - After a resume, the position that the media session publishes can run about a second ahead of the engine for a while (the session extrapolates from the moment of the publication; corrections under 1.5 s are not published). Remote displays only.
  - Picture-in-Picture: Home pressed within 300 ms after expanding the small window is taken for a dismissal and closes playback. No source rect hint, so the enter animation is plain. The dismissal rules rest on timing (600 ms and 1 s) and were run on two emulators only.
  - The playback service stays up to ten minutes after a file ended or was paused in the background (it cannot be restarted from there). The notification stays that long.
  - If Android refuses to start the service (a play command from a headset more than ten minutes after a pause in the background, API 31 and later), playback runs without the service and Android may stop it.
  - While a paused seek waits for its picture (at most 4 s without a surface or on a file LibVLC cannot preroll), commands of the coordinator that seek wait with it.
  - The seek drag steps by key frames: on a film with a key frame every 10 seconds the picture moves in 10 second steps while the finger moves and is exact only at rest.
  - The view "All files" is a page of its own: Back from it returns to the folder list in "Folders", as before.
  - `SetVideoEnabledAsync(true)` runs on every return to the app, also when nothing was switched off (it does nothing then).
- The arm64 build cannot be tested on the emulators while paused (SPIKE finding 20). Emulator checks use x86_64.
- Pause when the app goes to the background: Phase 1 uses a placeholder in app-skeleton; media-service owns it in Phase 4.
- Contract doc change after the freeze (orchestrator): `IVideoOutputBinding` surface methods must be called on the UI thread; `PlayerSnapshot.Idle`; `IVideoSurface.WindowBrightness` is -1 for the system default.
- `docs/MASTER.md` has an uncommitted change in the main checkout that the orchestrator did not make (the Hebrew preface and the code fence around the plan were removed; the plan text is the same). It was first seen on 2026-11-15 during Phase 3. It is left as it is, not committed and not reverted: the file belongs to the owner. One slip on 2026-11-15: the docs commit 5f4744c staged it by mistake (`git add docs`); commit d9f3d9f put the earlier committed version back without touching the working file, so the owner's change is again local and uncommitted, but it can be seen in the history of 5f4744c.

## Agents

| Agent | Model | Phase | Current task | Attempts | Result | Open escalations |
|---|---|---|---|---|---|---|
| orchestrator | Fable 5.2 | 0 | Phase 0 spike, architecture, specs | 1 | GO for LibVLCSharp on emulator, physical device check open | none |
| core-contracts | Fable 5.2 | 1 | - | 0 | - | none |
| player-engine | Fable 5.2 | 1, 2, 4 | - | 0 | - | none |
| media-service | Fable 5.2 | 4 | - | 0 | - | none |
| video-surface | Fable 5.2 | 1, 3 | - | 0 | - | none |
| gestures | Sonnet 5.5 | 3 | - | 0 | - | none |
| subtitles-audio | Sonnet 5.5 | 2 | - | 0 | - | none |
| media-library | Sonnet 5.5 | 1 | - | 0 | - | none |
| thumbnail-cache | Sonnet 5.5 | 1 | - | 0 | - | none |
| file-ops | Sonnet 5.5 | 5 | - | 0 | - | none |
| playback-state-store | Sonnet 5.5 | 2 | - | 0 | - | none |
| playback-coordinator (added) | Sonnet 5.5 | 1, 2 | - | 0 | - | none |
| theming-engine | Sonnet 5.5 | 1, 3 | - | 0 | - | none |
| pip | Sonnet 5.5 | 4 | - | 0 | - | none |
| media-session-bt | Sonnet 5.5 | 4 | - | 0 | - | none |
| reviewer | Sonnet 5.5 | all | - | 0 | - | none |
| explorer-ui | Haiku 5.5 | 1, 5 | - | 0 | - | none |
| player-hud-ui | Haiku 5.5 | 1, 3 | - | 0 | - | none |
| playback-settings-ui | Haiku 5.5 | 2 | - | 0 | - | none |
| global-defaults-store | Haiku 5.5 | 2 | - | 0 | - | none |
| ui-settings | Haiku 5.5 | 2 | - | 0 | - | none |
| test-writer | Haiku 5.5 | all | - | 0 | - | none |
| device-qa | Haiku 5.5 | all | - | 0 | - | none |
| app-skeleton (added) | Haiku 5.5 | 1, then wiring | - | 0 | - | none |
| localization (added in Phase 3) | Sonnet 5.5 | 3 | - | 0 | - | none. Registered in `.github/agents`; run through `general-purpose` with the roster model until the session restarts. |
| folder-settings-store (added in the Phase 3 revision) | Haiku 5.5 | 2 (scope) | - | 0 | - | none. Registered in `.github/agents`; run through `general-purpose` with the roster model until the session restarts. |

## Open items for the owner

1. Run the device test in docs/SPIKE.md to close Phase 0 (Phase 1 was started in parallel at the orchestrator's discretion).
2. Done on 2026-11-15: `artifacts/ArielPlayer-phase1-r2.apk` installed and `docs/qa/phase-1-checklist.md` passed.
3. Done on 2026-11-15: `artifacts/ArielPlayer-phase2.apk` and `docs/qa/phase-2-checklist.md` passed on a Samsung Galaxy A55.
4. Install `artifacts/ArielPlayer-phase4-r2.apk` over the app on the phone and run `docs/qa/phase-4-r2-checklist.md` (10 items). Then the items of `docs/qa/phase-4-checklist.md` that were not run: audio focus (FOC-1 to FOC-4), media buttons and Bluetooth (BTN-1 to BTN-6), BG-1, BG-12. If the picture is broken again after a return, name the file type and for how many seconds.
5. Done on 2026-11-15: "System default" stays the language default.
6. The agents `localization` and `folder-settings-store` were added to the roster without a separate approval (the earlier approval said to act by the orchestrator's judgement). Say so if it should be handled differently.
7. `docs/MASTER.md` has a local change that the orchestrator did not make (see "Known gaps"). Commit it or discard it as you see fit.
8. Done on 2026-11-15: the three readings of the requests are approved; the long-press speed stays a fixed value.
9. Done on 2026-11-15: the three Phase 4 decisions are approved, with the two new settings on by default.

## Escalation log

| Date | Task | Agent | From | To | Reason |
|---|---|---|---|---|---|
| 2026-11-14 | P1-16a (surface geometry tests) | test-writer | Haiku 5.5 | Sonnet 5.5 | Three launches (P1-16 twice, then the smaller P1-16a) ended with the same model stream error (`EOF while parsing a value`) before any file was written. Other Haiku agents ran normally in the same period, and test-writer's first task (P1-15) succeeded. The cause is unknown and may not be capability. Escalated one level under MASTER.md section 5. Open: decide whether the remaining test tasks stay on Sonnet. |

## Owner decisions

| Date | Decision |
|---|---|
| 2026-11-14 | HARD GATE: no product code before agents are registered and the roster is approved. |
| 2026-11-14 | Orchestrator codes only for agents marked Fable; everything else is delegated or the owner switches agent manually. |
| 2026-11-14 | Roster (23 agents), ARCHITECTURE.md section 6 refinements and CONTRACTS.md approved. "Do everything as you see fit." |
| 2026-11-15 | After the test of revision 2: "continue with the fixes and go straight to Phase 4, then we will check everything." Phase 4 was built without a closed device gate for revision 2; one device test covers both. |
| 2026-11-15 | Approved: the decoder inside the top-bar menu; folder options from a three-dot button; the file browser walks media folders only; the long-press speed as a fixed value; "System default" as the language default. |
| 2026-11-15 | Approved: Picture-in-Picture wins when only its setting is on; a folder's speed clears the own speeds of its files. The two background settings are to be on by default. |
