# Spec 12: Quiet display during playback (owner request of 2026-11-15, 19:10)

Phase: 5 (built with it). Agents: core-contracts (model and pure text logic), ui-settings (store and settings rows), player-hud-ui (the display).
Contracts: `UiSettings.Overlay` (`PlaybackOverlaySettings`), `OverlayCorner`, `OverlayEdge`, `OverlayTimeMode`, `PlaybackOverlayText`.

The owner's words: while a video plays on the full screen without the playback controls, show the following, each one optional in the player settings: a thin line along the whole screen that shows the progress of the video, in the color that was set; the time of day; the battery percent; elapsed, total and remaining time (the combination is a choice). Position: a corner of the screen, not of the picture, flush with no gap, with a choice of the margin from the edge. Size. Color, white by default. The elapsed and remaining time are to be in the playback controls as well.

## Requirements

1. **When.** The quiet display is shown while a file is open, the controls are hidden, the screen is not in Picture-in-Picture and no gesture indicator is on. It is hidden while the controls are shown. It takes no touches.
2. **Progress line.** A line 2 units high along the whole width of the screen, at the bottom edge (or the top edge: a setting), filled from the left by the played share of the file, in every language. Color: the theme's progress color (`ColorProgress`), which follows the accent color; the unfilled part is not drawn. Orchestrator's reading of "the color that was set".
3. **Text line.** One line in one corner of the screen: the playback times, then the clock, then the battery percent, separated by two spaces; each part only when its switch is on. The text is drawn left to right in every language. A thin dark outline or shadow keeps it readable on a bright picture.
4. **Times.** One of: off, elapsed, elapsed / total, remaining, elapsed / remaining, elapsed / total / remaining. Remaining is written with a minus sign. `PlaybackOverlayText` builds the texts.
5. **Position.** One of the four corners of the screen (physical, not mirrored). The text is flush with the screen edges when the margin is 0; the margin (0 to 48 units, default 0) moves it away from both edges of its corner. The corner is a corner of the screen, so the text can lie in the strip of the status bar or over the picture. Rounded screen corners or a camera hole can cut a text at margin 0: the margin setting is the answer.
6. **Size and color.** Text size 10 to 24 (default 12). Color from a fixed list, white by default.
7. **Settings.** On the Interface page, section "Player", a group "While the controls are hidden": switches for the progress line, the clock, the battery; choices for the line's edge, the times, the corner, the color; sliders for the margin and the size. All stored in `UiSettings.Overlay`. Defaults: line on at the bottom, clock on, battery on, times "elapsed / total", top right corner, margin 0, size 12, white.
8. **Updates.** The times and the line follow the position reports of the engine (four per second at most); the clock and the battery are read again once a minute and when the display appears. Nothing is computed while the display is hidden.
9. **Controls.** The seek row of the controls shows the elapsed time at the left and, at the right, the remaining time with a minus sign and the total time (`-32:30 / 45:00`).

## Acceptance

- Unit tests: `PlaybackOverlayText` (every time mode, unknown duration, clamping, the line with every switch combination).
- Device: each switch and choice takes effect at once in the player; the display hides with the controls shown and returns when they hide; nothing moves the picture.

## Out of scope

A separate position per item, more than one line, the progress line's thickness and its own color, a display over other apps.
