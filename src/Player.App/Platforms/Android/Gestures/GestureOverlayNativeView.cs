using Android.Content;
using Android.OS;
using Android.Views;
using Player.Core.Logic.Gestures;
using AndroidView = Android.Views.View;

namespace Player.App.Gestures;

/// <summary>
/// Native touch catcher. It consumes every touch inside its bounds, hands the raw pointers to the
/// <see cref="GestureSource"/> and schedules the tick that reports a single tap after the double-tap window
/// and the long press after its delay (the tick is rescheduled from <c>NextTickMs</c> on every event).
/// </summary>
internal sealed class GestureOverlayNativeView : AndroidView
{
    private const int MaxPointers = 10;

    private readonly GestureSource _source;
    private readonly float _touchSlop;
    private readonly Java.Lang.Runnable _tick;
    private readonly PointerSample[] _samples = new PointerSample[MaxPointers];

    public GestureOverlayNativeView(Context context, GestureSource source)
        : base(context)
    {
        _source = source;
        _touchSlop = ViewConfiguration.Get(context)?.ScaledTouchSlop ?? 16;
        _tick = new Java.Lang.Runnable(OnTick);
        Clickable = true;
    }

    public override bool OnTouchEvent(MotionEvent? e)
    {
        if (e is null)
            return false;

        var time = e.EventTime;
        switch (e.ActionMasked)
        {
            case MotionEventActions.Down:
                Parent?.RequestDisallowInterceptTouchEvent(true);
                _source.OnPointer(PointerAction.Down, e.GetPointerId(0), e.GetX(0), e.GetY(0), time);
                break;
            case MotionEventActions.PointerDown:
                _source.OnPointer(PointerAction.PointerDown, e.GetPointerId(e.ActionIndex), e.GetX(e.ActionIndex), e.GetY(e.ActionIndex), time);
                break;
            case MotionEventActions.Move:
                var count = Math.Min(e.PointerCount, MaxPointers);
                for (var i = 0; i < count; i++)
                    _samples[i] = new PointerSample(e.GetPointerId(i), e.GetX(i), e.GetY(i));

                _source.OnMove(_samples.AsSpan(0, count), time);
                break;
            case MotionEventActions.PointerUp:
                _source.OnPointer(PointerAction.PointerUp, e.GetPointerId(e.ActionIndex), e.GetX(e.ActionIndex), e.GetY(e.ActionIndex), time);
                break;
            case MotionEventActions.Up:
                _source.OnPointer(PointerAction.Up, e.GetPointerId(0), e.GetX(0), e.GetY(0), time);
                break;
            case MotionEventActions.Cancel:
                _source.OnPointer(PointerAction.Cancel, e.GetPointerId(0), e.GetX(0), e.GetY(0), time);
                break;
        }

        ScheduleTick();
        return true;
    }

    protected override void OnSizeChanged(int w, int h, int oldw, int oldh)
    {
        base.OnSizeChanged(w, h, oldw, oldh);
        _source.Configure(_touchSlop, w, h);
    }

    protected override void OnAttachedToWindow()
    {
        base.OnAttachedToWindow();
        _source.Configure(_touchSlop, Width, Height);
        ScheduleTick();
    }

    protected override void OnDetachedFromWindow()
    {
        RemoveCallbacks(_tick);
        _source.Reset();
        base.OnDetachedFromWindow();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            RemoveCallbacks(_tick);
            _tick.Dispose();
        }

        base.Dispose(disposing);
    }

    private void OnTick()
    {
        _source.Tick(SystemClock.UptimeMillis());
        ScheduleTick();
    }

    private void ScheduleTick()
    {
        RemoveCallbacks(_tick);
        if (_source.NextTickMs is { } due)
            PostDelayed(_tick, Math.Max(0, due - SystemClock.UptimeMillis()));
    }
}
