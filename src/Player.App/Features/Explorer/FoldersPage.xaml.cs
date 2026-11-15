using System.ComponentModel;
using Microsoft.Maui.Controls;
using Player.Core.Logic.Explorer;

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

        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        ApplyLayout();
    }

    // The folders show as rows or as a grid of tiles: the items layout and the item template follow the view model.
    private void ApplyLayout()
    {
        if (!_viewModel.IsGrid)
        {
            List.ItemsLayout = new LinearItemsLayout(ItemsLayoutOrientation.Vertical);
            List.ItemTemplate = (DataTemplate)Resources["ListItem"];
            return;
        }

        if (List.ItemsLayout is GridItemsLayout grid)
        {
            grid.Span = _viewModel.Columns;
        }
        else
        {
            List.ItemsLayout = new GridItemsLayout(_viewModel.Columns, ItemsLayoutOrientation.Vertical)
            {
                HorizontalItemSpacing = GridTiles.Spacing,
                VerticalItemSpacing = GridTiles.Spacing,
            };
        }

        List.ItemTemplate = (DataTemplate)Resources["GridItem"];
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(FoldersViewModel.IsGrid) || (e.PropertyName == nameof(FoldersViewModel.Columns) && _viewModel.IsGrid))
        {
            ApplyLayout();
        }
    }

    private void OnListSizeChanged(object? sender, EventArgs e) => _viewModel.SetListWidth(List.Width);

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
