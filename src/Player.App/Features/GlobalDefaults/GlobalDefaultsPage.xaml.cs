using Microsoft.Maui.Controls;

namespace Player.App.Features.DefaultsSettings;

public partial class GlobalDefaultsPage : ContentPage
{
    private readonly GlobalDefaultsViewModel _viewModel;

    public GlobalDefaultsPage(GlobalDefaultsViewModel viewModel)
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
            // The page keeps the values it already shows.
        }
    }

    private void OnLanguageUnfocused(object? sender, FocusEventArgs e) =>
        _viewModel.SaveLanguagesCommand.Execute(null);
}
