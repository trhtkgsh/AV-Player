using Player.Core.Contracts;
using Player.Core.Models;

namespace Player.Core.Tests.StateRecording;

/// <summary>A call held open by <see cref="RecordingStateStore"/> until the test calls <see cref="Release"/>.</summary>
internal sealed class StoreGate
{
    private readonly TaskCompletionSource _entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>Completes when the store call reached the gate.</summary>
    public Task Entered => _entered.Task.WaitAsync(RecordingStateStore.HangGuard);

    public void Release() => _release.TrySetResult();

    internal Task PassAsync()
    {
        _entered.TrySetResult();
        return _release.Task;
    }
}

/// <summary>
/// In-memory <see cref="IPlaybackStateStore"/>. <see cref="Saves"/> holds every save that succeeded, in order.
/// A failed save is counted in <see cref="SaveCalls"/> but not recorded. The waits use a long real-time guard
/// that only fires when a test would hang: no test outcome depends on it.
/// </summary>
internal sealed class RecordingStateStore : IPlaybackStateStore
{
    internal static readonly TimeSpan HangGuard = TimeSpan.FromSeconds(30);

    private readonly object _lock = new();
    private readonly Dictionary<MediaFileId, PlaybackState> _states = new();
    private readonly List<PlaybackState> _saves = new();
    private readonly Counter _getCalls = new();
    private readonly Counter _saveCalls = new();
    private readonly Counter _saveCount = new();
    private int _getsToFail;
    private int _savesToFail;
    private StoreGate? _getGate;
    private StoreGate? _saveGate;

    public int GetCalls => _getCalls.Value;

    /// <summary>Save attempts, counted when the call starts, including failed ones.</summary>
    public int SaveCalls => _saveCalls.Value;

    public IReadOnlyList<PlaybackState> Saves
    {
        get
        {
            lock (_lock)
            {
                return _saves.ToArray();
            }
        }
    }

    public void Preload(PlaybackState state)
    {
        lock (_lock)
        {
            _states[state.Id] = state;
        }
    }

    /// <summary>The state currently stored for the file, or null.</summary>
    public PlaybackState? Stored(MediaFileId id)
    {
        lock (_lock)
        {
            return _states.TryGetValue(id, out var state) ? state : null;
        }
    }

    public IReadOnlyList<PlaybackState> SavesFor(MediaFileId id) => Saves.Where(s => s.Id == id).ToArray();

    public void FailNextGets(int count)
    {
        lock (_lock)
        {
            _getsToFail = count;
        }
    }

    public void FailNextSaves(int count)
    {
        lock (_lock)
        {
            _savesToFail = count;
        }
    }

    /// <summary>The next <see cref="GetAsync"/> call waits until the returned gate is released.</summary>
    public StoreGate HoldNextGet()
    {
        var gate = new StoreGate();
        lock (_lock)
        {
            _getGate = gate;
        }

        return gate;
    }

    /// <summary>The next <see cref="SaveAsync"/> call waits until the returned gate is released.</summary>
    public StoreGate HoldNextSave()
    {
        var gate = new StoreGate();
        lock (_lock)
        {
            _saveGate = gate;
        }

        return gate;
    }

    /// <summary>Completes when at least <paramref name="count"/> saves succeeded.</summary>
    public Task WaitForSavesAsync(int count) => _saveCount.WaitForAsync(count).WaitAsync(HangGuard);

    /// <summary>Completes when at least <paramref name="count"/> save calls started.</summary>
    public Task WaitForSaveCallsAsync(int count) => _saveCalls.WaitForAsync(count).WaitAsync(HangGuard);

    public async Task<PlaybackState?> GetAsync(MediaFileId id, CancellationToken ct)
    {
        _getCalls.Increment();
        StoreGate? gate;
        lock (_lock)
        {
            gate = _getGate;
            _getGate = null;
        }

        if (gate is not null)
        {
            await gate.PassAsync().ConfigureAwait(false);
        }

        lock (_lock)
        {
            if (_getsToFail > 0)
            {
                _getsToFail--;
                throw new InvalidOperationException("Get failed.");
            }

            return _states.TryGetValue(id, out var state) ? state : null;
        }
    }

    public Task<IReadOnlyDictionary<MediaFileId, PlaybackState>> GetManyAsync(
        IReadOnlyCollection<MediaFileId> ids, CancellationToken ct)
    {
        lock (_lock)
        {
            IReadOnlyDictionary<MediaFileId, PlaybackState> result = ids
                .Where(_states.ContainsKey)
                .ToDictionary(i => i, i => _states[i]);
            return Task.FromResult(result);
        }
    }

    public async Task SaveAsync(PlaybackState state, CancellationToken ct)
    {
        _saveCalls.Increment();
        StoreGate? gate;
        lock (_lock)
        {
            gate = _saveGate;
            _saveGate = null;
        }

        if (gate is not null)
        {
            await gate.PassAsync().ConfigureAwait(false);
        }

        lock (_lock)
        {
            if (_savesToFail > 0)
            {
                _savesToFail--;
                throw new InvalidOperationException("Save failed.");
            }

            _states[state.Id] = state;
            _saves.Add(state);
        }

        _saveCount.Increment();
    }

    public Task DeleteAsync(MediaFileId id, CancellationToken ct)
    {
        lock (_lock)
        {
            _states.Remove(id);
        }

        return Task.CompletedTask;
    }

    private sealed class Counter
    {
        private readonly object _gate = new();
        private readonly List<(int Target, TaskCompletionSource Source)> _waiters = new();
        private int _value;

        public int Value
        {
            get
            {
                lock (_gate)
                {
                    return _value;
                }
            }
        }

        public void Increment()
        {
            TaskCompletionSource[] ready;
            lock (_gate)
            {
                _value++;
                ready = _waiters.Where(w => w.Target <= _value).Select(w => w.Source).ToArray();
                _waiters.RemoveAll(w => w.Target <= _value);
            }

            foreach (var source in ready)
            {
                source.TrySetResult();
            }
        }

        public Task WaitForAsync(int target)
        {
            lock (_gate)
            {
                if (_value >= target)
                {
                    return Task.CompletedTask;
                }

                var source = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                _waiters.Add((target, source));
                return source.Task;
            }
        }
    }
}
