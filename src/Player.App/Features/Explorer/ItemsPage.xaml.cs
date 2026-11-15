using System.ComponentModel;
using Microsoft.Maui.Controls;
using Player.Core.Logic.Explorer;

namespace Player.App.Features.Explorer;

public partial class ItemsPage : ContentPage
{
    private readonly ItemsViewModel _viewModel;

    public ItemsPage(ItemsViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;

        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        ApplyLayout();
        ApplyToolbar();
    }

    // The selection mode replaces the sort and layout actions with the selection actions, and Back leaves the mode.
    private void ApplyToolbar()
    {
        ToolbarItems.Clear();
        if (_viewModel.IsSelecting)
        {
            ToolbarItems.Add((ToolbarItem)Resources["SelectAllItem"]);
            ToolbarItems.Add((ToolbarItem)Resources["RenameItem"]);
            ToolbarItems.Add((ToolbarItem)Resources["DeleteItem"]);
            Shell.SetBackButtonBehavior(this, new BackButtonBehavior { Command = _viewModel.ExitSelectionCommand });
        }
        else
        {
            ToolbarItems.Add((ToolbarItem)Resources["SortItem"]);
            ToolbarItems.Add((ToolbarItem)Resources["LayoutItem"]);
            Shell.SetBackButtonBehavior(this, null);
        }
    }

    protected override bool OnBackButtonPressed()
    {
        if (_viewModel.IsSelecting)
        {
            _viewModel.ExitSelectionCommand.Execute(null);
            return true;
        }

        return base.OnBackButtonPressed();
    }

    // The list shows rows or a grid of tiles: the items layout and the item template follow the view model.
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
        if (e.PropertyName == nameof(ItemsViewModel.IsSelecting))
        {
            ApplyToolbar();
        }

        if (e.PropertyName == nameof(ItemsViewModel.IsGrid) || (e.PropertyName == nameof(ItemsViewModel.Columns) && _viewModel.IsGrid))
        {
            ApplyLayout();
        }
    }

    private void OnListSizeChanged(object? sender, EventArgs e) => _viewModel.SetListWidth(List.Width);

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
