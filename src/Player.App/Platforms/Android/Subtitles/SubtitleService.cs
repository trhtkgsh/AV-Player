using System.Globalization;
using Android.Content;
using Android.Database;
using Android.Provider;
using Player.Core.Contracts;
using Player.Core.Logic.Subtitles;
using Player.Core.Models;
using AndroidUri = Android.Net.Uri;

namespace Player.App.Subtitles;

/// <summary>
/// Subtitle files and the automatic audio/subtitle policy. Every file the engine gets is a UTF-8 copy
/// in the app's own folder, so a saved choice can be opened again later without any URI permission.
/// </summary>
public sealed class SubtitleService : ISubtitleService, IDisposable
{
    private const string Tag = "ArielSubs";
    private static readonly TimeSpan TrackWait = TimeSpan.FromMilliseconds(1500);

    private readonly IPlayerEngine _engine;
    private readonly IPlaybackCoordinator _coordinator;
    private readonly IPlaybackStateStore _stateStore;
    private readonly IPlaybackStateRecorder _recorder;
    private readonly IGlobalDefaultsStore _defaults;
    private readonly CancellationTokenSource _lifetime = new();
    private readonly object _gate = new();
    private Task _chain = Task.CompletedTask;
    private MediaFileId? _lastId;
    private PlayerStatus _lastStatus = PlayerStatus.Idle;
    private MediaFileId? _pending;
    private volatile bool _disposed;

    public SubtitleService(
        IPlayerEngine engine,
        IPlaybackCoordinator coordinator,
        IPlaybackStateStore stateStore,
        IPlaybackStateRecorder recorder,
        IGlobalDefaultsStore defaults)
    {
        _engine = engine ?? throw new ArgumentNullException(nameof(engine));
        _coordinator = coordinator ?? throw new ArgumentNullException(nameof(coordinator));
        _stateStore = stateStore ?? throw new ArgumentNullException(nameof(stateStore));
        _recorder = recorder ?? throw new ArgumentNullException(nameof(recorder));
        _defaults = defaults ?? throw new ArgumentNullException(nameof(defaults));

        _engine.SnapshotChanged += OnSnapshotChanged;
        _ = Task.Run(SubtitleFileStore.DeleteOld);
    }

    public void Dispose()
    {
        lock (_gate)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
        }

        _engine.SnapshotChanged -= OnSnapshotChanged;
        _lifetime.Cancel();
        _lifetime.Dispose();
    }

    public async Task<IReadOnlyList<SubtitleFile>> FindSidecarsAsync(MediaItem item, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(item);
        try
        {
            var defaults = await _defaults.GetAsync(ct).ConfigureAwait(false);
            return await Task.Run(() => QuerySidecars(item, defaults.PreferredSubtitleLanguage, ct), ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            Warn("Sidecar search failed", ex);
            return [];
        }
    }

    public Task<bool> LoadExternalAsync(string contentUri, CancellationToken ct) =>
        _engine.Snapshot.Current is { } id ? LoadAsync(id, contentUri, true, ct) : Task.FromResult(false);

    public async Task<bool> PickAndLoadAsync(CancellationToken ct)
    {
        try
        {
            // The file the user is looking at: a skip while the picker is open must not move the subtitle to another file.
            if (_engine.Snapshot.Current is not { } id)
            {
                return false;
            }

            var picked = await MainThread.InvokeOnMainThreadAsync(() => FilePicker.Default.PickAsync(PickOptions.Default))
                .WaitAsync(ct).ConfigureAwait(false);
            if (picked is null || SubtitleFileStore.ExtensionOf(picked.FileName) is null)
            {
                return false;
            }

            return await Task.Run(async () =>
            {
                await using var stream = await picked.OpenReadAsync().ConfigureAwait(false);
                return await ImportAsync(id, stream, picked.FileName, null, true, ct).ConfigureAwait(false);
            }, ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            Warn("Picking a subtitle file failed", ex);
            return false;
        }
    }

    public async Task ApplyPreferredTracksAsync(GlobalDefaults defaults, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(defaults);
        var id = _engine.Snapshot.Current;
        if (id is null)
        {
            return;
        }

        var state = await _stateStore.GetAsync(id.Value, ct).ConfigureAwait(false);
        await ApplyPreferredAsync(id.Value, state, defaults, false, ct).ConfigureAwait(false);
    }

    // Everything that touches the content resolver or the disk runs on a thread-pool thread.
    private async Task<bool> LoadAsync(MediaFileId id, string contentUri, bool record, CancellationToken ct)
    {
        try
        {
            return await Task.Run(() => LoadCoreAsync(id, contentUri, record, ct), ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            Warn("Loading a subtitle file failed", ex);
            return false;
        }
    }

    private async Task<bool> LoadCoreAsync(MediaFileId id, string contentUri, bool record, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(contentUri))
        {
            return false;
        }

        var local = SubtitleFileStore.FromFileUri(contentUri);
        if (local is not null)
        {
            return await AddAsync(id, local, SubtitleFileStore.ReadName(local), record, ct).ConfigureAwait(false);
        }

        var uri = AndroidUri.Parse(contentUri)!;
        if (string.Equals(uri.Scheme, "file", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var meta = ReadMeta(uri);
        var name = meta.Name ?? uri.LastPathSegment;
        if (SubtitleFileStore.ExtensionOf(name) is null)
        {
            return false;
        }

        var key = string.Create(CultureInfo.InvariantCulture, $"{contentUri}|{meta.Size}|{meta.Modified}");
        await using var stream = Resolver.OpenInputStream(uri);
        return stream is not null && await ImportAsync(id, stream, name!, key, record, ct).ConfigureAwait(false);
    }

    /// <summary>The shared copy step: decode, write the UTF-8 copy, hand it to the engine, optionally record it.</summary>
    private async Task<bool> ImportAsync(
        MediaFileId id, Stream stream, string displayName, string? sourceKey, bool record, CancellationToken ct)
    {
        var extension = SubtitleFileStore.ExtensionOf(displayName);
        if (extension is null)
        {
            return false;
        }

        var bytes = await SubtitleFileStore.ReadLimitedAsync(stream, ct).ConfigureAwait(false);
        if (bytes is null)
        {
            return false;
        }

        var defaults = await _defaults.GetAsync(ct).ConfigureAwait(false);
        var hint = LanguageSuffix(displayName) ?? defaults.PreferredSubtitleLanguage
            ?? CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        var text = SubtitleTextEncoding.Decode(bytes, hint);
        var local = SubtitleFileStore.Write(text, sourceKey, extension, displayName);
        return await AddAsync(id, local, displayName, record, ct).ConfigureAwait(false);
    }

    // The language part of "name.he.srt" (also for names with more dots); null when there is none.
    private static string? LanguageSuffix(string displayName)
    {
        var stem = Path.GetFileNameWithoutExtension(displayName);
        var dot = stem.LastIndexOf('.');
        return dot <= 0
            ? null
            : SidecarMatcher.Match(stem.Substring(0, dot) + ".mp4", [displayName], null).FirstOrDefault()?.Language;
    }

    /// <summary>
    /// Gives the copy to the engine unless the engine already holds it, and records it when asked. The file id is the one
    /// that was current when the load started: if the user moved on, nothing is added and nothing is recorded.
    /// </summary>
    private async Task<bool> AddAsync(MediaFileId id, string local, string displayName, bool record, CancellationToken ct)
    {
        if (!IsCurrent(id))
        {
            return false;
        }

        if (!SamePath(_engine.ExternalSubtitlePath, local))
        {
            try
            {
                await _engine.AddSubtitleFileAsync(local, displayName, ct).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                Warn("The engine rejected the subtitle file", ex);
                return false;
            }
        }

        if (record)
        {
            var uri = SubtitleFileStore.ToFileUri(local);
            _recorder.Update(s => s.Id == id ? s with { ExternalSubtitleUri = uri, SubtitleTrackId = null } : s);
        }

        return true;
    }

    private static bool SamePath(string? a, string? b)
    {
        try
        {
            return a is not null && b is not null
                && string.Equals(Path.GetFullPath(a), Path.GetFullPath(b), StringComparison.Ordinal);
        }
        catch (Exception)
        {
            return false;
        }
    }

    private IReadOnlyList<SubtitleFile> QuerySidecars(MediaItem item, string? preferredLanguage, CancellationToken ct)
    {
        var collection = MediaStore.Files.GetContentUri("external");
        if (collection is null)
        {
            return [];
        }

        var ids = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
        var args = new[] { item.BucketId.ToString(CultureInfo.InvariantCulture) };
        using (var cursor = Resolver.Query(collection, ["_id", "_display_name"], "bucket_id = ?", args, null))
        {
            if (cursor is null)
            {
                return [];
            }

            var idColumn = cursor.GetColumnIndexOrThrow("_id");
            var nameColumn = cursor.GetColumnIndexOrThrow("_display_name");
            while (cursor.MoveToNext())
            {
                ct.ThrowIfCancellationRequested();
                if (!cursor.IsNull(nameColumn) && cursor.GetString(nameColumn) is { } name)
                {
                    ids.TryAdd(name, cursor.GetLong(idColumn));
                }
            }
        }

        var result = new List<SubtitleFile>();
        foreach (var match in SidecarMatcher.Match(item.DisplayName, ids.Keys, preferredLanguage))
        {
            var uri = ContentUris.WithAppendedId(collection, ids[match.FileName])!.ToString()!;
            result.Add(new SubtitleFile(uri, match.FileName, match.Language));
        }

        return result;
    }

    private (string? Name, long Size, long Modified) ReadMeta(AndroidUri uri)
    {
        try
        {
            using var cursor = Resolver.Query(uri, null, null, null, null);
            if (cursor is null || !cursor.MoveToFirst())
            {
                return default;
            }

            var name = TextOf(cursor, IOpenableColumns.DisplayName);
            return (name, NumberOf(cursor, IOpenableColumns.Size), NumberOf(cursor, MediaStore.IMediaColumns.DateModified));
        }
        catch (Exception)
        {
            return default;
        }
    }

    private static string? TextOf(ICursor cursor, string column)
    {
        var index = cursor.GetColumnIndex(column);
        return index >= 0 && !cursor.IsNull(index) ? cursor.GetString(index) : null;
    }

    private static long NumberOf(ICursor cursor, string column)
    {
        var index = cursor.GetColumnIndex(column);
        return index >= 0 && !cursor.IsNull(index) ? cursor.GetLong(index) : 0;
    }

    private static ContentResolver Resolver => global::Android.App.Application.Context.ContentResolver!;

    private static void Warn(string message, Exception ex) =>
        global::Android.Util.Log.Warn(Tag, $"{message}: {ex.GetType().Name}: {ex.Message}");

    private static void Warn(string message) => global::Android.Util.Log.Warn(Tag, message);

    // ---- Automatic policy ----

    // Engine thread: only bookkeeping here, the work runs in a serialized chain.
    private void OnSnapshotChanged(object? sender, PlayerSnapshot snapshot)
    {
        lock (_gate)
        {
            if (_disposed)
            {
                return;
            }

            var previous = _lastStatus;
            var previousId = _lastId;
            _lastStatus = snapshot.Status;
            _lastId = snapshot.Current;

            if (snapshot.Current is not { } id)
            {
                _pending = null;
                return;
            }

            var restart = snapshot.Status == PlayerStatus.Opening
                && previous is PlayerStatus.Idle or PlayerStatus.Ended or PlayerStatus.Error;
            if (previousId != id || restart)
            {
                _pending = id;
            }

            if (_pending == id && snapshot.Status is PlayerStatus.Playing or PlayerStatus.Paused)
            {
                _pending = null;
                _chain = _chain.ContinueWith(_ => RunAsync(id), CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.Default).Unwrap();
            }
        }
    }

    private bool IsCurrent(MediaFileId id) => _engine.Snapshot.Current == id && !_lifetime.IsCancellationRequested;

    private async Task RunAsync(MediaFileId id)
    {
        try
        {
            var ct = _lifetime.Token;
            if (!IsCurrent(id))
            {
                return;
            }

            var state = await _stateStore.GetAsync(id, ct).ConfigureAwait(false);
            var defaults = await _defaults.GetAsync(ct).ConfigureAwait(false);
            var subtitleDecided = false;

            // Restore the saved copy. The engine keeps its own file across restarts and replays, so a copy it already
            // holds is not added again. A failure (copy deleted, engine refused it) leaves the subtitle undecided.
            var saved = SubtitleFileStore.FromFileUri(state?.ExternalSubtitleUri);
            if (saved is not null)
            {
                SubtitleFileStore.Touch(saved);
                subtitleDecided = await AddAsync(id, saved, SubtitleFileStore.ReadName(saved), false, ct).ConfigureAwait(false);
                if (subtitleDecided && state!.SubtitleTrackId is { } track)
                {
                    await WaitForAsync(() => track == -1 || HasSubtitle(track), id, ct).ConfigureAwait(false);
                    if (IsCurrent(id) && (track == -1 || HasSubtitle(track)))
                    {
                        await _engine.SelectSubtitleTrackAsync(track, ct).ConfigureAwait(false);
                    }
                }
            }

            if (!IsCurrent(id))
            {
                return;
            }

            // An external file that the engine already holds decides the subtitle kind: no sidecar on top of it.
            subtitleDecided |= _engine.ExternalSubtitlePath is not null;

            if (!subtitleDecided && state?.SubtitleTrackId is null && defaults.SubtitlesEnabled)
            {
                subtitleDecided = await LoadSidecarAsync(id, ct).ConfigureAwait(false);
            }

            // A saved copy that could not be restored does not fix the subtitle kind.
            var effective = subtitleDecided || state is null ? state : state with { ExternalSubtitleUri = null };
            await ApplyPreferredAsync(id, effective, defaults, subtitleDecided, ct).ConfigureAwait(false);
        }
        catch (Exception) when (_disposed)
        {
            // The service was disposed while it worked (a cancelled wait is no failure).
        }
        catch (Exception ex)
        {
            Warn("Automatic subtitle policy failed", ex);
        }
    }

    private async Task<bool> LoadSidecarAsync(MediaFileId id, CancellationToken ct)
    {
        // The coordinator sets its current item only after the engine accepted the open: wait for the item of this open.
        await WaitForAsync(() => _coordinator.Current?.Id == id, id, ct).ConfigureAwait(false);
        if (_coordinator.Current is not { } item || item.Id != id)
        {
            Warn("Sidecar search skipped: the coordinator has not reached the item of this open");
            return false;
        }

        var sidecars = await FindSidecarsAsync(item, ct).ConfigureAwait(false);
        return sidecars.Count > 0 && IsCurrent(id)
            && await LoadAsync(id, sidecars[0].ContentUri, false, ct).ConfigureAwait(false);
    }

    private async Task ApplyPreferredAsync(
        MediaFileId id, PlaybackState? state, GlobalDefaults defaults, bool subtitleDecided, CancellationToken ct)
    {
        var audio = state?.AudioTrackId is null && !string.IsNullOrWhiteSpace(defaults.PreferredAudioLanguage);
        var subtitle = !subtitleDecided && state?.SubtitleTrackId is null && state?.ExternalSubtitleUri is null;

        if (audio)
        {
            await WaitForAsync(() => _engine.AudioTracks.Count > 0, id, ct).ConfigureAwait(false);
            if (IsCurrent(id)
                && PreferredTrackSelector.SelectAudio(_engine.AudioTracks, defaults.PreferredAudioLanguage) is { } audioId
                && audioId != _engine.CurrentAudioTrackId)
            {
                await _engine.SelectAudioTrackAsync(audioId, ct).ConfigureAwait(false);
            }
        }

        if (subtitle)
        {
            // The engine reports no subtitle track (not even -1) until it knows the subtitle tracks.
            if (defaults.SubtitlesEnabled)
            {
                await WaitForAsync(() => _engine.CurrentSubtitleTrackId is not null, id, ct).ConfigureAwait(false);
            }

            if (IsCurrent(id)
                && PreferredTrackSelector.SelectSubtitle(_engine.SubtitleTracks, defaults.PreferredSubtitleLanguage, defaults.SubtitlesEnabled) is { } subtitleId
                && subtitleId != _engine.CurrentSubtitleTrackId)
            {
                await _engine.SelectSubtitleTrackAsync(subtitleId, ct).ConfigureAwait(false);
            }
        }
    }

    private bool HasSubtitle(int trackId) => _engine.SubtitleTracks.Any(t => t.Id == trackId);

    // The engine fills its track lists a moment after the status changes. Bounded; cancelled by Dispose.
    private async Task WaitForAsync(Func<bool> condition, MediaFileId id, CancellationToken ct)
    {
        var until = DateTime.UtcNow + TrackWait;
        while (!condition() && IsCurrent(id) && DateTime.UtcNow < until)
        {
            await Task.Delay(100, ct).ConfigureAwait(false);
        }
    }
}
