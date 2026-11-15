using Microsoft.Maui.Controls;

namespace Player.App.Features.PlaybackSettings;

public partial class SpeedSheet : ContentPage
{
    private readonly SpeedSheetViewModel _viewModel;

    public SpeedSheet(SpeedSheetViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.Activate();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _viewModel.Deactivate();
    }

    protected override bool OnBackButtonPressed()
    {
        _viewModel.CloseCommand.Execute(null);
        return true;
    }
}
