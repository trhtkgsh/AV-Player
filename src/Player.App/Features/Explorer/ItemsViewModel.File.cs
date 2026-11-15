using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Devices;
using Player.Core.Contracts;
using Player.Core.Logic.Explorer;
using Player.Core.Models;

namespace Player.App.Features.Explorer;

// The file options of the items page: rename and delete of one file, opened from its ⋮ button.
public sealed partial class ItemsViewModel
{
    private bool _choosingFileOptions;

    // True from the start of a rename or delete until its result is applied. Opening files is blocked meanwhile.
    private bool _fileOperationRunning;

    [RelayCommand]
    private async Task OpenOptions(MediaRowViewModel row)
    {
        if (_choosingFileOptions || _fileOperationRunning)
        {
            return;
        }

        _choosingFileOptions = true;
        try
        {
            var shell = Shell.Current;
            if (shell is null)
            {
                return;
            }

            var renameText = _localizer["Explorer.Rename"];
            var deleteText = _localizer["Explorer.Delete"];
            var moveText = _localizer["Explorer.Move"];
            var copyText = _localizer["Explorer.Copy"];
            var choice = await shell.DisplayActionSheetAsync(
                row.Name,
                _localizer["Common.Cancel"],
                null,
                renameText,
                deleteText,
                moveText,
                copyText);

            if (choice == renameText)
            {
                await RenameFileAsync(shell, row.Item);
            }
            else if (choice == deleteText)
            {
                await DeleteFileAsync(shell, row.Item);
            }
            else if (choice == moveText)
            {
                await TransferAsync(shell, [row.Item], move: true);
            }
            else if (choice == copyText)
            {
                await TransferAsync(shell, [row.Item], move: false);
            }
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
            _choosingFileOptions = false;
        }
    }

    // The prompt shows the base name only. A problem is shown in the prompt, which asks again with the typed text.
    private async Task RenameFileAsync(Shell shell, MediaItem item)
    {
        var ct = SessionToken;
        var (baseName, extension) = FileNameRules.Split(item.DisplayName);
        var typed = baseName;
        var message = _localizer["Explorer.RenameHint"];

        while (true)
        {
            var input = await shell.DisplayPromptAsync(
                _localizer["Explorer.RenameTitle"],
                message,
                _localizer["Explorer.Rename"],
                _localizer["Common.Cancel"],
                null,
                FileNameRules.MaxLength,
                Keyboard.Text,
                typed);

            if (input is null)
            {
                return;
            }

            var problem = FileNameRules.Check(input, extension, item.DisplayName);
            if (problem == FileNameProblem.Unchanged)
            {
                return;
            }

            if (problem != FileNameProblem.None)
            {
                typed = input;
                message = ProblemText(problem);
                continue;
            }

            _fileOperationRunning = true;
            try
            {
                await CloseIfOpenAsync(item, ct);
                var result = await _fileOperations.RenameAsync(item, FileNameRules.BuildName(input, extension), ct);
                await ApplyFileResultAsync(result, "Explorer.RenameFailed", requiresItem: true, updated =>
                {
                    // Success without an item is handled by the reload in ApplyFileResultAsync.
                    if (updated is null)
                    {
                        return;
                    }

                    _items = MediaListEdits.Replace(_items, item.Id, updated);
                    // Keep the row's state on screen under the new id until the next reload.
                    if (_states.Remove(item.Id, out var state))
                    {
                        _states[updated.Id] = state;
                    }

                    if (_metadata.Remove(item.Id, out var metadata))
                    {
                        _metadata[updated.Id] = metadata;
                    }

                    RebuildRows();
                });
            }
            finally
            {
                _fileOperationRunning = false;
            }

            return;
        }
    }

    private async Task DeleteFileAsync(Shell shell, MediaItem item)
    {
        var ct = SessionToken;
        var question = _localizer.Format(
            SelectionOperations.DeleteWordingIsTrash ? "Explorer.TrashConfirm" : "Explorer.DeleteConfirm",
            item.DisplayName);

        var confirmed = await shell.DisplayAlertAsync(
            _localizer["Explorer.Delete"],
            question,
            _localizer["Explorer.Delete"],
            _localizer["Common.Cancel"]);
        if (!confirmed)
        {
            return;
        }

        _fileOperationRunning = true;
        try
        {
            await CloseIfOpenAsync(item, ct);
            var result = await _fileOperations.TrashAsync([item], ct);
            await ApplyFileResultAsync(result, "Explorer.DeleteFailed", requiresItem: false, _ =>
            {
                _items = MediaListEdits.Remove(_items, item.Id);
                _states.Remove(item.Id);
                _metadata.Remove(item.Id);
                RebuildRows();
            });
        }
        finally
        {
            _fileOperationRunning = false;
        }
    }

    // Android 11 (API 30) and later: see SelectionOperations.DeleteWordingIsTrash.

    // The file that is open in playback (also in background audio or Picture-in-Picture) is closed first.
    private async Task CloseIfOpenAsync(MediaItem item, CancellationToken ct)
    {
        if (_coordinator.Current?.Id == item.Id)
        {
            await _coordinator.CloseAsync(ct);
        }
    }

    // Success applies the edit in place. A rename that comes back without an item reloads instead.
    // Denied and Failed show a message. Cancelled does nothing.
    private async Task ApplyFileResultAsync(
        FileOperationResult result,
        string failedKey,
        bool requiresItem,
        Action<MediaItem?> applyEdit)
    {
        switch (result.Status)
        {
            case FileOperationStatus.Success when !requiresItem || result.Updated is not null:
                await EditInPlaceAsync(() => applyEdit(result.Updated));
                break;

            case FileOperationStatus.Success:
                await LoadAsync();
                break;

            case FileOperationStatus.Cancelled:
                break;

            default:
                ErrorMessage = FileFailureText(result, failedKey);
                break;
        }
    }

    // A running load would overwrite the edit with its older result, so it is dropped first.
    // Then one fresh load runs, so the list ends consistent with the store.
    private async Task EditInPlaceAsync(Action edit)
    {
        var loadWasRunning = IsLoading;
        _loadVersion++;
        edit();
        if (loadWasRunning)
        {
            await LoadAsync();
        }
    }

    private string FileFailureText(FileOperationResult result, string failedKey)
    {
        var text = _localizer[result.Status == FileOperationStatus.Denied ? "Explorer.OperationDenied" : failedKey];
        return string.IsNullOrWhiteSpace(result.Error)
            ? text
            : _localizer.Format("Explorer.OperationDetails", text, result.Error);
    }

    private string ProblemText(FileNameProblem problem) => problem switch
    {
        FileNameProblem.Empty => _localizer["Explorer.NameEmpty"],
        FileNameProblem.InvalidCharacter => _localizer["Explorer.NameInvalidCharacters"],
        FileNameProblem.Reserved => _localizer["Explorer.NameReserved"],
        FileNameProblem.TooLong => _localizer.Format("Explorer.NameTooLong", FileNameRules.MaxLength),
        _ => _localizer["Explorer.RenameHint"],
    };
}
