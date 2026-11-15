using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Controls;
using Player.Core.Logic.Explorer;
using Player.Core.Models;

namespace Player.App.Features.Explorer;

// Selection mode of the folders page: a long press on a media folder starts it. The selection stands for the media files in the folders.
public sealed partial class FoldersViewModel
{
    // The selected folders by bucket. Only media folders can be selected; a file-browser container cannot.
    private readonly ExplorerSelection<long> _selection = new();

    // True from the start of a delete, move or copy (its target prompt and confirmation included) until it ends. A second command does nothing meanwhile.
    private bool _actionBusy;

    // True while the operation itself runs. Opening files is blocked meanwhile.
    private bool _fileOperationRunning;

    [ObservableProperty]
    private bool _isSelecting;

    [ObservableProperty]
    private int _selectedCount;

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

    private static bool IsSelectable(FolderRow row) => row.Kind == FolderRowKind.Folder && row.Folder is not null;

    [RelayCommand]
    private void BeginSelect(FolderRow row)
    {
        if (row is null || !IsSelectable(row) || _fileOperationRunning)
        {
            return;
        }

        _selection.Add(row.Folder!.BucketId);
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

        _selection.AddRange(Folders.Where(IsSelectable).Select(row => row.Folder!.BucketId));
        UpdateSelection();
    }

    private void ToggleSelect(FolderRow row)
    {
        if (!IsSelectable(row))
        {
            return;
        }

        _selection.Toggle(row.Folder!.BucketId);
        UpdateSelection();
    }

    // Keeps the rows, the mode, the count and the title in step with the selection. Clearing the last folder leaves the mode.
    private void UpdateSelection()
    {
        for (var i = 0; i < Folders.Count; i++)
        {
            var row = Folders[i];
            var selected = IsSelectable(row) && _selection.Contains(row.Folder!.BucketId);
            if (row.Selected != selected)
            {
                Folders[i] = row with { Selected = selected };
            }
        }

        IsSelecting = _selection.IsActive;
        SelectedCount = _selection.Count;
        SetTreePath(_treePath);
        if (IsSelecting)
        {
            Title = _localizer.Format("Explorer.SelectedCount", _selection.Count);
        }
    }

    // The guard covers the whole command: a second tap while the dialog, the picker or the operation is open does nothing.
    private async Task RunActionAsync(Shell? shell, Func<Shell, Task> action)
    {
        if (_actionBusy || _fileOperationRunning || !IsSelecting || shell is null)
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

    // The files of the selected folders now. Taken again after each prompt, so the operation uses the then-current selection.
    private Task<IReadOnlyList<MediaItem>> CaptureFilesAsync() =>
        _operations.FilesAsync([], _selection.Keys.ToList(), SessionToken);

    [RelayCommand]
    private Task DeleteSelected() => RunActionAsync(Shell.Current, async shell =>
    {
        var items = await CaptureFilesAsync();
        if (items.Count == 0)
        {
            ExitSelection();
            return;
        }

        if (!await _operations.ConfirmDeleteAsync(shell, items.Count, _selection.Count))
        {
            return;
        }

        // Taken again after the dialog: the list the operation uses is the one that is selected now.
        var current = await CaptureFilesAsync();
        if (current.Count == 0)
        {
            ExitSelection();
            return;
        }

        _fileOperationRunning = true;
        IsOperationRunning = true;
        OperationName = _localizer["Explorer.OperationDelete"];
        try
        {
            // The session token does not stop a delete: it runs to its result even when the page is left.
            var result = await _operations.TrashAsync(current, CancellationToken.None);
            await ApplyBatchResultAsync(result, "Explorer.SomeNotDeleted");
        }
        finally
        {
            _fileOperationRunning = false;
            IsOperationRunning = false;
        }
    });

    // The mode ends with the operation. The folders reload once: their counts change. Denied and Failed show the message.
    private async Task ApplyBatchResultAsync(FileOperationResult result, string partialKey)
    {
        _selection.Clear();
        UpdateSelection();

        switch (result.Status)
        {
            case FileOperationStatus.Success:
                await LoadAsync(LoadReason.Library);
                break;

            case FileOperationStatus.Cancelled when result.CompletedCount > 0:
                await LoadAsync(LoadReason.Library);
                break;

            case FileOperationStatus.Cancelled:
                break;

            default:
                await LoadAsync(LoadReason.Library);
                ErrorMessage = _operations.FailureText(result, partialKey);
                break;
        }
    }

    [RelayCommand]
    private Task MoveSelected() => RunActionAsync(Shell.Current, shell => TransferSelectedAsync(shell, move: true));

    [RelayCommand]
    private Task CopySelected() => RunActionAsync(Shell.Current, shell => TransferSelectedAsync(shell, move: false));

    [RelayCommand]
    private void CancelOperation() => _operationCts?.Cancel();

    // The files of the selected folders go to the chosen folder. Move closes an open file first and cannot be cancelled;
    // copy has its own Cancel token and is not tied to the page. The folders always reload: their counts change.
    private async Task TransferSelectedAsync(Shell shell, bool move)
    {
        var items = await CaptureFilesAsync();
        if (items.Count == 0)
        {
            ExitSelection();
            return;
        }

        var target = await _operations.ChooseTargetAsync(shell, items, SessionToken);
        if (target is null)
        {
            return;
        }

        // Taken again after the picker: the operation uses the then-current selection.
        var current = await CaptureFilesAsync();
        if (current.Count == 0)
        {
            ExitSelection();
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
            await ApplyBatchResultAsync(result, move ? "Explorer.SomeNotMoved" : "Explorer.SomeNotCopied");
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

    // Only the selectable rows on screen stay selected: navigating to another level or a new list drops the others.
    private void PruneSelection() =>
        _selection.KeepOnly(Folders.Where(IsSelectable).Select(row => row.Folder!.BucketId).ToHashSet());
}
