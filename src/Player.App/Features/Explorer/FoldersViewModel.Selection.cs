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

    [RelayCommand]
    private async Task DeleteSelected()
    {
        var shell = Shell.Current;
        if (_fileOperationRunning || !IsSelecting || shell is null)
        {
            return;
        }

        var buckets = _selection.Keys.ToList();
        try
        {
            var items = await _operations.FilesAsync([], buckets, SessionToken);
            if (items.Count == 0)
            {
                ExitSelection();
                return;
            }

            if (!await _operations.ConfirmDeleteAsync(shell, items.Count, buckets.Count))
            {
                return;
            }

            _fileOperationRunning = true;
            IsOperationRunning = true;
            OperationName = _localizer["Explorer.OperationDelete"];
            try
            {
                var result = await _operations.TrashAsync(items, SessionToken);
                await ApplyBatchResultAsync(result, "Explorer.SomeNotDeleted");
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
    private Task MoveSelected() => TransferSelectedAsync(move: true);

    [RelayCommand]
    private Task CopySelected() => TransferSelectedAsync(move: false);

    [RelayCommand]
    private void CancelOperation() => _operationCts?.Cancel();

    // The files of the selected folders go to the chosen folder. Move closes an open file first; copy can be cancelled.
    // The folders always reload: their counts change.
    private async Task TransferSelectedAsync(bool move)
    {
        var shell = Shell.Current;
        if (_fileOperationRunning || !IsSelecting || shell is null)
        {
            return;
        }

        var buckets = _selection.Keys.ToList();
        try
        {
            var items = await _operations.FilesAsync([], buckets, SessionToken);
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

            _fileOperationRunning = true;
            _operationCts = CancellationTokenSource.CreateLinkedTokenSource(SessionToken);
            var ct = _operationCts.Token;
            IsOperationRunning = true;
            CanCancelOperation = !move;
            OperationName = _localizer[move ? "Explorer.OperationMove" : "Explorer.OperationCopy"];
            try
            {
                var result = move
                    ? await _operations.MoveAsync(items, target, ct)
                    : await _operations.CopyAsync(items, target, ct);
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
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            ReportError(ex);
        }
    }

    // Selected folders that no longer exist in the library are dropped. The rest stay across file-browser navigation.
    private void PruneSelection()
    {
        foreach (var bucket in _selection.Keys.Where(bucket => !_folders.Any(folder => folder.BucketId == bucket)).ToList())
        {
            _selection.Remove(bucket);
        }
    }
}
