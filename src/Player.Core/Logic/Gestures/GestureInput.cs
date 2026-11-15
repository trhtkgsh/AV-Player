namespace Player.Core.Logic.Gestures;

/// <summary>Kind of a raw pointer event, as Android reports it.</summary>
public enum PointerAction
{
    /// <summary>The first pointer touched the view.</summary>
    Down,

    /// <summary>A further pointer touched the view while another one is down.</summary>
    PointerDown,

    /// <summary>One or more pointers moved.</summary>
    Move,

    /// <summary>A pointer lifted while another one stays down.</summary>
    PointerUp,

    /// <summary>The last pointer lifted.</summary>
    Up,

    /// <summary>The system took the touch away.</summary>
    Cancel,
}

/// <summary>The position of one pointer, in pixels of the view.</summary>
public readonly record struct PointerSample(int Id, double X, double Y);

/// <summary>Thresholds of <see cref="GestureClassifier"/>.</summary>
/// <param name="TouchSlop">Movement in pixels that turns a tap candidate into a drag.</param>
/// <param name="DoubleTapWindowMs">Longest time from the first tap's release to the second touch.</param>
/// <param name="DoubleTapDistance">Largest distance in pixels between the two touch-down points.</param>
public sealed record GestureClassifierOptions(
    double TouchSlop = 16,
    long DoubleTapWindowMs = 300,
    double DoubleTapDistance = 100);
