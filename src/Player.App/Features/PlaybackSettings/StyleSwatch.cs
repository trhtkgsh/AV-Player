using CommunityToolkit.Mvvm.ComponentModel;

namespace Player.App.Features.PlaybackSettings;

/// <summary>One color of a subtitle style palette.</summary>
public sealed partial class StyleSwatch : ObservableObject
{
    public StyleSwatch(uint argb, string name)
    {
        Argb = argb;
        Name = name;
    }

    public uint Argb { get; }

    public Microsoft.Maui.Graphics.Color Color => Microsoft.Maui.Graphics.Color.FromUint(Argb);

    public string Name { get; }

    [ObservableProperty] private bool _isSelected;
}
