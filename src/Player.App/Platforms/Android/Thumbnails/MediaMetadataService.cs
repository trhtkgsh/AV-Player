using Android.Util;
using Player.Core.Contracts;
using Player.Core.Models;
using Player.Data.Metadata;

namespace Player.App.Platforms.Android.Thumbnails;

public sealed class MediaMetadataService : IMediaMetadataService
{
    private const string Tag = "ArielThumbs";
    private const int MaxConcurrentExtractions = 2;

    private sealed class InFlight(CancellationTokenSource cts)
    {
        public CancellationTokenSource Cts { get; } = cts;
        public Task<MediaMetadata?> Task { get; set; } = null!;
        public int Waiters;
    }

    private readonly MediaMetadataRepository _repository;
    private readonly AndroidMetadataExtractor _extractor;
    private readonly SemaphoreSlim _slots = new(MaxConcurrentExtractions);
    private readonly object _gate = new();
    private readonly Dictionary<string, InFlight> _inFlight = new();
    private readonly HashSet<string> _retriedFailed = new();
    private readonly string _thumbsDir = Path.Combine(FileSystem.CacheDirectory, "thumbs");
    private int _extractionCount;

    public MediaMetadataService(MediaMetadataRepository repository, AndroidMetadataExtractor extractor)
    {
        _repository = repository;
        _extractor = extractor;
    }

    public async Task<IReadOnlyDictionary<MediaFileId, MediaMetadata>> GetCachedAsync(
        IReadOnlyCollection<MediaFileId> ids, CancellationToken ct)
    {
        var byKey = new Dictionary<string, MediaFileId>();
        foreach (var id in ids)
            byKey[id.ToKey()] = id;

        var rows = await _repository.GetManyAsync(byKey.Keys, ct).ConfigureAwait(false);
        return await Task.Run(() =>
        {
            var result = new Dictionary<MediaFileId, MediaMetadata>();
            foreach (var row in rows.Where(r => !r.Failed))
                result[byKey[row.FileKey]] = ToModel(byKey[row.FileKey], row);
            return (IReadOnlyDictionary<MediaFileId, MediaMetadata>)result;
        }, ct).ConfigureAwait(false);
    }

    public async Task<MediaMetadata?> GetOrCreateAsync(MediaItem item, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var key = item.Id.ToKey();
        InFlight entry;
        lock (_gate)
        {
            if (!_inFlight.TryGetValue(key, out entry!) || entry.Cts.IsCancellationRequested)
            {
                var created = entry = new InFlight(new CancellationTokenSource());
                _inFlight[key] = created;
                created.Task = Task.Run(() => RunAsync(item, key, created, created.Cts.Token));
            }
            entry.Waiters++;
        }

        try
        {
            return await entry.Task.WaitAsync(ct).ConfigureAwait(false);
        }
        finally
        {
            // The last caller to leave cancels queued or running work that nobody waits for.
            lock (_gate)
            {
                if (--entry.Waiters == 0 && !entry.Task.IsCompleted)
                    entry.Cts.Cancel();
            }
        }
    }

    public async Task PruneAsync(IReadOnlyCollection<MediaFileId> existing, CancellationToken ct)
    {
        var keys = existing.Select(id => id.ToKey()).ToHashSet();
        await _repository.DeleteNotInAsync(keys, ct).ConfigureAwait(false);

        var keepFiles = existing.Select(AndroidMetadataExtractor.ThumbnailFileName).ToHashSet();
        await Task.Run(() =>
        {
            if (!Directory.Exists(_thumbsDir))
                return;
            foreach (var file in Directory.EnumerateFiles(_thumbsDir, "*.jpg"))
            {
                ct.ThrowIfCancellationRequested();
                if (!keepFiles.Contains(Path.GetFileName(file)))
                    TryDelete(file);
            }
        }, ct).ConfigureAwait(false);
    }

    private async Task<MediaMetadata?> RunAsync(MediaItem item, string key, InFlight self, CancellationToken ct)
    {
        try
        {
            var cached = (await _repository.GetManyAsync([key], ct).ConfigureAwait(false)).FirstOrDefault();
            if (cached is not null)
            {
                if (!cached.Failed)
                {
                    if (cached.ThumbnailPath is null || File.Exists(cached.ThumbnailPath))
                        return ToModel(item.Id, cached);
                }
                else if (!RetryAllowed(key))
                {
                    return null;
                }
            }

            await _slots.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                return await ExtractAndStoreAsync(item, key, ct).ConfigureAwait(false);
            }
            finally
            {
                _slots.Release();
            }
        }
        finally
        {
            lock (_gate)
            {
                if (_inFlight.TryGetValue(key, out var current) && ReferenceEquals(current, self))
                    _inFlight.Remove(key);
            }
        }
    }

    private async Task<MediaMetadata?> ExtractAndStoreAsync(MediaItem item, string key, CancellationToken ct)
    {
        var row = new MediaMetadataRow
        {
            FileKey = key,
            MediaStoreId = item.Id.MediaStoreId,
            CreatedUtc = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
        };

        MediaMetadata? result = null;
        try
        {
            Directory.CreateDirectory(_thumbsDir);
            Log.Debug(Tag, $"decode #{Interlocked.Increment(ref _extractionCount)} {key}");
            var extracted = await _extractor.ExtractAsync(item, _thumbsDir, ct).ConfigureAwait(false);
            row.DurationMs = extracted.DurationMs;
            row.Width = extracted.Width;
            row.Height = extracted.Height;
            row.ThumbnailPath = extracted.ThumbnailPath;
            result = ToModel(item.Id, row);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Log.Warn(Tag, $"extraction failed for {key}: {ex.GetType().Name}: {ex.Message}");
            row.Failed = true;
        }

        await _repository.UpsertAsync(row, CancellationToken.None).ConfigureAwait(false);
        if (row.Failed)
            lock (_gate)
                _retriedFailed.Add(key);
        return result;
    }

    // A failed row is retried once per app start, not on every scroll.
    private bool RetryAllowed(string key)
    {
        lock (_gate)
            return !_retriedFailed.Contains(key);
    }

    private static MediaMetadata ToModel(MediaFileId id, MediaMetadataRow row) =>
        new(id, row.DurationMs, row.Width, row.Height,
            row.ThumbnailPath is not null && File.Exists(row.ThumbnailPath) ? row.ThumbnailPath : null);

    private static void TryDelete(string path)
    {
        try { File.Delete(path); }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }
}
