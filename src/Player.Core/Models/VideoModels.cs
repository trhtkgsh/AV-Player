namespace Player.Core.Models;

/// <summary>Decoded video size and its sample aspect ratio.</summary>
public sealed record VideoFormat(int Width, int Height, int SarNumerator, int SarDenominator);

/// <param name="Scale">1 .. 4.</param>
/// <param name="PanX">Fraction of the view width.</param>
/// <param name="PanY">Fraction of the view height.</param>
public sealed record ViewTransform(double Scale, double PanX, double PanY)
{
    public static ViewTransform Identity { get; } = new(1, 0, 0);
}

/// <param name="Side">The half of the screen where the gesture began.</param>
/// <param name="DeltaX">Fraction of the view width since <see cref="GesturePhase.Began"/>.</param>
/// <param name="DeltaY">Fraction of the view height since <see cref="GesturePhase.Began"/>. Positive is down.</param>
/// <param name="Scale">Pinch scale relative to <see cref="GesturePhase.Began"/>. 1 for other gestures.</param>
/// <param name="FocusX">Fraction of the view width.</param>
/// <param name="FocusY">Fraction of the view height.</param>
public sealed record GestureEvent(
    GestureKind Kind,
    GesturePhase Phase,
    ScreenSide Side,
    double DeltaX,
    double DeltaY,
    double Scale,
    double FocusX,
    double FocusY);
