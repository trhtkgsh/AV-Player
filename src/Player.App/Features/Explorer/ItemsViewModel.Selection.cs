using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Controls;
using Player.Core.Logic.Explorer;
using Player.Core.Models;

namespace Player.App.Features.Explorer;

// Selection mode of the items page: a long press starts it, a tap toggles a file, and the top bar acts on the selection.
public sealed partial class ItemsViewModel
{
    // A tap within this time after a long press is ignored, so the press that starts the mode does not also toggle.
    private const long LongPressTapGuardMs = 600;

    private readonly ExplorerSelection<MediaFileId> _selection = new();
    private long _tapGuardUntil;

    [ObservableProperty]
    private bool _isSelecting;

    [ObservableProperty]
    private int _selectedCount;

    [ObservableProperty]
    private bool _canRename;

    // True while a batch operation runs: the list is covered by a busy state and taps are blocked.
    [ObservableProperty]
    private bool _isOperationRunning;

    [ObservableProperty]
    private string _operationName = string.Empty;

    // The ⋮ buttons and the normal toolbar actions show only outside the selection mode.
    public bool IsBrowsing => !IsSelecting;

    partial void OnIsSelectingChanged(bool value) => OnPropertyChanged(nameof(IsBrowsing));

    [RelayCommand]
    private void BeginSelect(MediaRowViewModel row)
    {
        if (_fileOperationRunning)
        {
            return;
        }

        _tapGuardUntil = Environment.TickCount64 + LongPressTapGuardMs;
        _selection.Add(row.Item.Id);
        UpdateSelection();
    }

    [RelayCommand]
    private void ExitSelection()
    {
        _selection.Clear();
        UpdateSelection();
    }

    [RelayCommand]
    private void SelectAll()
    {
        if (_fileOperationRunning)
        {
            return;
        }

        _selection.AddRange(Rows.Select(row => row.Item.Id));
        UpdateSelection();
    }

    private void ToggleSelect(MediaRowViewModel row)
    {
        _selection.Toggle(row.Item.Id);
        UpdateSelection();
    }

    // Keeps the rows, the mode, the count and the title in step with the selection. Clearing the last item leaves the mode.
    private void UpdateSelection()
    {
        foreach (var row in Rows)
        {
            row.IsSelected = _selection.Contains(row.Item.Id);
        }

        IsSelecting = _selection.IsActive;
        SelectedCount = _selection.Count;
        CanRename = SelectionRules.Allows(SelectionAction.Rename, _selection.Count, 0);
        RebuildTitle();
    }

    private List<MediaItem> SelectedItems() =>
        Rows.Where(row => _selection.Contains(row.Item.Id)).Select(row => row.Item).ToList();

    [RelayCommand]
    private async Task DeleteSelected()
    {
        var shell = Shell.Current;
        if (_fileOperationRunning || !IsSelecting || shell is null)
        {
            return;
        }

        var items = SelectedItems();
        try
        {
            if (!await _operations.ConfirmDeleteAsync(shell, items.Count, 0))
            {
                return;
            }

            _fileOperationRunning = true;
            IsOperationRunning = true;
            OperationName = _localizer["Explorer.OperationDelete"];
            try
            {
                var result = await _operations.TrashAsync(items, SessionToken);
                await ApplyBatchResultAsync(
                    result,
                    "Explorer.SomeNotDeleted",
                    items.Count == 1 ? () => RemoveFromList(items[0]) : null);
            }
            finally
            {
                _fileOperationRunning = false;
                IsOperationRunning = false;
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            ReportError(ex);
        }
    }

    // The selection mode ends with the operation. One file is edited in place when it succeeds (editInPlace is set),
    // several files reload once. Cancelled reloads only when some files were done. Denied and Failed show the message.
    private async Task ApplyBatchResultAsync(FileOperationResult result, string partialKey, Action? editInPlace)
    {
        _selection.Clear();
        UpdateSelection();

        switch (result.Status)
        {
            case FileOperationStatus.Success when editInPlace is not null:
                await EditInPlaceAsync(editInPlace);
                break;

            case FileOperationStatus.Success:
                await LoadAsync();
                break;

            case FileOperationStatus.Cancelled when result.CompletedCount > 0:
                await LoadAsync();
                break;

            case FileOperationStatus.Cancelled:
                break;

            default:
                await LoadAsync();
                ErrorMessage = _operations.FailureText(result, partialKey);
                break;
        }
    }

    private void RemoveFromList(MediaItem item)
    {
        _items = MediaListEdits.Remove(_items, item.Id);
        _states.Remove(item.Id);
        _metadata.Remove(item.Id);
        RebuildRows();
    }

    // Rename is offered for exactly one file. The mode ends first, then the normal rename prompt runs.
    [RelayCommand]
    private async Task RenameSelected()
    {
        var shell = Shell.Current;
        var items = SelectedItems();
        if (_fileOperationRunning || items.Count != 1 || !CanRename || shell is null)
        {
            return;
        }

        _selection.Clear();
        UpdateSelection();
        try
        {
            await RenameFileAsync(shell, items[0]);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            ReportError(ex);
        }
    }
}
