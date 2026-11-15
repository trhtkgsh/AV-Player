using Player.Core.Contracts;
using Player.Core.Models;

namespace Player.Core.Tests.Playback.Fakes;

/// <summary>Holds folder rows by bucket id, records the bucket ids asked for and can throw.</summary>
internal sealed class FakeFolderSettingsStore : IFolderSettingsStore
{
    private readonly object _lock = new();
    private readonly List<long> _asked = new();

    /// <summary>The rows the store returns.</summary>
    public Dictionary<long, FolderSettings> Rows { get; } = new();

    /// <summary>When set, <see cref="GetAsync"/> throws it after recording the call.</summary>
    public Exception? GetException { get; set; }

    /// <summary>Awaited by <see cref="GetAsync"/> after the call was recorded.</summary>
    public Func<CancellationToken, Task>? GetHook { get; set; }

    public IReadOnlyList<long> Asked
    {
        get
        {
            lock (_lock)
            {
                return _asked.ToArray();
            }
        }
    }

    public event EventHandler? Changed
    {
        add { }
        remove { }
    }

    public async Task<FolderSettings?> GetAsync(long bucketId, CancellationToken ct)
    {
        lock (_lock)
        {
            _asked.Add(bucketId);
        }

        if (GetHook is not null)
        {
            await GetHook(ct).ConfigureAwait(false);
        }

        if (GetException is not null)
        {
            throw GetException;
        }

        return Rows.TryGetValue(bucketId, out var row) ? row : null;
    }

    public Task<IReadOnlyDictionary<long, FolderSettings>> GetAllAsync(CancellationToken ct) =>
        Task.FromResult<IReadOnlyDictionary<long, FolderSettings>>(new Dictionary<long, FolderSettings>(Rows));

    public Task SaveAsync(FolderSettings value, CancellationToken ct)
    {
        Rows[value.BucketId] = value;
        return Task.CompletedTask;
    }
}
