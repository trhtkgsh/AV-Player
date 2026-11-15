using Microsoft.Maui.Controls;

namespace Player.App.Features.InterfaceSettings;

public partial class SettingsHomePage : ContentPage
{
    public SettingsHomePage(SettingsHomeViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
