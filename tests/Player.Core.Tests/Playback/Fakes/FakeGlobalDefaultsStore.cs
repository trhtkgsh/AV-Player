using Player.Core.Contracts;
using Player.Core.Models;

namespace Player.Core.Tests.Playback.Fakes;

/// <summary>Holds one <see cref="GlobalDefaults"/>. A save stores the value and raises <see cref="Changed"/>.</summary>
internal sealed class FakeGlobalDefaultsStore : IGlobalDefaultsStore
{
    private readonly object _lock = new();
    private readonly List<GlobalDefaults> _saved = new();
    private int _getCount;

    public GlobalDefaults Value { get; set; } = new();
    public Exception? GetException { get; set; }
    public Exception? SaveException { get; set; }
    public int GetCount => Volatile.Read(ref _getCount);

    public int SaveCount
    {
        get
        {
            lock (_lock)
            {
                return _saved.Count;
            }
        }
    }

    public IReadOnlyList<GlobalDefaults> Saved
    {
        get
        {
            lock (_lock)
            {
                return _saved.ToArray();
            }
        }
    }

    public event EventHandler<GlobalDefaults>? Changed;

    public Task<GlobalDefaults> GetAsync(CancellationToken ct)
    {
        Interlocked.Increment(ref _getCount);
        return GetException is { } ex ? Task.FromException<GlobalDefaults>(ex) : Task.FromResult(Value);
    }

    public Task SaveAsync(GlobalDefaults value, CancellationToken ct)
    {
        if (SaveException is { } ex)
        {
            return Task.FromException(ex);
        }

        lock (_lock)
        {
            Value = value;
            _saved.Add(value);
        }

        Changed?.Invoke(this, value);
        return Task.CompletedTask;
    }

    /// <summary>Raises <see cref="Changed"/> as if another part of the app had saved.</summary>
    public void RaiseChanged(GlobalDefaults value) => Changed?.Invoke(this, value);
}
