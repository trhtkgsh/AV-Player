using Microsoft.Maui.Controls;

namespace Player.App.Features.Explorer;

public partial class FoldersPage : ContentPage
{
    private readonly FoldersViewModel _viewModel;

    public FoldersPage(FoldersViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;

        // The back arrow shows only in the file browser below the root; at the root the page behaves as before.
        var back = new BackButtonBehavior();
        back.SetBinding(BackButtonBehavior.IsVisibleProperty, new Binding(nameof(FoldersViewModel.CanGoUp), source: viewModel));
        back.SetBinding(BackButtonBehavior.CommandProperty, new Binding(nameof(FoldersViewModel.GoUpCommand), source: viewModel));
        Shell.SetBackButtonBehavior(this, back);
    }

    protected override bool OnBackButtonPressed()
    {
        if (_viewModel.CanGoUp)
        {
            _viewModel.GoUpCommand.Execute(null);
            return true;
        }

        return base.OnBackButtonPressed();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        try
        {
            await _viewModel.ActivateAsync();
        }
        catch (Exception)
        {
            // The view model reports its own errors through ErrorMessage.
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _viewModel.Deactivate();
    }
}
