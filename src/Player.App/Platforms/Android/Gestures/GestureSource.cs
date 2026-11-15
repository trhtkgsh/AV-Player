using Android.Util;
using Player.Core.Contracts;
using Player.Core.Logic.Gestures;
using Player.Core.Models;

namespace Player.App.Gestures;

/// <summary>
/// The gesture source of the app. The native overlay view feeds it with touch, it raises
/// <see cref="Gesture"/> on the UI thread. It never changes volume, brightness, position or zoom.
/// </summary>
public sealed class GestureSource : IGestureSource
{
    /// <summary>A transform scale above this means the picture is zoomed.</summary>
    private const double ZoomedThreshold = 1.001;

    private readonly IVideoSurface _surface;
    private readonly GestureClassifier _classifier;

    /// <summary>Creates the source. <paramref name="surface"/> tells it whether the picture is zoomed.</summary>
    public GestureSource(IVideoSurface surface)
    {
        _surface = surface;
        _classifier = new GestureClassifier(Raise);
    }

    /// <inheritdoc />
    public bool Locked
    {
        get => _classifier.Locked;
        set => _classifier.Locked = value;
    }

    /// <inheritdoc />
    public event EventHandler<GestureEvent>? Gesture;

    internal long? NextTickMs => _classifier.NextTickMs;

    internal void Configure(double touchSlop, double width, double height)
    {
        _classifier.Options = _classifier.Options with { TouchSlop = touchSlop };
        _classifier.ViewWidth = width;
        _classifier.ViewHeight = height;
    }

    internal void OnPointer(PointerAction action, int pointerId, double x, double y, long timeMs)
    {
        if (action == PointerAction.Down)
            _classifier.PanMode = _surface.Transform.Scale > ZoomedThreshold;

        _classifier.Process(action, pointerId, x, y, timeMs);
    }

    internal void OnMove(ReadOnlySpan<PointerSample> pointers, long timeMs) => _classifier.ProcessMove(pointers, timeMs);

    internal void Tick(long nowMs) => _classifier.Tick(nowMs);

    internal void Reset() => _classifier.Reset();

    /// <summary>A throwing subscriber must not take the UI thread down.</summary>
    private void Raise(GestureEvent gesture)
    {
        try
        {
            Gesture?.Invoke(this, gesture);
        }
        catch (Exception exception)
        {
            Log.Error("ArielGestures", $"A gesture subscriber threw: {exception}");
        }
    }
}
