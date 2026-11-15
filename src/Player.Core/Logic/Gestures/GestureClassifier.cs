using Player.Core.Models;

namespace Player.Core.Logic.Gestures;

/// <summary>
/// Turns raw pointer events into <see cref="GestureEvent"/>s. A pure state machine: it has no clock,
/// time comes in with the events and with <see cref="Tick"/>. Not thread safe, call it from one thread.
/// </summary>
public sealed class GestureClassifier
{
    private enum State { Idle, Candidate, Drag, Pinch, LongPress, Dead }

    private readonly Action<GestureEvent> _sink;

    private State _state;
    private readonly int[] _ids = new int[2];
    private readonly double[] _xs = new double[2];
    private readonly double[] _ys = new double[2];
    private int _count;

    private ScreenSide _side;
    private double _downX, _downY;
    private long _downTimeMs;
    private bool _sequenceLocked;
    private bool _secondTap;

    private GestureKind _dragKind;
    private double _originX, _originY;
    private double _pinchStartDistance, _pinchStartMidX, _pinchStartMidY;
    private GestureEvent? _lastChanged;

    private PendingTap? _pending;

    // Written by the UI thread of the HUD, read by the touch thread.
    private volatile bool _locked;

    private readonly record struct PendingTap(
        long UpTimeMs, double DownX, double DownY, ScreenSide Side, double FocusX, double FocusY);

    /// <summary>Creates a classifier that reports every event to <paramref name="sink"/>, in order.</summary>
    public GestureClassifier(Action<GestureEvent> sink, GestureClassifierOptions? options = null)
    {
        _sink = sink ?? throw new ArgumentNullException(nameof(sink));
        Options = options ?? new GestureClassifierOptions();
    }

    /// <summary>Thresholds. The platform view replaces them with the real touch slop of the device.</summary>
    public GestureClassifierOptions Options { get; set; }

    /// <summary>Width of the view in pixels.</summary>
    public double ViewWidth { get; set; } = 1;

    /// <summary>Height of the view in pixels.</summary>
    public double ViewHeight { get; set; } = 1;

    /// <summary>
    /// The caller sets this while the picture is zoomed: a one-pointer drag is then a <see cref="GestureKind.Pan"/>.
    /// It is read at the moment the drag starts.
    /// </summary>
    public bool PanMode { get; set; }

    /// <summary>
    /// Screen lock. Only <see cref="GestureKind.Tap"/> is reported, at once on release.
    /// It is read when a touch goes down and holds for that whole touch.
    /// </summary>
    public bool Locked
    {
        get => _locked;
        set => _locked = value;
    }

    /// <summary>
    /// The time at which <see cref="Tick"/> must be called: the end of the double-tap window of the waiting
    /// single tap, or the long-press time of the touch that is down and still a tap candidate, whichever is
    /// earlier. Null when nothing waits. Same time base as the events.
    /// </summary>
    public long? NextTickMs
    {
        get
        {
            var tap = PendingDueMs;
            var press = LongPressDueMs;
            if (tap is null)
                return press;

            return press is null ? tap : Math.Min(tap.Value, press.Value);
        }
    }

    private long? PendingDueMs => _pending is { } p && !_secondTap ? p.UpTimeMs + Options.DoubleTapWindowMs : null;

    private long? LongPressDueMs =>
        _state == State.Candidate && !_sequenceLocked ? _downTimeMs + Options.LongPressMs : null;

    /// <summary>Drops any pending tap and any active gesture without reporting anything.</summary>
    public void Reset()
    {
        _pending = null;
        EndTouch();
    }

    /// <summary>
    /// Reports the waiting single tap once the double-tap window has passed, and the long press once the
    /// touch has been down for <see cref="GestureClassifierOptions.LongPressMs"/> without passing the slop.
    /// </summary>
    public void Tick(long nowMs)
    {
        if (PendingDueMs is { } tapDue && nowMs >= tapDue)
            FlushPending();

        CheckLongPress(nowMs);
    }

    /// <summary>
    /// Feeds one pointer event. For <see cref="PointerAction.Move"/> only the given pointer is updated;
    /// use <see cref="ProcessMove"/> when Android reports several pointers at once.
    /// </summary>
    public void Process(PointerAction action, int pointerId, double x, double y, long timeMs)
    {
        switch (action)
        {
            case PointerAction.Down: OnDown(pointerId, x, y, timeMs); break;
            case PointerAction.PointerDown: OnPointerDown(pointerId, x, y, timeMs); break;
            case PointerAction.Move:
                if (IndexOf(pointerId) >= 0)
                {
                    SetPosition(pointerId, x, y);
                    OnMoved(timeMs);
                }

                break;
            case PointerAction.PointerUp: OnPointerUp(pointerId, x, y, timeMs); break;
            case PointerAction.Up: OnUp(pointerId, x, y, timeMs); break;
            case PointerAction.Cancel:
                CheckLongPress(timeMs);
                OnCancel();
                break;
        }
    }

    /// <summary>Feeds a Move event that carries the positions of all pointers.</summary>
    public void ProcessMove(ReadOnlySpan<PointerSample> pointers, long timeMs)
    {
        var any = false;
        foreach (var pointer in pointers)
        {
            if (IndexOf(pointer.Id) < 0)
                continue;

            SetPosition(pointer.Id, pointer.X, pointer.Y);
            any = true;
        }

        if (any)
            OnMoved(timeMs);
    }

    private void OnDown(int id, double x, double y, long timeMs)
    {
        if (_state != State.Idle)
            OnCancel();

        _sequenceLocked = Locked;
        _secondTap = false;
        if (_sequenceLocked)
        {
            FlushPending();
        }
        else if (_pending is { } p)
        {
            var near = Distance(p.DownX, p.DownY, x, y) <= Options.DoubleTapDistance;
            if (timeMs - p.UpTimeMs < Options.DoubleTapWindowMs && near)
                _secondTap = true;
            else
                FlushPending();
        }

        _count = 1;
        _ids[0] = id;
        _xs[0] = x;
        _ys[0] = y;
        _downX = x;
        _downY = y;
        _downTimeMs = timeMs;
        _side = SideOf(x);
        _state = State.Candidate;
    }

    private void OnPointerDown(int id, double x, double y, long timeMs)
    {
        if (_count != 1 || IndexOf(id) >= 0)
            return; // a third pointer, or a stray event: ignored for the whole touch

        CheckLongPress(timeMs);
        switch (_state)
        {
            case State.LongPress:
                Emit(LongPressEvent(GesturePhase.Cancelled));
                break;
            case State.Candidate when _sequenceLocked:
                _state = State.Dead;
                return;
            case State.Candidate:
                FlushSecondTap();
                break;
            case State.Drag:
                Emit(DragEvent(GesturePhase.Cancelled));
                break;
            default:
                return;
        }

        _ids[1] = id;
        _xs[1] = x;
        _ys[1] = y;
        _count = 2;
        _state = State.Pinch;
        _pinchStartDistance = Math.Max(PinchDistance(), 1);
        (_pinchStartMidX, _pinchStartMidY) = Midpoint();
        Began(PinchEvent(GesturePhase.Began));
    }

    private void OnMoved(long timeMs)
    {
        switch (_state)
        {
            case State.Candidate:
                if (_count != 1 || Distance(_xs[0], _ys[0], _downX, _downY) <= Options.TouchSlop)
                {
                    CheckLongPress(timeMs);
                    break;
                }

                if (_sequenceLocked)
                {
                    _state = State.Dead;
                    break;
                }

                // The touch is not a tap, so a tap waiting for its double is a single tap after all.
                FlushSecondTap();
                var horizontal = Math.Abs(_xs[0] - _downX) >= Math.Abs(_ys[0] - _downY);
                _dragKind = PanMode ? GestureKind.Pan : horizontal ? GestureKind.HorizontalDrag : GestureKind.VerticalDrag;
                _originX = _downX;
                _originY = _downY;
                _state = State.Drag;
                Began(DragEvent(GesturePhase.Began));
                break;
            case State.Drag:
                Changed(DragEvent(GesturePhase.Changed));
                break;
            case State.Pinch:
                Changed(PinchEvent(GesturePhase.Changed));
                break;
        }
    }

    private void OnPointerUp(int id, double x, double y, long timeMs)
    {
        var index = IndexOf(id);
        if (index < 0)
            return;

        if (_state != State.Pinch)
        {
            OnUp(id, x, y, timeMs);
            return;
        }

        SetPosition(id, x, y);
        Emit(PinchEvent(GesturePhase.Ended));

        // The remaining finger pans until it lifts. It never becomes a tap or a drag.
        var keep = 1 - index;
        _ids[0] = _ids[keep];
        _xs[0] = _xs[keep];
        _ys[0] = _ys[keep];
        _count = 1;
        _dragKind = GestureKind.Pan;
        _originX = _xs[0];
        _originY = _ys[0];
        _state = State.Drag;
        Began(DragEvent(GesturePhase.Began));
    }

    private void OnUp(int id, double x, double y, long timeMs)
    {
        if (IndexOf(id) >= 0)
            SetPosition(id, x, y);

        CheckLongPress(timeMs);
        switch (_state)
        {
            case State.LongPress:
                Emit(LongPressEvent(GesturePhase.Ended));
                break;
            case State.Candidate when _sequenceLocked:
                Emit(TapEvent(GestureKind.Tap, _xs[0], _ys[0], _side));
                break;
            case State.Candidate when _secondTap:
                _pending = null;
                Emit(TapEvent(GestureKind.DoubleTap, _xs[0], _ys[0], _side));
                break;
            case State.Candidate:
                _pending = new PendingTap(timeMs, _downX, _downY, _side, _xs[0] / SafeWidth, _ys[0] / SafeHeight);
                break;
            case State.Drag:
                Emit(DragEvent(GesturePhase.Ended));
                break;
            case State.Pinch:
                Emit(PinchEvent(GesturePhase.Ended));
                break;
        }

        EndTouch();
    }

    private void OnCancel()
    {
        switch (_state)
        {
            case State.LongPress:
                Emit(LongPressEvent(GesturePhase.Cancelled));
                break;
            case State.Drag:
                Emit(DragEvent(GesturePhase.Cancelled));
                break;
            case State.Pinch:
                Emit(PinchEvent(GesturePhase.Cancelled));
                break;
        }

        // A tap that was waiting for its double stays pending; Tick reports it.
        EndTouch();
    }

    /// <summary>
    /// Turns the touch into a long press when it has been down long enough and is still inside the slop.
    /// A tap waiting for its double is reported first, as before a drag.
    /// </summary>
    private void CheckLongPress(long nowMs)
    {
        if (LongPressDueMs is not { } due || nowMs < due || _count != 1
            || Distance(_xs[0], _ys[0], _downX, _downY) > Options.TouchSlop)
            return;

        FlushSecondTap();
        _state = State.LongPress;
        Emit(LongPressEvent(GesturePhase.Began));
    }

    private GestureEvent LongPressEvent(GesturePhase phase) =>
        new(GestureKind.LongPress, phase, _side, 0, 0, 1, _downX / SafeWidth, _downY / SafeHeight);

    private void EndTouch()
    {
        _state = State.Idle;
        _count = 0;
        _secondTap = false;
        _lastChanged = null;
    }

    private void FlushSecondTap()
    {
        if (!_secondTap)
            return;

        _secondTap = false;
        FlushPending();
    }

    private void FlushPending()
    {
        if (_pending is not { } p)
            return;

        _pending = null;
        Emit(new GestureEvent(GestureKind.Tap, GesturePhase.Ended, p.Side, 0, 0, 1, p.FocusX, p.FocusY));
    }

    private GestureEvent TapEvent(GestureKind kind, double x, double y, ScreenSide side) =>
        new(kind, GesturePhase.Ended, side, 0, 0, 1, x / SafeWidth, y / SafeHeight);

    /// <summary>An axis-locked drag reports only its own axis; a pan reports both.</summary>
    private GestureEvent DragEvent(GesturePhase phase) => new(
        _dragKind, phase, _side,
        _dragKind == GestureKind.VerticalDrag ? 0 : (_xs[0] - _originX) / SafeWidth,
        _dragKind == GestureKind.HorizontalDrag ? 0 : (_ys[0] - _originY) / SafeHeight,
        1, _xs[0] / SafeWidth, _ys[0] / SafeHeight);

    /// <summary>Deltas of a pinch are the movement of the midpoint since the pinch began.</summary>
    private GestureEvent PinchEvent(GesturePhase phase)
    {
        var (midX, midY) = Midpoint();
        return new GestureEvent(
            GestureKind.Pinch, phase, _side,
            (midX - _pinchStartMidX) / SafeWidth, (midY - _pinchStartMidY) / SafeHeight,
            PinchDistance() / _pinchStartDistance, midX / SafeWidth, midY / SafeHeight);
    }

    private void Began(GestureEvent began)
    {
        _lastChanged = began with { Phase = GesturePhase.Changed };
        Emit(began);
    }

    /// <summary>Android sends Move for pressure changes too; an event that repeats the last one is dropped.</summary>
    private void Changed(GestureEvent changed)
    {
        if (changed == _lastChanged)
            return;

        _lastChanged = changed;
        Emit(changed);
    }

    private void Emit(GestureEvent gestureEvent) => _sink(gestureEvent);

    private int IndexOf(int id)
    {
        for (var i = 0; i < _count; i++)
        {
            if (_ids[i] == id)
                return i;
        }

        return -1;
    }

    private void SetPosition(int id, double x, double y)
    {
        var index = IndexOf(id);
        _xs[index] = x;
        _ys[index] = y;
    }

    private double SafeWidth => Math.Max(ViewWidth, 1);

    private double SafeHeight => Math.Max(ViewHeight, 1);

    private ScreenSide SideOf(double x) => x < ViewWidth / 2 ? ScreenSide.Left : ScreenSide.Right;

    private double PinchDistance() => Distance(_xs[0], _ys[0], _xs[1], _ys[1]);

    private (double X, double Y) Midpoint() => ((_xs[0] + _xs[1]) / 2, (_ys[0] + _ys[1]) / 2);

    private static double Distance(double x1, double y1, double x2, double y2) =>
        Math.Sqrt(((x2 - x1) * (x2 - x1)) + ((y2 - y1) * (y2 - y1)));
}
