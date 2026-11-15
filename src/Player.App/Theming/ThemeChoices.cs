using CommunityToolkit.Mvvm.ComponentModel;
using Player.Core.Models;

namespace Player.App.Theming;

/// <summary>One row of the theme list.</summary>
public sealed partial class ThemeChoice : ObservableObject
{
    public ThemeChoice(ThemeId id, string name)
    {
        Id = id;
        Name = name;
    }

    public ThemeId Id { get; }

    public string Name { get; }

    [ObservableProperty]
    private bool _isSelected;
}

/// <summary>One accent color of the palette.</summary>
public sealed partial class AccentSwatch : ObservableObject
{
    public AccentSwatch(uint argb)
    {
        Argb = argb;
        Description = $"#{argb & 0x00FFFFFF:X6}";
    }

    public uint Argb { get; }

    public Microsoft.Maui.Graphics.Color Color => Microsoft.Maui.Graphics.Color.FromUint(Argb);

    /// <summary>The hex code, for screen readers.</summary>
    public string Description { get; }

    [ObservableProperty]
    private bool _isSelected;
}
