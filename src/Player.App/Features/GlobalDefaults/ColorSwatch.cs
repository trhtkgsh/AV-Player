using CommunityToolkit.Mvvm.ComponentModel;

namespace Player.App.Features.DefaultsSettings;

/// <summary>One color choice of the subtitle style page.</summary>
public sealed partial class ColorSwatch : ObservableObject
{
    public ColorSwatch(uint argb, string name)
    {
        Argb = argb;
        Name = name;
    }

    public uint Argb { get; }

    public Microsoft.Maui.Graphics.Color Color => Microsoft.Maui.Graphics.Color.FromUint(Argb);

    public string Name { get; }

    [ObservableProperty]
    private bool _isSelected;
}
