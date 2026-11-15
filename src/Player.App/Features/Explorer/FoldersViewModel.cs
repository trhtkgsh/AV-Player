using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Player.Core.Contracts;
using Player.Core.Logic.Explorer;
using Player.Core.Models;

namespace Player.App.Features.Explorer;

/// <summary>
/// The folders page: one row per MediaStore bucket.
/// Everything that touches <see cref="Folders"/> or the observable properties runs on the UI thread.
/// </summary>
public sealed partial class FoldersViewModel : ObservableObject
{
    private const string ItemsRoute = "explorer/items";

    // Why a load runs. Only an activation may move the stored view mode or navigate.
    private enum LoadReason { Activate, Library, Refresh, Retry }

    private readonly IMediaLibrary _library;
    private readonly IUiSettingsStore _uiSettings;
    private readonly IFolderSettingsStore _folderSettings;
    private readonly ILocalizer _localizer;

    // The folders of the last successful load, and the per-folder settings read with them.
    // The rows are rebuilt from them when the language or a setting changes.
    private IReadOnlyList<MediaFolder> _folders = [];
    private IReadOnlyDictionary<long, FolderSettings> _folderSettingsByBucket = new Dictionary<long, FolderSettings>();

    // The UI settings of the last load or change. Null before the first one.
    private UiSettings? _settings;

    // The file-browser path: segments joined with '/', empty at the storage root.
    private string _treePath = "";

    // True once a load has finished, with the folders or with a denied permission.
    private bool _loaded;

    private CancellationTokenSource? _session;
    private int _loadVersion;
    private bool _flatSeen;
    private bool _openingSettings;
    private bool _choosingView;
    private bool _choosingOptions;
    private bool _opening;

    // The speeds of the folder speed sheet, after the general default.
    private static readonly float[] FolderSpeeds = [0.5f, 0.75f, 1.0f, 1.25f, 1.5f, 1.75f, 2.0f, 2.5f, 3.0f];

    // The NewTagDays the current or last load read from the settings. Null when no load has read them yet, or the last load failed.
    private int? _loadedNewTagDays;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isRefreshing;

    [ObservableProperty]
    private bool _permissionDenied;

    [ObservableProperty]
    private bool _showNoFolders;

    [ObservableProperty]
    private bool _showNoFavorites;

    // The file-browser folder name below the root. Null shows the app name.
    [ObservableProperty]
    private string? _title;

    // True in the file browser below the storage root: the back button goes one level up.
    [ObservableProperty]
    private bool _canGoUp;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private bool _hasError;

    [ObservableProperty]
    private bool _showBusy;

    public FoldersViewModel(IMediaLibrary library, IUiSettingsStore uiSettings, IFolderSettingsStore folderSettings, ILocalizer localizer)
    {
        _library = library;
        _uiSettings = uiSettings;
        _folderSettings = folderSettings;
        _localizer = localizer;
    }

    public ObservableCollection<FolderRow> Folders { get; } = new();

    private CancellationToken SessionToken => _session?.Token ?? new CancellationToken(canceled: true);

    public async Task ActivateAsync()
    {
        StopSession();
        _session = new CancellationTokenSource();
        _library.Changed += OnLibraryChanged;
        _uiSettings.Changed += OnSettingsChanged;
        _folderSettings.Changed += OnFolderSettingsChanged;
        _localizer.Changed += OnLanguageChanged;
        await LoadAsync(LoadReason.Activate);
    }

    public void Deactivate()
    {
        StopSession();
        _loadVersion++;
    }

    [RelayCommand]
    private async Task Refresh()
    {
        IsRefreshing = true;
        await LoadAsync(LoadReason.Refresh);
    }

    [RelayCommand]
    private async Task OpenRow(FolderRow row)
    {
        // A file-browser node that holds more goes one level down in this page.
        if (row.Kind == FolderRowKind.Container && row.TreePath is { } path)
        {
            SetTreePath(path);
            RebuildRows();
            return;
        }

        if (row.Folder is null || _opening)
        {
            return;
        }

        _opening = true;
        try
        {
            var shell = Shell.Current;
            if (shell is null)
            {
                return;
            }

            await shell.GoToAsync(
                ItemsRoute,
                new Dictionary<string, object>
                {
                    ["bucketId"] = row.Folder.BucketId,
                    ["title"] = row.Name,
                });
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
            _opening = false;
        }
    }

    [RelayCommand]
    private async Task ChooseView()
    {
        if (_choosingView)
        {
            return;
        }

        _choosingView = true;
        try
        {
            var shell = Shell.Current;
            if (shell is null)
            {
                return;
            }

            var ct = SessionToken;
            var current = (await _uiSettings.GetAsync(ct)).ViewMode;
            var choices = new (string Text, ExplorerViewMode Mode)[]
            {
                (_localizer["Explorer.ViewFolders"], ExplorerViewMode.Folders),
                (_localizer["Explorer.ViewTree"], ExplorerViewMode.Tree),
                (_localizer["Explorer.ViewFavorites"], ExplorerViewMode.Favorites),
                (_localizer["Explorer.ViewFlat"], ExplorerViewMode.Flat),
            };
            var labels = choices
                .Select(c => c.Mode == current ? "✓ " + c.Text : c.Text)
                .ToArray();

            var choice = await shell.DisplayActionSheetAsync(
                _localizer["Explorer.View"],
                _localizer["Common.Cancel"],
                null,
                labels);

            // A cancelled or dismissed sheet gives no matching text, so the mode stays.
            var index = Array.IndexOf(labels, choice);
            if (index >= 0)
            {
                await SetViewModeAsync(choices[index].Mode, ct);
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
            _choosingView = false;
        }
    }

    private async Task SetViewModeAsync(ExplorerViewMode mode, CancellationToken ct)
    {
        // Read the latest stored settings: only the view mode changes.
        var settings = await _uiSettings.GetAsync(ct);
        var saved = settings with { ViewMode = mode };
        await _uiSettings.SaveAsync(saved, ct);
        ApplySettings(saved);

        if (mode == ExplorerViewMode.Flat)
        {
            await NavigateToAllAsync();
        }
    }

    [RelayCommand]
    private async Task OpenOptions(FolderRow row)
    {
        if (row.Folder is not { } folder || _choosingOptions)
        {
            return;
        }

        _choosingOptions = true;
        try
        {
            var shell = Shell.Current;
            if (shell is null)
            {
                return;
            }

            // The state shown in the sheet is read now, not taken from the rows.
            var ct = SessionToken;
            var current = await _folderSettings.GetAsync(folder.BucketId, ct);
            var favoriteText = _localizer[current?.Favorite == true ? "Explorer.RemoveFavorite" : "Explorer.AddFavorite"];
            var speedName = current?.Speed is { } speed ? FormatSpeed(speed) : _localizer["Explorer.FolderSpeedGeneral"];
            var speedText = _localizer.Format("Explorer.FolderSpeed", speedName);

            var choice = await shell.DisplayActionSheetAsync(
                folder.Name,
                _localizer["Common.Cancel"],
                null,
                favoriteText,
                speedText);

            if (choice == favoriteText)
            {
                await SaveFolderSettingsAsync(folder.BucketId, s => s with { Favorite = !s.Favorite }, ct);
            }
            else if (choice == speedText)
            {
                await ChooseFolderSpeedAsync(shell, folder, ct);
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
            _choosingOptions = false;
        }
    }

    private async Task ChooseFolderSpeedAsync(Shell shell, MediaFolder folder, CancellationToken ct)
    {
        var labels = new[] { _localizer["Explorer.FolderSpeedUseGeneral"] }
            .Concat(FolderSpeeds.Select(FormatSpeed))
            .ToArray();

        var choice = await shell.DisplayActionSheetAsync(
            folder.Name,
            _localizer["Common.Cancel"],
            null,
            labels);

        var index = Array.IndexOf(labels, choice);
        if (index < 0)
        {
            return;
        }

        // The first choice is the general default: no speed for this folder.
        float? speed = index == 0 ? null : FolderSpeeds[index - 1];
        await SaveFolderSettingsAsync(folder.BucketId, s => s with { Speed = speed }, ct);
    }

    private async Task SaveFolderSettingsAsync(long bucketId, Func<FolderSettings, FolderSettings> change, CancellationToken ct)
    {
        try
        {
            // Read the latest stored entry: only the changed member moves.
            var current = await _folderSettings.GetAsync(bucketId, ct) ?? new FolderSettings(bucketId);
            await _folderSettings.SaveAsync(change(current), ct);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
            ErrorMessage = _localizer["Explorer.SaveFailed"];
        }
    }

    private static string FormatSpeed(float speed) => speed.ToString("0.0#", CultureInfo.InvariantCulture) + "x";

    [RelayCommand]
    private void GoUp()
    {
        if (_treePath.Length == 0)
        {
            return;
        }

        var slash = _treePath.LastIndexOf('/');
        SetTreePath(slash < 0 ? "" : _treePath[..slash]);
        RebuildRows();
    }

    [RelayCommand]
    private Task RetryPermission() => LoadAsync(LoadReason.Retry);

    [RelayCommand]
    private async Task OpenSettings()
    {
        if (_openingSettings)
        {
            return;
        }

        _openingSettings = true;
        try
        {
            if (Shell.Current is { } shell)
            {
                await shell.GoToAsync("settings");
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception)
        {
            ErrorMessage = _localizer["Explorer.SettingsFailed"];
        }
        finally
        {
            _openingSettings = false;
        }
    }

    [RelayCommand]
    private void OpenAppSettings()
    {
        try
        {
            AppInfo.Current.ShowSettingsUI();
        }
        catch (Exception ex)
        {
            ReportError(ex);
        }
    }

    private async Task LoadAsync(LoadReason reason)
    {
        var version = ++_loadVersion;
        var ct = SessionToken;
        IsLoading = true;
        ErrorMessage = null;

        try
        {
            var settings = await _uiSettings.GetAsync(ct);
            if (!IsCurrentLoad(version))
            {
                return;
            }

            _loadedNewTagDays = settings.NewTagDays;

            if (reason == LoadReason.Activate && settings.ViewMode == ExplorerViewMode.Flat)
            {
                if (!_flatSeen)
                {
                    // First time the stored view is Flat: go to the flat list once.
                    await NavigateToAllAsync();
                    return;
                }

                // The user came back to this page while the stored view is still Flat: show folders.
                settings = settings with { ViewMode = ExplorerViewMode.Folders };
                await _uiSettings.SaveAsync(settings, ct);
                if (!IsCurrentLoad(version))
                {
                    return;
                }
            }

            var permitted = await _library.EnsureReadPermissionAsync(ct);
            if (!IsCurrentLoad(version))
            {
                return;
            }

            if (!permitted)
            {
                PermissionDenied = true;
                _folders = [];
                _loaded = true;
                ApplySettings(settings);
                return;
            }

            PermissionDenied = false;
            var folders = await _library.GetFoldersAsync(settings.NewTagDays, ct);
            if (!IsCurrentLoad(version))
            {
                return;
            }

            var folderSettings = await _folderSettings.GetAllAsync(ct);
            if (!IsCurrentLoad(version))
            {
                return;
            }

            _folders = folders;
            _folderSettingsByBucket = folderSettings;
            _loaded = true;
            ApplySettings(settings);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            if (IsCurrentLoad(version))
            {
                _loadedNewTagDays = null;
                ReportError(ex);
            }
        }
        finally
        {
            // Only the newest load owns the loading flags.
            if (IsCurrentLoad(version))
            {
                IsLoading = false;
                IsRefreshing = false;
            }
        }
    }

    private async Task NavigateToAllAsync()
    {
        _flatSeen = true;

        var shell = Shell.Current;
        if (shell is null)
        {
            return;
        }

        await shell.GoToAsync(
            ItemsRoute,
            new Dictionary<string, object>());
    }

    private bool IsCurrentLoad(int version) => version == _loadVersion;

    private void StopSession()
    {
        _library.Changed -= OnLibraryChanged;
        _uiSettings.Changed -= OnSettingsChanged;
        _folderSettings.Changed -= OnFolderSettingsChanged;
        _localizer.Changed -= OnLanguageChanged;

        _session?.Cancel();
        _session?.Dispose();
        _session = null;
    }

    private void OnLibraryChanged(object? sender, EventArgs e) =>
        MainThread.BeginInvokeOnMainThread(() =>
        {
            // A callback queued before Deactivate does nothing.
            if (_session is not null)
            {
                _ = LoadAsync(LoadReason.Library);
            }
        });

    private void OnSettingsChanged(object? sender, UiSettings settings) =>
        MainThread.BeginInvokeOnMainThread(() =>
        {
            // A callback queued before Deactivate does nothing.
            if (_session is null)
            {
                return;
            }

            // Only a changed new-tag window reloads. Null: no load has read the settings, the next load reads them.
            if (_loadedNewTagDays is int loaded && settings.NewTagDays != loaded)
            {
                _ = LoadAsync(LoadReason.Library);
                return;
            }

            // A view mode or audio-only change shows at once.
            if (_loaded)
            {
                ApplySettings(settings);
            }
        });

    private void OnFolderSettingsChanged(object? sender, EventArgs e) =>
        MainThread.BeginInvokeOnMainThread(() =>
        {
            // A callback queued before Deactivate does nothing.
            if (_session is not null)
            {
                _ = ReloadFolderSettingsAsync();
            }
        });

    private async Task ReloadFolderSettingsAsync()
    {
        try
        {
            _folderSettingsByBucket = await _folderSettings.GetAllAsync(SessionToken);
            RebuildRows();
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            ReportError(ex);
        }
    }

    private void OnLanguageChanged(object? sender, EventArgs e) => RebuildRows();

    private void ApplySettings(UiSettings settings)
    {
        // The file browser starts again at the root when the view mode changes.
        if (settings.ViewMode != _settings?.ViewMode)
        {
            SetTreePath("");
        }

        _settings = settings;
        RebuildRows();
    }

    private void SetTreePath(string path)
    {
        _treePath = path;
        CanGoUp = path.Length > 0;
        Title = path.Length > 0 ? path[(path.LastIndexOf('/') + 1)..] : null;
    }

    // The rows keep the texts of the language they were made in: they are made again from the folders held now.
    private void RebuildRows()
    {
        var mode = _settings?.ViewMode ?? ExplorerViewMode.Folders;
        var includeAudioOnly = _settings?.IncludeAudioOnlyFolders ?? true;

        // Audio-only folders are left out before any view is built, so an empty container disappears with them.
        var visible = _folders.Where(f => includeAudioOnly || !f.AudioOnly).ToList();

        Folders.Clear();
        switch (mode)
        {
            case ExplorerViewMode.Favorites:
                foreach (var folder in visible.Where(IsFavorite))
                {
                    Folders.Add(FolderRowFor(folder));
                }

                break;

            case ExplorerViewMode.Tree:
                AddTreeRows(visible);
                break;

            default:
                foreach (var folder in visible)
                {
                    Folders.Add(FolderRowFor(folder));
                }

                break;
        }

        var empty = _loaded && !PermissionDenied && Folders.Count == 0;
        ShowNoFavorites = empty && mode == ExplorerViewMode.Favorites;
        ShowNoFolders = empty && mode != ExplorerViewMode.Favorites;
    }

    private void AddTreeRows(IReadOnlyList<MediaFolder> visible)
    {
        if (_treePath.Length > 0 && CurrentTreeFolder(visible) is { } current)
        {
            Folders.Add(new FolderRow(FolderRowKind.FilesHere, current.Name, current, current.ItemCount, current.NewCount, _localizer, null, IsFavorite(current)));
        }

        foreach (var node in FolderTree.GetChildren(visible, _treePath))
        {
            var folder = node.Folder;
            Folders.Add(node.FolderCount == 1 && folder is not null
                ? new FolderRow(FolderRowKind.Folder, folder.Name, folder, node.ItemCount, node.NewCount, _localizer, null, IsFavorite(folder))
                : new FolderRow(FolderRowKind.Container, folder?.Name ?? node.Name, folder, node.ItemCount, node.NewCount, _localizer, node.Path, IsFavorite(folder)));
        }
    }

    private FolderRow FolderRowFor(MediaFolder folder) =>
        new(FolderRowKind.Folder, folder.Name, folder, folder.ItemCount, folder.NewCount, _localizer, null, IsFavorite(folder));

    // The media folder at the current file-browser path, or null when the path is only a container.
    private MediaFolder? CurrentTreeFolder(IReadOnlyList<MediaFolder> visible)
    {
        var slash = _treePath.LastIndexOf('/');
        var parent = slash < 0 ? "" : _treePath[..slash];
        return FolderTree.GetChildren(visible, parent).FirstOrDefault(n => n.Path == _treePath)?.Folder;
    }

    private bool IsFavorite(MediaFolder? folder) =>
        folder is not null && _folderSettingsByBucket.TryGetValue(folder.BucketId, out var settings) && settings.Favorite;

    partial void OnErrorMessageChanged(string? value)
    {
        HasError = !string.IsNullOrEmpty(value);
    }

    partial void OnIsLoadingChanged(bool value)
    {
        UpdateShowBusy();
    }

    partial void OnIsRefreshingChanged(bool value)
    {
        UpdateShowBusy();
    }

    // The centered spinner is for the first load only; a pull-to-refresh shows its own spinner.
    private void UpdateShowBusy() => ShowBusy = IsLoading && !IsRefreshing;

    private void ReportError(Exception ex) => ErrorMessage = ex.Message;
}
