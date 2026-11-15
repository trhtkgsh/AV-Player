using Microsoft.Maui.Controls;
using Microsoft.Maui.Devices;
using Player.Core.Contracts;
using Player.Core.Logic.Explorer;
using Player.Core.Models;

namespace Player.App.Features.Explorer;

/// <summary>
/// What the items and folder pages share in selection mode: the files behind a selection, the confirmation,
/// the batch operation (closing an open file first) and the failure text.
/// </summary>
public sealed class SelectionOperations
{
    private readonly IMediaLibrary _library;
    private readonly IFileOperations _fileOperations;
    private readonly IPlaybackCoordinator _coordinator;
    private readonly ILocalizer _localizer;

    public SelectionOperations(
        IMediaLibrary library,
        IFileOperations fileOperations,
        IPlaybackCoordinator coordinator,
        ILocalizer localizer)
    {
        _library = library;
        _fileOperations = fileOperations;
        _coordinator = coordinator;
        _localizer = localizer;
    }

    // Android 11 (API 30) and later move the file to the system trash, with the system dialog.
    public static bool DeleteWordingIsTrash =>
        DeviceInfo.Platform == DevicePlatform.Android && DeviceInfo.Version.Major >= 11;

    // The media files of the selected files and of the selected folders (each folder stands for the files in it). No duplicates.
    public async Task<IReadOnlyList<MediaItem>> FilesAsync(
        IEnumerable<MediaItem> files,
        IEnumerable<long> buckets,
        CancellationToken ct)
    {
        var all = new List<MediaItem>(files);
        foreach (var bucket in buckets)
        {
            all.AddRange(await _library.GetItemsAsync(new MediaQuery(bucket, null), ct));
        }

        return SelectionRules.Distinct(all, item => item.Id);
    }

    // "Move 12 files to the trash?" for files; "Move 12 files in 3 folders to the trash?" when folders are selected.
    public async Task<bool> ConfirmDeleteAsync(Shell shell, int files, int folders)
    {
        var count = folders == 0
            ? _localizer.Plural("Explorer.Files", files)
            : _localizer.Format("Explorer.FilesInFolders", files, folders);
        var question = _localizer.Format(DeleteWordingIsTrash ? "Explorer.TrashConfirm" : "Explorer.DeleteConfirm", count);

        return await shell.DisplayAlertAsync(
            _localizer["Explorer.Delete"],
            question,
            _localizer["Explorer.Delete"],
            _localizer["Common.Cancel"]);
    }

    // The file that is open in playback is closed first. Then the batch goes to the trash.
    public async Task<FileOperationResult> TrashAsync(IReadOnlyList<MediaItem> items, CancellationToken ct)
    {
        if (_coordinator.Current is { } current && items.Any(item => item.Id == current.Id))
        {
            await _coordinator.CloseAsync(ct);
        }

        return await _fileOperations.TrashAsync(items, ct);
    }

    // The localized line for a batch, then the implementation's English error (or a localized fallback when there is none).
    public string FailureText(FileOperationResult result, string partialKey)
    {
        var detail = string.IsNullOrWhiteSpace(result.Error)
            ? _localizer[result.Status == FileOperationStatus.Denied ? "Explorer.OperationDenied" : "Explorer.OperationFailed"]
            : result.Error;
        return $"{_localizer[partialKey]} {detail}";
    }
}
