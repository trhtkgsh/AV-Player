namespace Player.App.Gestures;

/// <summary>
/// Transparent layer that catches every touch inside its bounds and reports it as gestures
/// through <c>IGestureSource</c>. Place it above the video and below the HUD controls.
/// It has no properties.
/// </summary>
public sealed class GestureOverlayView : View
{
}
