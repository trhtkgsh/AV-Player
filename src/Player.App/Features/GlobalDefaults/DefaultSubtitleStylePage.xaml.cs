using Microsoft.Maui.Controls;

namespace Player.App.Features.DefaultsSettings;

public partial class DefaultSubtitleStylePage : ContentPage
{
    private readonly DefaultSubtitleStyleViewModel _viewModel;

    public DefaultSubtitleStylePage(DefaultSubtitleStyleViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.Activate();
        try
        {
            await _viewModel.LoadAsync();
        }
        catch (Exception)
        {
            // The page keeps the values it already shows.
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _viewModel.Deactivate();
    }
}
