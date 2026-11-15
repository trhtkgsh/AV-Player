# SPIKE: Phase 0 engine verification

Date: 2026-11-14. Author: orchestrator. Code: `spike/EngineSpike` (throwaway, never copied into the product wholesale).

## Setup

- .NET SDK 10.0.404, android workload 36.1.69, MAUI from the installed workload, target `net10.0-android`, min API 26.
- LibVLCSharp.MAUI 3.10.2, VideoLAN.LibVLC.Android 3.6.5 (LibVLC 3.0.21 Vetinari).
- Device: Android emulator only (Pixel 5 AVD, API 34, x86_64, WHPX). No physical device was connected.
- Media: local 60 s H.264 High 1280x720 + AAC file, generated offline (no network):
  `ffmpeg -f lavfi -i testsrc2=size=1280x720:rate=30 -f lavfi -i sine=frequency=440:sample_rate=44100 -t 60 -c:v libx264 -profile:v high -pix_fmt yuv420p -g 60 -c:a aac spike/EngineSpike/Resources/Raw/sample.mp4`
- The spike manifest declares no INTERNET or network permission.
- Evidence: screenshots in `spike/evidence/`, log lines quoted below (logcat tags `SPIKE`, `SPIKEVLC`).

## Results

| # | Check | Tested | Evidence | Verdict |
|---|---|---|---|---|
| 1 | LibVLCSharp on net10.0-android | Yes, emulator | `dotnet build -f net10.0-android`: 0 errors. Log: `LibVLC 3.0.21 Vetinari initialized`, `using video decoder module "mediacodec"`, `state=Playing ... vout=1`. Screenshot `04-fit.png` (video fitted, MAUI controls drawn above the surface). Playback by MediaStore id through a file descriptor: `14-mediastore-fd.png`. | PASS on emulator |
| 2 | Foreground service with MediaSession and headset buttons on a physical device | Partly: emulator only. Physical device: NO | `dumpsys activity services`: `isForeground=true foregroundId=1 types=00000002` (mediaPlayback). `dumpsys media_session`: `Media button session is com.arielplayer.enginespike/EngineSpike`, `active=true`, `state=PLAYING(3)`, `description=sample.mp4`. Injected keys 127, 126, 79, 79 produced `session: OnPause`, `OnPlay`, `OnPause`, `OnPlay` and matching `event Paused` / `event Playing`. After HOME the position kept advancing (12152 ms to 35904 ms) with the app in the background. | PASS on emulator. PENDING owner device test (see below) |
| 3 | Zoom / pan | Yes, emulator, programmatic transform | `05-zoom2x-pan.png` (2x with pan), `06-zoom-reset.png`. Log: `zoom scale=2 tx=270 ty=-331.2 view=1080x2208`. | PASS with limits (see below) |
| 4 | HW/SW switch during playback | Yes, emulator | Log: `switch decoder -> SW at 6654ms`, `using video decoder module "avcodec"`, next tick `time=8087ms hw=False`; then `switch decoder -> HW at 12585ms`, `using video decoder module "mediacodec"`, next tick `time=13586ms hw=True`. Screenshots `01`..`03` show the HUD line changing between `HW (mediacodec)` and `SW (avcodec)` at the continued timestamp. | PASS on emulator |

### What was not tested

- Anything on a physical device: real wired or Bluetooth headset buttons, pause on disconnect (becoming-noisy), audio focus loss from another app, long background playback with the screen off, OEM battery managers.
- Real hardware decoders. The emulator "hardware" codec is `c2.goldfish.h264.decoder`. HEVC, 10-bit, 4K and high bitrate files were not played.
- Pinch and drag gestures (zoom was driven by a command, not by fingers), zoom above 2x (400% is in scope), zoom combined with aspect modes.
- Picture-in-Picture, subtitles, audio tracks, speed with pitch correction. Not part of the four Phase 0 checks.
- API levels other than 34. A release (AOT, trimmed, arm64) build.

## Findings that constrain the design

1. **Never call the player from a LibVLC event handler.** `Stopped` is raised inside `Stop()` while LibVLC holds its lock. A handler that read `Player.Time` aborted the process: `Fatal signal 6 (SIGABRT)` with `libvlc_media_player_get_time` -> `vlc_mutex_lock` -> `__assert2`. The engine must hop to its own queue before touching the player.
2. **Engine commands must be serialized and off the UI thread.** `Stop()` blocks, and decoder switch is stop + new media + start.
3. **Decoder switch is not in place.** LibVLC 3 selects the decoder per media: `:codec=mediacodec_ndk,all` (HW) or `:codec=all` (SW), plus `:start-time`. The switch costs a restart (about 80-110 ms from the command to the `Playing` event in the log; visible gap not measured).
4. **HW can silently fall back to SW.** The only reliable signal is the log line `using video decoder module "..."`. The engine must report the actual decoder, not the requested one.
5. **Late attach renders unscaled.** LibVLCSharp reports the window size to LibVLC only from a layout-change callback. Attaching the player after the first layout pass drew the frame at native size in the bottom-left corner (`01-late-attach-unscaled-hw.png`). Attaching before layout fixed it (`04-fit.png`).
6. **The surface does not survive backgrounding.** After HOME and return, audio continued but the picture was black (`11-back-from-background-black.png`), and a full restart failed with `video output creation failed`. Toggling the video track alone did not help (`12-...png`). Re-attaching the view, forcing a window-size report and then restarting the video track recovered it (`13-reattach-recovered.png`). After that recovery the decoder was `avcodec`, so the restart must wait until the new surface is ready or HW is lost.
7. **No surface, no HW decoding.** With the screen off at launch, `mediacodec` failed (`Opaque Vout request failed`) and LibVLC retried the video output about 30 times per second. Audio-only mode must disable the video track instead of leaving it running.
8. **Zoom by view transform upscales the existing surface.** The picture softens at 2x because the surface keeps its on-screen resolution. Sharp zoom needs a larger render target or LibVLC scale and crop.
9. **16 KB page size.** Build warning XA0141: `libvlc.so` and `libc++_shared.so` in 3.6.5 are 4 KB aligned on every ABI. Measured ELF LOAD alignment of 3.7.0-beta: 16 KB for arm64-v8a and x86_64, 4 KB for `libc++_shared.so` on armv7 and x86. Matters for Google Play and for 16 KB devices.
10. **Size.** `libvlc.so` is 41 MB for arm64-v8a. The debug x86_64 APK is 53.7 MB (not representative of a release build).

Findings 5-8 mean the product should not rely on the stock `LibVLCSharp.MAUI.VideoView`. The `video-surface` agent owns a native surface view that drives LibVLC's `AWindow` itself.

### Findings added by the first run of the product (Phase 1, emulator, 2026-11-14)

11. **LibVLC's native libraries must be loaded on the UI thread.** `Core.Initialize()` on the engine's own thread failed with `dlopen failed: library "libc++_shared.so" not found`, although the libraries were in the APK. `System.loadLibrary` resolves the app's library directory only from a thread with a Java frame of the app. The spike never saw this because it initialized on the UI thread. The engine now initializes in its constructor and requires the UI thread.
12. **Finding 6 is solved in the product, with two additions.** Detaching the views and deselecting the video track on surface loss, then re-attaching and selecting the track again on surface return, keeps hardware decoding (`mediacodec` selected every time in 16 background/foreground cycles, no video output failure). Two things were needed on top:
    - A track selected in mid-stream stayed black for about 4.5 s after Play (`More than 11 late frames, dropping frame`). A seek to the current position right after the selection resyncs it.
    - A PAUSED input has no reliable way to draw one frame after that in LibVLC 3. The new decoder is not paused although the input is, so its first picture races the stopped clock (black about 1 time in 8). `NextFrame` aborts the process on `input_DecoderFrameNext: assertion "p_owner->paused" failed`. A restart with `:start-paused` stayed black 15 times out of 16. What works: run the input muted until its clock has moved about 120 ms, then pause again, while still reporting Paused. Result: 16 of 16 cycles show the picture, position drift about 50 ms per cycle.
13. **Opening before the surface exists works by waiting.** The explorer opens the file and then navigates, so the engine holds the start until the surface is reported (measured about 60 ms from surface ready to `mediacodec` selected). Without a surface within 2 s it starts with `:no-video`.

Evidence: `docs/evidence/phase1/`.

### Findings added in Phase 2 (emulator API 34, 2026-11-15)

14. **Subtitle renderer options are read from the LibVLC instance, not from the media.** The video output and its text renderer belong to the media player object, and the input's `:option` values are not inherited by them. `:freetype-color`, `:freetype-bold`, `:sub-margin` and the others given as media options changed nothing at all (the Phase 1 code did this, and no style was ever applied). Given to `new LibVLC(...)` as `--option` they all work. So a style change means a new LibVLC instance and a new player, and the engine recreates both when the style differs. Evidence: `docs/evidence/phase2/01-subtitle-style-and-rtl.png`.
15. **Hebrew punctuation needs `--freetype-text-direction=2`.** The default paragraph direction is left to right, so the full stop of a Hebrew sentence was drawn on the right. With direction 2 (automatic) the line is laid out right to left and mixed Hebrew, Latin and digits come out in the right order.
16. **Option names and units that work in 3.6.5:** `--sub-text-scale` (percent) for the size; `--freetype-color` and `--freetype-outline-color` (RGB as an integer); `--freetype-outline-thickness` (0, 2, 4 or 6); `--freetype-background-opacity` (0 to 255); `--freetype-bold`; `--sub-margin` in pixels of the video, measured up from LibVLC's own bottom position (200 on a 720p video moved the text up by about 200 video pixels). The margin therefore needs the video height before the instance exists; the engine takes it from the known format or from `MediaMetadataRetriever`.
17. **MediaStore shows `.srt` files to an app that holds only the media permissions, but not `.ass`.** On API 34 `ep2.srt` has `media_type = 5` (subtitle) and `application/x-subrip`; `vertical.ass` has `media_type = 0` and `application/octet-stream` and is invisible. Sidecar discovery therefore works for `.srt`; an `.ass` or `.ssa` file must be opened with the file picker. Not checked on other API levels.
18. **A modal Shell page with a transparent background is see-through over the video,** and the playback goes on behind it. The player page gets `OnDisappearing` when the modal opens and `OnAppearing` when it closes. The in-player settings sheets are built this way.
19. **An input started with `:start-paused` reports Playing and then Paused, 1 ms apart, and its time is 0 until the clock has run.** A restart of a paused file (subtitle style, decoder switch) therefore must not read `Time`: the first restart published position 0, the second one really started at 0, and the picture of the old instance stayed on the surface. The engine trusts the reported time only after the first `TimeChanged` of the input, and it draws the first picture of such an input by running it muted until 120 ms after the first reported time. The position moves forward by 250 to 500 ms per paused restart (accepted). An external subtitle track also survives these restarts only because the engine adds the file again and recognises its track by name: LibVLC gives it a new id every time.
20. **The arm64 build cannot be tested on the emulator's ARM translation while the video is paused.** On the API 36 image (x86_64 host, `libndk_translation`) the Release APK's arm64 half stopped responding in 8 of 10 paused runs as soon as enough UI was opened to need a garbage collection (the Speed sheet alone, 4th opening), and crashed twice inside the translator's JNI trampoline on a LibVLC audio thread. The ANR dump shows every .NET thread in `sigsuspend` and the main thread waiting: a stop-the-world that never ends. The likely reason: the .NET runtime stops threads with a signal, LibVLC's threads that ran a managed callback count as runtime threads, and while paused they block in a system call for good; the translator queues the signal but runs the guest handler only when the call returns. The x86_64 half of the same APK passes the same steps on the same emulator. This is not proven on hardware: the first two items of the Phase 2 checklist (ARM-1, ARM-2) check it on the owner's phone. If a real phone shows it too, the engine must stop raising managed callbacks on LibVLC threads (today: the log callback and the player events).

## Go / no-go

**GO for LibVLCSharp (LibVLC 3) as the engine. The Media3 fallback is not needed.**

All four checks passed on the emulator and no blocker was found. Every problem met has a demonstrated workaround, and each is recorded above as a design constraint.

This go is conditional on one item: check 2 requires a physical device and the owner has not run it yet. Phase 0 is not closed until then.

### Owner device test (closes Phase 0)

Install: `dotnet build spike/EngineSpike -f net10.0-android -t:Install` with the phone connected over USB (recreate `sample.mp4` first with the ffmpeg command above).

1. The video plays, fitted to the screen. The status line shows `HW (mediacodec)`.
2. Tap SW, then HW. Playback continues from the same position each time and the status line changes.
3. Tap Zoom 2x, then Zoom reset.
4. Tap Service on. A playback notification appears.
5. Press HOME. Audio continues for at least 2 minutes, also with the screen off.
6. Press play/pause on a wired headset and on a Bluetooth headset. Playback pauses and resumes.
7. Unplug the wired headset and disconnect Bluetooth while playing. Playback pauses.
8. Start audio in another app. Playback pauses.

Report the phone model, Android version and the number of any step that failed.

## Open decision (not blocking Phase 1)

VideoLAN.LibVLC.Android 3.6.5 (stable, 4 KB) or 3.7.0-beta (16 KB on 64-bit). Needed before any store release. Recommendation: build on 3.6.5 now, re-check when 3.7.0 is stable.
