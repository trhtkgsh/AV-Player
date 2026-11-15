using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Controls;
using Player.Core.Logic.Explorer;
using Player.Core.Models;

namespace Player.App.Features.Explorer;

// Selection mode of the items page: a long press starts it, a tap toggles a file, and the top bar acts on the selection.
public sealed partial class ItemsViewModel
{
    private readonly ExplorerSelection<MediaFileId> _selection = new();

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

    // Only a copy can be cancelled: a move is not stopped halfway.
    [ObservableProperty]
    private bool _canCancelOperation;

    private CancellationTokenSource? _operationCts;

    // The ⋮ buttons and the normal toolbar actions show only outside the selection mode.
    public bool IsBrowsing => !IsSelecting;

    partial void OnIsSelectingChanged(bool value) => OnPropertyChanged(nameof(IsBrowsing));

    [RelayCommand]
    private void BeginSelect(MediaRowViewModel row)
    {
        if (row is null || _fileOperationRunning)
        {
            return;
        }

        _selection.Add(row.Item.Id);
        UpdateSelection();
    }

    [RelayCommand]
    private void ExitSelection()
    {
        // Back and the bar's arrow leave the mode only when no operation runs.
        if (IsOperationRunning)
        {
            return;
        }

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
            var selected = _selection.Contains(row.Item.Id);
            row.IsSelected = selected;
            row.AccessibleName = selected ? _localizer.Format("Explorer.SelectedItem", row.Name) : null;
        }

        IsSelecting = _selection.IsActive;
        SelectedCount = _selection.Count;
        CanRename = SelectionRules.Allows(SelectionAction.Rename, _selection.Count, 0);
        RebuildTitle();
    }

    private List<MediaItem> SelectedItems() =>
        Rows.Where(row => _selection.Contains(row.Item.Id)).Select(row => row.Item).ToList();

    // The guard covers the whole command: a second tap while the dialog, the picker or the operation is open does nothing.
    private async Task RunActionAsync(Func<Shell, Task> action)
    {
        var shell = Shell.Current;
        if (_actionBusy || _fileOperationRunning || shell is null)
        {
            return;
        }

        _actionBusy = true;
        try
        {
            await action(shell);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            ReportError(ex);
        }
        finally
        {
            _actionBusy = false;
        }
    }

    [RelayCommand]
    private Task DeleteSelected() => RunActionAsync(async shell =>
    {
        if (!IsSelecting || SelectedItems().Count == 0)
        {
            return;
        }

        if (!await _operations.ConfirmDeleteAsync(shell, SelectedItems().Count, 0))
        {
            return;
        }

        // Taken again after the dialog: the operation uses the then-current selection.
        var items = SelectedItems();
        if (items.Count == 0)
        {
            return;
        }

        _fileOperationRunning = true;
        IsOperationRunning = true;
        OperationName = _localizer["Explorer.OperationDelete"];
        try
        {
            // The session token does not stop a delete: it runs to its result even when the page is left.
            var result = await _operations.TrashAsync(items, CancellationToken.None);
            await ApplyBatchResultAsync(result, "Explorer.SomeNotDeleted", () => AfterRemovalAsync(items));
        }
        finally
        {
            _fileOperationRunning = false;
            IsOperationRunning = false;
        }
    });

    // The selection mode ends with the operation. Success runs onSuccess. Cancelled reloads only when some files were done.
    // Denied and Failed reload and show the message.
    private async Task ApplyBatchResultAsync(FileOperationResult result, string partialKey, Func<Task> onSuccess)
    {
        _selection.Clear();
        UpdateSelection();

        switch (result.Status)
        {
            case FileOperationStatus.Success:
                await onSuccess();
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

    // One file leaves the list in place. Several files reload once.
    private Task AfterRemovalAsync(IReadOnlyList<MediaItem> items)
    {
        if (items.Count != 1)
        {
            return LoadAsync();
        }

        var item = items[0];
        return EditInPlaceAsync(() =>
        {
            _items = MediaListEdits.Remove(_items, item.Id);
            _states.Remove(item.Id);
            _metadata.Remove(item.Id);
            RebuildRows();
        });
    }

    // Copied files appear in the list only when the shown folder is the target (or the list is all media).
    private Task ReloadIfShownAsync(MediaFolder target) =>
        _bucketId is null || target.BucketId == _bucketId ? LoadAsync() : Task.CompletedTask;

    [RelayCommand]
    private Task MoveSelected() => RunActionAsync(shell => TransferAsync(shell, SelectedItems, move: true));

    [RelayCommand]
    private Task CopySelected() => RunActionAsync(shell => TransferAsync(shell, SelectedItems, move: false));

    [RelayCommand]
    private void CancelOperation() => _operationCts?.Cancel();

    // Move and copy of the selection, or of one file from its ⋮ menu. The target is picked first; the items are taken
    // again after the picker. Then the list is covered while the operation runs. Move closes an open file first and
    // cannot be cancelled; copy has its own Cancel token and is not tied to the page.
    private async Task TransferAsync(Shell shell, Func<List<MediaItem>> capture, bool move)
    {
        var items = capture();
        if (items.Count == 0)
        {
            return;
        }

        var target = await _operations.ChooseTargetAsync(shell, items, SessionToken);
        if (target is null)
        {
            return;
        }

        var current = capture();
        if (current.Count == 0)
        {
            return;
        }

        _fileOperationRunning = true;
        _operationCts = new CancellationTokenSource();
        var ct = move ? CancellationToken.None : _operationCts.Token;
        IsOperationRunning = true;
        CanCancelOperation = !move;
        OperationName = _localizer[move ? "Explorer.OperationMove" : "Explorer.OperationCopy"];
        try
        {
            var result = move
                ? await _operations.MoveAsync(current, target, ct)
                : await _operations.CopyAsync(current, target, ct);
            await ApplyBatchResultAsync(
                result,
                move ? "Explorer.SomeNotMoved" : "Explorer.SomeNotCopied",
                () => move ? AfterMoveAsync(current) : ReloadIfShownAsync(target));
        }
        finally
        {
            _operationCts.Dispose();
            _operationCts = null;
            _fileOperationRunning = false;
            IsOperationRunning = false;
            CanCancelOperation = false;
        }
    }

    // In the All files list a moved file still belongs to the list, so it reloads. In a folder it leaves the list in place.
    private Task AfterMoveAsync(IReadOnlyList<MediaItem> items) =>
        _bucketId is null ? LoadAsync() : AfterRemovalAsync(items);

    // Rename is offered for exactly one file. The mode ends first, then the normal rename prompt runs.
    [RelayCommand]
    private Task RenameSelected() => RunActionAsync(async shell =>
    {
        var items = SelectedItems();
        if (!IsSelecting || items.Count != 1 || !CanRename)
        {
            return;
        }

        _selection.Clear();
        UpdateSelection();
        await RenameFileAsync(shell, items[0]);
    });
}

