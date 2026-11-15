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

    // The rows of the first screen of a grid that get their thumbnails at once: six rows of tiles.
    private const int GridVisibleRows = 6;
    private const int SearchDebounceMs = 150;

    // The file-name size at the font scale 1.0 (the Label style of the app).
    private const double BaseNameFontSize = 16;

    private readonly IMediaLibrary _library;
    private readonly IMediaMetadataService _metadataService;
    private readonly IPlaybackStateStore _stateStore;
    private readonly IUiSettingsStore _uiSettings;
    private readonly IPlaybackCoordinator _coordinator;
    private readonly IFileOperations _fileOperations;
    private readonly SelectionOperations _operations;
    private readonly TimeProvider _timeProvider;
    private readonly ILocalizer _localizer;

    private readonly Dictionary<MediaFileId, CancellationTokenSource> _pending = new();

    private CancellationTokenSource? _session;
    private CancellationTokenSource? _searchDebounce;
    private long? _bucketId;
    private string _folderTitle = string.Empty;
    private UiSettings _settings = new();
    private IReadOnlyList<MediaItem> _items = [];
    private Dictionary<MediaFileId, MediaMetadata> _metadata = new();
    private Dictionary<MediaFileId, PlaybackState> _states = new();
    private Dictionary<MediaFileId, MediaRowViewModel> _rows = new();

    // The buckets of the audio-only folders. Read for the All files list only.
    private HashSet<long> _audioOnlyBuckets = new();
    private int _loadVersion;
    private bool _settingsLoaded;
    private bool _choosingLayout;

    // The height of a grid tile's thumbnail, set on every row. Kept here for the rows made later.
    private double _thumbnailHeight;

    // The width of the list, as the page reported it. The columns follow it and the item size.
    private double _listWidth;

    [ObservableProperty]
    private double _listThumbnailWidth = ItemSizes.ListThumbnailWidth(ExplorerItemSize.Large);

    [ObservableProperty]
    private double _listThumbnailHeight = ItemSizes.ListThumbnailHeight(ExplorerItemSize.Large);

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

    // True when the files are shown as a grid of tiles, false as a list of rows. Follows the stored layout.
    [ObservableProperty]
    private bool _isGrid;

    // The number of grid columns for the width of the list.
    [ObservableProperty]
    private int _columns = GridTiles.MinColumns;
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
        IFileOperations fileOperations,
        SelectionOperations operations,
        TimeProvider timeProvider,
        ILocalizer localizer)
    {
        _library = library;
        _metadataService = metadataService;
        _stateStore = stateStore;
        _uiSettings = uiSettings;
        _coordinator = coordinator;
        _fileOperations = fileOperations;
        _operations = operations;
        _timeProvider = timeProvider;
        _localizer = localizer;
    }

    public ObservableCollection<MediaRowViewModel> Rows { get; } = new();

    private CancellationToken SessionToken => _session?.Token ?? new CancellationToken(canceled: true);

    /// <summary>Navigation parameters: "bucketId" (long, absent means all folders) and "title" (string, the folder name).</summary>
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        _bucketId = query.TryGetValue("bucketId", out var bucket) && bucket is long id ? (long?)id : null;
        _folderTitle = query.TryGetValue("title", out var title) && title is string text ? text : string.Empty;
        RebuildTitle();
    }

    public async Task ActivateAsync()
    {
        StopSession();
        _session = new CancellationTokenSource();
        _library.Changed += OnLibraryChanged;
        _uiSettings.Changed += OnSettingsChanged;
        _localizer.Changed += OnLanguageChanged;
        RebuildTitle();
        await LoadAsync();
    }

    public void Deactivate()
    {
        StopSession();
        _loadVersion++;
        _selection.Clear();
        UpdateSelection();
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
        if (_fileOperationRunning || Environment.TickCount64 < _tapGuardUntil)
        {
            return;
        }

        if (IsSelecting)
        {
            ToggleSelect(row);
            return;
        }

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

            var choices = new (string Text, SortField Field)[]
            {
                (_localizer["Explorer.SortName"], SortField.Name),
                (_localizer["Explorer.SortDateAdded"], SortField.DateAdded),
                (_localizer["Explorer.SortSize"], SortField.Size),
                (_localizer["Explorer.SortDuration"], SortField.Duration),
            };

            var choice = await shell.DisplayActionSheetAsync(
                _localizer["Explorer.SortBy"],
                _localizer["Common.Cancel"],
                null,
                choices.Select(c => c.Text).ToArray());

            // A cancelled or dismissed sheet gives no matching text, so no field is chosen.
            SortField? field = choices
                .Where(c => c.Text == choice)
                .Select(c => (SortField?)c.Field)
                .FirstOrDefault();

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

            // Read the latest stored settings: only the sort changes.
            var latest = await _uiSettings.GetAsync(SessionToken);
            _settings = latest with { SortField = field, SortDirection = direction };
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

    // The button shows the other layout: "Grid" while the list is shown, "List" while the grid is shown.
    public string LayoutText => _localizer[IsGrid ? "Explorer.LayoutList" : "Explorer.LayoutGrid"];

    [RelayCommand]
    private async Task ToggleLayout()
    {
        if (_choosingLayout)
        {
            return;
        }

        _choosingLayout = true;
        try
        {
            // Read the latest stored settings: only the layout changes.
            var latest = await _uiSettings.GetAsync(SessionToken);
            var saved = latest with { Layout = latest.Layout == ExplorerLayout.Grid ? ExplorerLayout.List : ExplorerLayout.Grid };
            await _uiSettings.SaveAsync(saved, SessionToken);
            ApplySettings(saved);
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
            _choosingLayout = false;
        }
    }

    // The list width gives the number of grid columns and the thumbnail height of a tile. Called when the list changes size.
    public void SetListWidth(double width)
    {
        if (width <= 0)
        {
            return;
        }

        _listWidth = width;
        var previous = Columns;
        UpdateColumns();

        // A new span shows other tiles: their thumbnails are requested now.
        if (Columns != previous)
        {
            VisibleRangeChanged(0, InitialLastIndex());
        }
    }

    // The columns and the tile thumbnail follow the list width and the item size. Rows are updated here or by RebuildRows.
    private void UpdateColumns()
    {
        if (_listWidth <= 0)
        {
            return;
        }

        Columns = GridTiles.Columns(_listWidth, _settings.ItemSize);
        _thumbnailHeight = GridTiles.TileWidth(_listWidth, Columns) * 9 / 16;
        foreach (var row in Rows)
        {
            row.ThumbnailHeight = _thumbnailHeight;
        }
    }

    partial void OnIsGridChanged(bool value) => OnPropertyChanged(nameof(LayoutText));

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

            var permitted = await _library.EnsureReadPermissionAsync(ct);
            if (!IsCurrentLoad(version))
            {
                return;
            }

            // The results are applied together, after the newest stored settings are read.
            IReadOnlyList<MediaItem> items = [];
            var states = new Dictionary<MediaFileId, PlaybackState>();
            var metadata = new Dictionary<MediaFileId, MediaMetadata>();
            var audioOnly = new HashSet<long>();
            PermissionDenied = !permitted;
            if (permitted)
            {
                items = await _library.GetItemsAsync(new MediaQuery(_bucketId, null), ct);
                if (!IsCurrentLoad(version))
                {
                    return;
                }

                var ids = items.Select(item => item.Id).ToList();
                states = new Dictionary<MediaFileId, PlaybackState>(await _stateStore.GetManyAsync(ids, ct));
                if (!IsCurrentLoad(version))
                {
                    return;
                }

                metadata = new Dictionary<MediaFileId, MediaMetadata>(await _metadataService.GetCachedAsync(ids, ct));
                if (!IsCurrentLoad(version))
                {
                    return;
                }

                // The All files list leaves out the files of audio-only folders while the setting is off.
                // The newest stored settings decide, not the ones read at the start of the load.
                if (_bucketId is null)
                {
                    var decision = await _uiSettings.GetAsync(ct);
                    if (!IsCurrentLoad(version))
                    {
                        return;
                    }

                    if (!decision.IncludeAudioOnlyFolders)
                    {
                        audioOnly = await ReadAudioOnlyBucketsAsync(settings.NewTagDays, ct);
                        if (!IsCurrentLoad(version))
                        {
                            return;
                        }
                    }
                }
            }

            // Apply the newest stored settings: a change made while this load ran must not be lost.
            var newest = await _uiSettings.GetAsync(ct);
            if (!IsCurrentLoad(version))
            {
                return;
            }

            _settings = newest;
            _settingsLoaded = true;
            SortField = newest.SortField;
            SortDirection = newest.SortDirection;
            _items = items;
            _audioOnlyBuckets = audioOnly;
            _states = states;
            _metadata = metadata;
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

    // The buckets of the audio-only folders. A failed query leaves every folder shown.
    private async Task<HashSet<long>> ReadAudioOnlyBucketsAsync(int newTagDays, CancellationToken ct)
    {
        try
        {
            var folders = await _library.GetFoldersAsync(newTagDays, ct);
            return folders.Where(f => f.AudioOnly).Select(f => f.BucketId).ToHashSet();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
            return new HashSet<long>();
        }
    }

    private void RebuildRows()
    {
        IsGrid = _settings.Layout == ExplorerLayout.Grid;
        ListThumbnailWidth = ItemSizes.ListThumbnailWidth(_settings.ItemSize);
        ListThumbnailHeight = ItemSizes.ListThumbnailHeight(_settings.ItemSize);
        UpdateColumns();

        var previous = _rows;
        _rows = new Dictionary<MediaFileId, MediaRowViewModel>(_items.Count);
        foreach (var item in _items)
        {
            _rows[item.Id] = previous.TryGetValue(item.Id, out var existing) && existing.Item == item
                ? existing
                : new MediaRowViewModel(item);
        }

        var shown = _settings.IncludeAudioOnlyFolders || _bucketId is not null
            ? _items
            : _items.Where(item => !_audioOnlyBuckets.Contains(item.BucketId)).ToList();

        var ordered = SearchFilter.Apply(
                MediaSorter.Sort(shown, SortField, SortDirection, _metadata),
                SearchText)
            .Select(item => _rows[item.Id])
            .ToList();

        // The rows that stay are updated in place. Only a change of their set or order replaces the Rows.
        foreach (var row in ordered)
        {
            ApplyRow(row);
        }

        if (!SameOrder(ordered))
        {
            Rows.Clear();
            foreach (var row in ordered)
            {
                Rows.Add(row);
            }
        }

        foreach (var id in _selection.Keys.Where(id => !_rows.ContainsKey(id)).ToList())
        {
            _selection.Remove(id);
        }

        UpdateIsEmpty();
        UpdateSelection();
        VisibleRangeChanged(0, InitialLastIndex());
    }

    private bool SameOrder(IReadOnlyList<MediaRowViewModel> rows)
    {
        if (Rows.Count != rows.Count)
        {
            return false;
        }

        for (var i = 0; i < rows.Count; i++)
        {
            if (!ReferenceEquals(Rows[i], rows[i]))
            {
                return false;
            }
        }

        return true;
    }

    private int InitialLastIndex() =>
        IsGrid ? Math.Max(InitialVisibleLast, Columns * GridVisibleRows - 1) : InitialVisibleLast;

    private void ApplyRow(MediaRowViewModel row)
    {
        _metadata.TryGetValue(row.Item.Id, out var metadata);
        _states.TryGetValue(row.Item.Id, out var state);
        row.Apply(metadata, state, _settings, _timeProvider.GetUtcNow().ToUnixTimeSeconds(), TimeZoneInfo.Local);
        row.NameFontSize = BaseNameFontSize * _settings.ExplorerFontScale;
        row.ThumbnailHeight = _thumbnailHeight;
    }

    private async Task RequestMetadataAsync(MediaItem item, CancellationTokenSource request)
    {
        var id = item.Id;
        try
        {
            var metadata = await _metadataService.GetOrCreateAsync(item, request.Token);
            // A file renamed or deleted meanwhile is no longer in the list: its result is dropped.
            if (metadata is null || request.Token.IsCancellationRequested || _session is null
                || !_items.Any(i => i.Id == id))
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
        _localizer.Changed -= OnLanguageChanged;
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
            if (_session is null)
            {
                return;
            }

            // The echo of a change this page made and applied is skipped: no second rebuild.
            if (settings == _settings)
            {
                return;
            }

            // Turning the audio-only filter off needs the audio-only folders: the load reads them.
            if (_bucketId is null && _settings.IncludeAudioOnlyFolders && !settings.IncludeAudioOnlyFolders)
            {
                _settings = settings;
                _ = LoadAsync();
                return;
            }

            ApplySettings(settings);
        });

    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        OnPropertyChanged(nameof(LayoutText));
        RebuildTitle();
    }

    // All media has its own title; a folder shows its name, which is not translated.
    private void RebuildTitle() =>
        Title = IsSelecting
            ? _localizer.Format("Explorer.SelectedCount", _selection.Count)
            : _bucketId is null ? _localizer["Explorer.AllMedia"] : _folderTitle;

    // Only the load owns IsLoading and IsRefreshing; a failed command reports through ErrorMessage alone.
    private void ReportError(Exception ex) => ErrorMessage = ex.Message;
}
