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

    private CancellationTokenSource? _session;
    private int _loadVersion;
    private bool _flatSeen;
    private bool _openingSettings;

    // The NewTagDays the folder list on screen was loaded with. Null while no list is shown.
    private int? _shownNewTagDays;

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

    public FoldersViewModel(IMediaLibrary library, IUiSettingsStore uiSettings)
    {
        _library = library;
        _uiSettings = uiSettings;
    }

    public ObservableCollection<FolderRow> Folders { get; } = new();

    private CancellationToken SessionToken => _session?.Token ?? new CancellationToken(canceled: true);

    public async Task ActivateAsync()
    {
        StopSession();
        _session = new CancellationTokenSource();
        _library.Changed += OnLibraryChanged;
        _uiSettings.Changed += OnSettingsChanged;
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
        catch (Exception)
        {
            ErrorMessage = "Settings could not be opened.";
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
                Folders.Clear();
                _shownNewTagDays = null;
                IsEmpty = false;
                return;
            }

            PermissionDenied = false;
            var folders = await _library.GetFoldersAsync(settings.NewTagDays, ct);
            if (!IsCurrentLoad(version))
            {
                return;
            }

            Folders.Clear();
            foreach (var folder in folders)
            {
                Folders.Add(new FolderRow(folder));
            }

            _shownNewTagDays = settings.NewTagDays;
            IsEmpty = Folders.Count == 0;
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            if (IsCurrentLoad(version))
            {
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
            new Dictionary<string, object> { ["title"] = "All media" });
    }

    private bool IsCurrentLoad(int version) => version == _loadVersion;

    private void StopSession()
    {
        _library.Changed -= OnLibraryChanged;
        _uiSettings.Changed -= OnSettingsChanged;

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
            // Only the new-tag window changes the folder list. A callback queued before Deactivate does nothing.
            if (_session is not null && settings.NewTagDays != _shownNewTagDays)
            {
                _ = LoadAsync(LoadReason.Library);
            }
        });

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
