using CommunityToolkit.Mvvm.ComponentModel;

namespace Player.App.Features.PlayerHud;

/// <summary>
/// The text of one physical corner of the quiet display. The label is shown exactly while its text is not empty.
/// <see cref="Set"/> changes nothing when the text is the same, so an unchanged corner is never set again.
/// </summary>
public sealed partial class OverlayCornerText : ObservableObject
{
    [ObservableProperty] private string _text = "";
    [ObservableProperty] private bool _visible;

    /// <summary>Sets the text; returns without change when it is the same as before.</summary>
    public void Set(string value)
    {
        if (value == Text) return;
        Text = value;
        Visible = value.Length > 0;
    }
}
