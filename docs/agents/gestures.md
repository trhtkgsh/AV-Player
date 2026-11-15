# Agent: gestures

**Mission.** Turn native touch input on the player into semantic `GestureEvent`s.

**Model and why.** Sonnet 5.5. Needs Android touch knowledge (multi-pointer, slop, conflicts between drag, pinch and double tap), but the behavior is fully specified and testable.

**Phase.** 3.

**Owned paths.** `src/Player.App/Platforms/Android/Gestures/**`, `src/Player.Core/Logic/Gestures/**`.

**Read-only contracts.** Implements `IGestureSource`. Uses `GestureEvent`, `GestureKind`, `GesturePhase`, `ScreenSide`.

**Inputs.** docs/specs/02 requirements 4 and 5, docs/CONTRACTS.md.

**Deliverables.** `GestureClassifier` (pure, in Core/Logic/Gestures), `GestureOverlayView` (transparent MAUI view with an Android handler that feeds the classifier), `GestureSource` (`IGestureSource`), `AddGestures()` DI extension.

**Steps.**
1. Write `GestureClassifier` as a pure state machine. Input: pointer events (action, pointer id, x, y, time) and the view size. Output: `GestureEvent`s. No Android types.
2. Rules: movement below the touch slop is a tap candidate. The first axis that passes the slop locks the drag as vertical or horizontal until release. Vertical drag side is decided by the start x. Two pointers start a pinch and cancel any drag. With one pointer after a pinch, or when the caller says zoom > 1, a drag is a pan.
3. Double tap: two taps within 300 ms and within the slop distance. A single tap is reported only after the double-tap window expires.
4. Report `DeltaX`/`DeltaY` as fractions of the view size since `Began`. Report pinch `Scale` relative to `Began` with the focus point as fractions.
5. `Locked = true`: report only taps.
6. The overlay view forwards native `MotionEvent`s to the classifier and raises events off the classifier output. It consumes all touches inside its bounds.
7. No side effects: this agent never changes volume, brightness, position or zoom.

**Acceptance.**
- Build green.
- Classifier is 100% pure and covered by test-writer: every rule above, cancellation, locked mode.
- Device: each gesture in spec 02 is recognized on the first try, 10 of 10 times. A diagonal drag never switches axis. Pinch never triggers a seek.

**Decisions made in Phase 3 (orchestrator).**
- Thresholds are an options record (slop from the platform, double-tap window 300 ms, distance 100 px). The classifier has no clock: time comes with the events and with `Tick`.
- Deltas are fractions of the view size since the touch-down point; an axis-locked drag reports only its own axis; the pan after a pinch is measured from the end of the pinch.
- A one-pointer drag is a `Pan` while the picture is zoomed: `GestureSource` reads `IVideoSurface.Transform.Scale` before each touch-down.
- A long press without movement is a tap. `Reset()` drops pending state when the view detaches.

**Out of scope.** Applying gestures (player-hud-ui ViewModel). OSD visuals. Zoom limits (video-surface).

**Escalation.** Stop and report when a contract is missing or wrong, a file outside the owned paths is needed, an acceptance item cannot be met, or the same step failed twice. Never change a contract.

**Report format.** `STATUS: done | blocked`, `BRANCH`, `FILES`, `BUILD`, `TESTS`, `DEVICE CHECKLIST` (items for the owner), `DEVIATIONS`, `QUESTIONS`.
