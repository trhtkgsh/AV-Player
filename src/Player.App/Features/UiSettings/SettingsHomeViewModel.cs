using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Controls;

namespace Player.App.Features.InterfaceSettings;

/// <summary>The settings home page only navigates to the settings pages.</summary>
public sealed partial class SettingsHomeViewModel : ObservableObject
{
    [RelayCommand]
    private async Task OpenPlaybackDefaults()
    {
        try
        {
            await Shell.Current.GoToAsync("settings-defaults");
        }
        catch (Exception)
        {
            // Navigation failures are ignored: the row simply does nothing.
        }
    }

    [RelayCommand]
    private async Task OpenInterface()
    {
        try
        {
            await Shell.Current.GoToAsync("settings-ui");
        }
        catch (Exception)
        {
            // Navigation failures are ignored: the row simply does nothing.
        }
    }
}
