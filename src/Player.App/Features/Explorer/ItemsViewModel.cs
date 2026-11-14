using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Player.Core.Contracts;
using Player.Core.Logic.Explorer;
using Player.Core.Models;

namespace Player.App.Features.Explorer;

/// <summary>
/// The items page: one bucket or the flat list, sorted, filtered and with lazy metadata.
/// Everything that touches <see cref="Rows"/> or the observable properties runs on the UI thread.
/// </summary>
public sealed partial class ItemsViewModel : ObservableObject, IQueryAttributable
{
    private const int PrefetchBefore = 2;
    private const int PrefetchAfter = 6;
    private const int InitialVisibleLast = 12;
    private const int SearchDebounceMs = 150;

    private readonly IMediaLibrary _library;
    private readonly IMediaMetadataService _metadataService;
    private readonly IPlaybackStateStore _stateStore;
    private readonly IUiSettingsStore _uiSettings;
    private readonly IPlaybackCoordinator _coordinator;
    private readonly TimeProvider _timeProvider;

    private readonly Dictionary<MediaFileId, CancellationTokenSource> _pending = new();

    private CancellationTokenSource? _session;
    private CancellationTokenSource? _searchDebounce;
    private long? _bucketId;
    private UiSettings _settings = new();
    private IReadOnlyList<MediaItem> _items = [];
    private Dictionary<MediaFileId, MediaMetadata> _metadata = new();
    private Dictionary<MediaFileId, PlaybackState> _states = new();
    private Dictionary<MediaFileId, MediaRowViewModel> _rows = new();
    private int _loadVersion;
    private bool _settingsLoaded;

    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isRefreshing;

    [ObservableProperty]
    private bool _permissionDenied;

    [ObservableProperty]
    private bool _isEmpty;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private SortField _sortField;

    [ObservableProperty]
    private SortDirection _sortDirection;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private bool _hasError;

    [ObservableProperty]
    private bool _showBusy;

    public ItemsViewModel(
        IMediaLibrary library,
        IMediaMetadataService metadataService,
        IPlaybackStateStore stateStore,
        IUiSettingsStore uiSettings,
        IPlaybackCoordinator coordinator,
        TimeProvider timeProvider)
    {
        _library = library;
        _metadataService = metadataService;
        _stateStore = stateStore;
        _uiSettings = uiSettings;
        _coordinator = coordinator;
        _timeProvider = timeProvider;
    }

    public ObservableCollection<MediaRowViewModel> Rows { get; } = new();

    private CancellationToken SessionToken => _session?.Token ?? new CancellationToken(canceled: true);

    /// <summary>Navigation parameters: "bucketId" (long, absent means all folders) and "title" (string).</summary>
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        _bucketId = query.TryGetValue("bucketId", out var bucket) && bucket is long id ? (long?)id : null;
        Title = query.TryGetValue("title", out var title) && title is string text ? text : string.Empty;
    }

    public async Task ActivateAsync()
    {
        StopSession();
        _session = new CancellationTokenSource();
        _library.Changed += OnLibraryChanged;
        _uiSettings.Changed += OnSettingsChanged;
        await LoadAsync();
    }

    public void Deactivate()
    {
        StopSession();
        _loadVersion++;
    }

    public void VisibleRangeChanged(int firstIndex, int lastIndex)
    {
        var session = _session;
        if (session is null)
        {
            return;
        }

        var from = Math.Max(0, firstIndex - PrefetchBefore);
        var to = Math.Min(Rows.Count - 1, lastIndex + PrefetchAfter);

        var wanted = new HashSet<MediaFileId>();
        for (var i = from; i <= to; i++)
        {
            wanted.Add(Rows[i].Item.Id);
        }

        foreach (var id in _pending.Keys.Where(id => !wanted.Contains(id)).ToList())
        {
            _pending[id].Cancel();
            _pending.Remove(id);
        }

        for (var i = from; i <= to; i++)
        {
            var item = Rows[i].Item;
            if (Rows[i].Metadata is not null || _pending.ContainsKey(item.Id))
            {
                continue;
            }

            var request = CancellationTokenSource.CreateLinkedTokenSource(session.Token);
            _pending[item.Id] = request;
            _ = RequestMetadataAsync(item, request);
        }
    }

    [RelayCommand]
    private async Task Refresh()
    {
        IsRefreshing = true;
        await LoadAsync();
    }

    [RelayCommand]
    private async Task Open(MediaRowViewModel row)
    {
        try
        {
            await _coordinator.OpenAsync(row.Item, Rows.Select(r => r.Item).ToList(), SessionToken);
            var shell = Shell.Current;
            if (shell is not null)
            {
                await shell.GoToAsync("player");
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

    [RelayCommand]
    private Task SetSort(SortField field) => ApplySortAsync(field);

    [RelayCommand]
    private async Task ChooseSort()
    {
        try
        {
            var shell = Shell.Current;
            if (shell is null)
            {
                return;
            }

            var choice = await shell.DisplayActionSheetAsync(
                "Sort by", "Cancel", null, "Name", "Date added", "Size", "Duration");

            SortField? field = choice switch
            {
                "Name" => SortField.Name,
                "Date added" => SortField.DateAdded,
                "Size" => SortField.Size,
                "Duration" => SortField.Duration,
                _ => null,
            };

            if (field is not null)
            {
                await ApplySortAsync(field.Value);
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

    private async Task ApplySortAsync(SortField field)
    {
        try
        {
            // Until the first load has read the saved settings, read them now. Never save defaults.
            if (!_settingsLoaded)
            {
                _settings = await _uiSettings.GetAsync(SessionToken);
                _settingsLoaded = true;
                SortField = _settings.SortField;
                SortDirection = _settings.SortDirection;
            }

            var direction = field != SortField
                ? SortDirection.Ascending
                : SortDirection == SortDirection.Ascending ? SortDirection.Descending : SortDirection.Ascending;

            SortField = field;
            SortDirection = direction;

            _settings = _settings with { SortField = field, SortDirection = direction };
            await _uiSettings.SaveAsync(_settings, SessionToken);
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

    [RelayCommand]
    private Task RetryPermission() => LoadAsync();

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

    // "No media files" is not shown while a load is in flight.
    private void UpdateIsEmpty() => IsEmpty = Rows.Count == 0 && !PermissionDenied && !IsLoading;

    partial void OnSearchTextChanged(string value)
    {
        _searchDebounce?.Cancel();
        _searchDebounce?.Dispose();
        _searchDebounce = null;

        if (_session is null)
        {
            return;
        }

        _searchDebounce = new CancellationTokenSource();
        _ = DebounceRebuildAsync(_searchDebounce.Token);
    }

    private async Task DebounceRebuildAsync(CancellationToken ct)
    {
        try
        {
            await Task.Delay(SearchDebounceMs, ct);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        // Rebuilds from the fields held now, also while a load is running. A session that ended is skipped.
        if (ct.IsCancellationRequested)
        {
            return;
        }

        RebuildRows();
    }

    private bool IsCurrentLoad(int version) => version == _loadVersion;

    private async Task LoadAsync()
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

            _settings = settings;
            _settingsLoaded = true;
            SortField = _settings.SortField;
            SortDirection = _settings.SortDirection;

            var permitted = await _library.EnsureReadPermissionAsync(ct);
            if (!IsCurrentLoad(version))
            {
                return;
            }

            if (!permitted)
            {
                PermissionDenied = true;
                _items = [];
                RebuildRows();
                return;
            }

            PermissionDenied = false;
            var items = await _library.GetItemsAsync(new MediaQuery(_bucketId, null), ct);
            if (!IsCurrentLoad(version))
            {
                return;
            }

            var ids = items.Select(item => item.Id).ToList();
            var states = await _stateStore.GetManyAsync(ids, ct);
            if (!IsCurrentLoad(version))
            {
                return;
            }

            var cached = await _metadataService.GetCachedAsync(ids, ct);
            if (!IsCurrentLoad(version))
            {
                return;
            }

            _items = items;
            _states = new Dictionary<MediaFileId, PlaybackState>(states);
            _metadata = new Dictionary<MediaFileId, MediaMetadata>(cached);
            RebuildRows();
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
                UpdateIsEmpty();
            }
        }
    }

    private void RebuildRows()
    {
        var previous = _rows;
        _rows = new Dictionary<MediaFileId, MediaRowViewModel>(_items.Count);
        foreach (var item in _items)
        {
            _rows[item.Id] = previous.TryGetValue(item.Id, out var existing) && existing.Item == item
                ? existing
                : new MediaRowViewModel(item);
        }

        var ordered = SearchFilter.Apply(
            MediaSorter.Sort(_items, SortField, SortDirection, _metadata),
            SearchText);

        Rows.Clear();
        foreach (var item in ordered)
        {
            var row = _rows[item.Id];
            ApplyRow(row);
            Rows.Add(row);
        }

        UpdateIsEmpty();
        VisibleRangeChanged(0, InitialVisibleLast);
    }

    private void ApplyRow(MediaRowViewModel row)
    {
        _metadata.TryGetValue(row.Item.Id, out var metadata);
        _states.TryGetValue(row.Item.Id, out var state);
        row.Apply(metadata, state, _settings, _timeProvider.GetUtcNow().ToUnixTimeSeconds(), TimeZoneInfo.Local);
    }

    private async Task RequestMetadataAsync(MediaItem item, CancellationTokenSource request)
    {
        var id = item.Id;
        try
        {
            var metadata = await _metadataService.GetOrCreateAsync(item, request.Token);
            if (metadata is null || request.Token.IsCancellationRequested || _session is null)
            {
                return;
            }

            _metadata[id] = metadata;
            if (_rows.TryGetValue(id, out var row))
            {
                ApplyRow(row);
            }
        }
        catch (Exception)
        {
            // A cancelled or failed thumbnail request is not shown to the user.
        }
        finally
        {
            if (_pending.TryGetValue(id, out var current) && ReferenceEquals(current, request))
            {
                _pending.Remove(id);
            }

            request.Dispose();
        }
    }

    private void StopSession()
    {
        _library.Changed -= OnLibraryChanged;
        _uiSettings.Changed -= OnSettingsChanged;

        _searchDebounce?.Cancel();
        _searchDebounce?.Dispose();
        _searchDebounce = null;

        foreach (var request in _pending.Values)
        {
            request.Cancel();
        }

        _pending.Clear();

        _session?.Cancel();
        _session?.Dispose();
        _session = null;
    }

    private void ApplySettings(UiSettings settings)
    {
        _settings = settings;
        SortField = settings.SortField;
        SortDirection = settings.SortDirection;
        RebuildRows();
    }

    private void OnLibraryChanged(object? sender, EventArgs e) =>
        MainThread.BeginInvokeOnMainThread(() =>
        {
            // A callback queued before Deactivate does nothing.
            if (_session is not null)
            {
                _ = LoadAsync();
            }
        });

    private void OnSettingsChanged(object? sender, UiSettings settings) =>
        MainThread.BeginInvokeOnMainThread(() =>
        {
            // A callback queued before Deactivate does nothing.
            if (_session is not null)
            {
                ApplySettings(settings);
            }
        });

    // Only the load owns IsLoading and IsRefreshing; a failed command reports through ErrorMessage alone.
    private void ReportError(Exception ex) => ErrorMessage = ex.Message;
}
