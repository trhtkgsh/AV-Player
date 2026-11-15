# Agent: video-surface

**Mission.** Provide the native video view: surface lifecycle, aspect modes, zoom/pan and window brightness.

**Model and why.** Fable 5.2. A native surface that stays sharp at 400% and survives backgrounding is easy to get subtly wrong (docs/SPIKE.md findings 5-8).

**Phase.** 1: plain Fit surface with full lifecycle. 3: aspect modes, zoom/pan, brightness.

**Owned paths.** `src/Player.App/Platforms/Android/Surface/**`, `src/Player.Core/Logic/Surface/**`.

**Read-only contracts.** Implements `IVideoSurface`. Uses `IVideoOutputBinding`, `VideoFormat`, `ViewTransform`, `AspectMode`.

**Inputs.** docs/specs/02, docs/ARCHITECTURE.md section 3, docs/SPIKE.md findings 5-8.

**Deliverables.** `VideoSurfaceView` (MAUI view) with its Android handler and native `SurfaceView`, `VideoSurfaceController` (`IVideoSurface`), `AddVideoSurface()` DI extension.

**Steps.**
1. Native `SurfaceView` inside a clipping container. Forward `surfaceCreated`, `surfaceChanged` and `surfaceDestroyed` to `IVideoOutputBinding.SurfaceReady`, `SurfaceSizeChanged` and `SurfaceLost`. Report the size explicitly every time it changes.
2. Phase 1: size the surface to fit the container using `IVideoOutputBinding.Format` (including the sample aspect ratio). Re-layout on `FormatChanged` and on rotation.
3. Phase 3: compute the surface rectangle for Fit, Stretch, Crop and Original from the video format and the container size.
4. Zoom and pan change the layout bounds of the surface (so the frame is rendered at the zoomed size), not only a view transform. Clamp scale to 1..4 and keep the picture covering the container when zoomed. If bounds exceed device limits, fall back to a transform for the excess and report it as a deviation.
5. Reset the transform when the aspect mode changes.
6. Window brightness through the activity window attributes. `null` restores the system value. Restore on leaving the player page.
7. No LibVLC type and no touch handling in the owned path.

**Acceptance.**
- Build green. No reference to LibVLC in the owned path.
- Geometry is pure static code in `src/Player.Core/Logic/Surface` (inputs: format, container, mode, transform; output: rectangle), so test-writer can cover it.
- Device: the four aspect modes look right for 16:9, 4:3 and vertical video in both orientations. Text in a 1080p frame stays sharp at 400%. Pan never shows an empty edge. 20 background/foreground cycles keep the picture.

**Decisions made in Phase 3 (orchestrator).**
- `IVideoSurface.SetImmersive` hides the system bars and lets the picture use the cutout area; it belongs here because it is window code, like the brightness.
- The surface keeps its last shape when the engine reports "no format" (a stop); a newly attached view starts from the engine's current format.
- The engine fills the surface exactly (it sets LibVLC's aspect ratio to the surface size), so the rectangle computed here is the shape of the picture in every mode.

**Decisions made in the Phase 3 revision (spec 11; they win over the text above).**
- `SetImmersive(true)` is the player mode: the window is laid out edge to edge and the system bars start hidden. `SetSystemBarsVisible` shows or hides them inside that mode. The window is not laid out again; the page that hosts the surface must set `SafeAreaEdges="None"`, or MAUI pads the surface while the bars are visible (SPIKE finding 27).
- `SystemBarInsets` gives the space taken by the system bars and the display cutout (read ignoring visibility), for the HUD's paddings. It is read again after a mode change and after a layout of the container.
- In player mode the bars are transparent with white symbols in every theme; the look the theme had set is saved and put back when the mode ends. Up to API 34 the end of player mode also clears the bar padding MAUI left on the pages under the player (SPIKE finding 28).
- In player mode the picture area starts below the status bar, whether the bar is shown or hidden (owner: the picture may reach the bar but not pass it). A black strip over the surface cuts a zoomed picture at the bar; the pan limits use the same area. In Picture-in-Picture there is no such strip.

**Out of scope.** Touch recognition (gestures). HUD (player-hud-ui). Engine internals. Picture-in-Picture.

**Escalation.** Stop and report when a contract is missing or wrong, a file outside the owned path is needed, an acceptance item cannot be met, or the same step failed twice. Never change a contract.

**Report format.** `STATUS: done | blocked`, `BRANCH`, `FILES`, `BUILD`, `TESTS`, `DEVICE CHECKLIST` (items for the owner), `DEVIATIONS`, `QUESTIONS`.
