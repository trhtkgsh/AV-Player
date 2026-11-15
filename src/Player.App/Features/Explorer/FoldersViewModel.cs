using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Player.Core.Contracts;
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
    private readonly ILocalizer _localizer;

    // The folders of the last successful load. The rows are rebuilt from it when the language changes.
    private IReadOnlyList<MediaFolder> _folders = [];

    private CancellationTokenSource? _session;
    private int _loadVersion;
    private bool _flatSeen;
    private bool _openingSettings;

    // The NewTagDays the current or last load read from the settings. Null when no load has read them yet, or the last load failed.
    private int? _loadedNewTagDays;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isRefreshing;

    [ObservableProperty]
    private bool _permissionDenied;

    [ObservableProperty]
    private bool _isEmpty;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private bool _hasError;

    [ObservableProperty]
    private bool _showBusy;

    public FoldersViewModel(IMediaLibrary library, IUiSettingsStore uiSettings, ILocalizer localizer)
    {
        _library = library;
        _uiSettings = uiSettings;
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
    private async Task OpenFolder(FolderRow row)
    {
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
    }

    [RelayCommand]
    private async Task ShowAll()
    {
        try
        {
            var ct = SessionToken;
            var settings = await _uiSettings.GetAsync(ct);
            _flatSeen = true;
            await _uiSettings.SaveAsync(settings with { ViewMode = ExplorerViewMode.Flat }, ct);
            await NavigateToAllAsync();
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            ReportError(ex);
        }
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
                RebuildRows();
                IsEmpty = false;
                return;
            }

            PermissionDenied = false;
            var folders = await _library.GetFoldersAsync(settings.NewTagDays, ct);
            if (!IsCurrentLoad(version))
            {
                return;
            }

            _folders = folders;
            RebuildRows();
            IsEmpty = Folders.Count == 0;
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
            new Dictionary<string, object> { ["title"] = _localizer["Explorer.AllMedia"] });
    }

    private bool IsCurrentLoad(int version) => version == _loadVersion;

    private void StopSession()
    {
        _library.Changed -= OnLibraryChanged;
        _uiSettings.Changed -= OnSettingsChanged;
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
            // Only a changed new-tag window reloads. Null: no load has read the settings, the next load reads them.
            // A callback queued before Deactivate does nothing.
            if (_session is not null && _loadedNewTagDays is int loaded && settings.NewTagDays != loaded)
            {
                _ = LoadAsync(LoadReason.Library);
            }
        });

    private void OnLanguageChanged(object? sender, EventArgs e) => RebuildRows();

    // The rows keep the texts of the language they were made in: they are made again from the folders held now.
    private void RebuildRows()
    {
        Folders.Clear();
        foreach (var folder in _folders)
        {
            Folders.Add(new FolderRow(folder, _localizer));
        }
    }

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
