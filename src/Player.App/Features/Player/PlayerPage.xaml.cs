using Microsoft.Maui.Controls;
using Player.Core.Contracts;

namespace Player.App.Features.PlayerHud;

public partial class PlayerPage : ContentPage
{
    private readonly PlayerViewModel _viewModel;
    private readonly ILocalizer _localizer;

    public PlayerPage(PlayerViewModel viewModel, ILocalizer localizer)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _localizer = localizer;
        BindingContext = viewModel;
        SizeChanged += (_, _) => _viewModel.SetScreenWidth(Width);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        try
        {
            await _viewModel.ActivateAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Activation failed: {ex}");
            await DisplayAlertAsync(_localizer["Common.Error"], _localizer["Player.ActionFailed"], _localizer["Common.Ok"]);
        }
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
