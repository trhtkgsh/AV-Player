using Microsoft.Maui.Controls;

namespace Player.App.Features.Explorer;

public partial class ItemsPage : ContentPage
{
    private readonly ItemsViewModel _viewModel;

    public ItemsPage(ItemsViewModel viewModel)
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

    private void OnListScrolled(object? sender, ItemsViewScrolledEventArgs e) =>
        _viewModel.VisibleRangeChanged(e.FirstVisibleItemIndex, e.LastVisibleItemIndex);
}
