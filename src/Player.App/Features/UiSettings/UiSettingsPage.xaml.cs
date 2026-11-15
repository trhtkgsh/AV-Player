using Microsoft.Maui.Controls;

namespace Player.App.Features.InterfaceSettings;

public partial class UiSettingsPage : ContentPage
{
    private readonly UiSettingsViewModel _viewModel;

    public UiSettingsPage(UiSettingsViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        try
        {
            await _viewModel.LoadAsync();
        }
        catch (Exception)
        {
            // The view model reports its own errors through ErrorMessage.
        }
    }
}
