using Player.Core.Contracts;
using Player.Core.Logic.Resolution;
using Player.Core.Models;

namespace Player.Core.Logic.Playback;

/// <summary>
/// Runs a queue on top of the engine. Public commands and engine events share one gate, so they never interleave.
/// <see cref="Changed"/> is raised after the gate is released.
/// </summary>
public sealed class PlaybackCoordinator : IPlaybackCoordinator, IDisposable
{
    private const long RestartThresholdMs = 3000;
    private const long MinAbRangeMs = 1000;
    private static readonly TimeSpan AbJumpRearm = TimeSpan.FromSeconds(1);

    private readonly IPlayerEngine _engine;
    private readonly IPlaybackStateStore _stateStore;
    private readonly IPlaybackStateRecorder _recorder;
    private readonly IGlobalDefaultsStore _defaults;
    private readonly TimeProvider _time;

    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly CancellationTokenSource _lifetime = new();
    private readonly object _eventLock = new();
    private readonly object _openLock = new();

    private MediaItem? _current;
    private IReadOnlyList<MediaItem> _queue = Array.Empty<MediaItem>();
    private int _index = -1;
    private volatile RepeatMode _repeat;
    private AbRepeat? _abRepeat;
    private bool _repeatLoaded;
    private bool _dirty;
    private bool _disposed;

    private bool _endedSeen;
    private bool _abJumpPending;
    private long _abJumpStamp;

    private long _openSeq;
    private CancellationTokenSource? _runningOpen;

    public PlaybackCoordinator(
        IPlayerEngine engine,
        IPlaybackStateStore stateStore,
        IPlaybackStateRecorder recorder,
        IGlobalDefaultsStore defaults,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(engine);
        ArgumentNullException.ThrowIfNull(stateStore);
        ArgumentNullException.ThrowIfNull(recorder);
        ArgumentNullException.ThrowIfNull(defaults);
        ArgumentNullException.ThrowIfNull(timeProvider);

        _engine = engine;
        _stateStore = stateStore;
        _recorder = recorder;
        _defaults = defaults;
        _time = timeProvider;
        _engine.SnapshotChanged += OnSnapshotChanged;
    }

    public MediaItem? Current => Volatile.Read(ref _current);
    public IReadOnlyList<MediaItem> Queue => Volatile.Read(ref _queue);
    public RepeatMode Repeat => _repeat;
    public AbRepeat? AbRepeat => Volatile.Read(ref _abRepeat);

    public event EventHandler? Changed;

    public Task OpenAsync(MediaItem item, IReadOnlyList<MediaItem> queue, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(queue);

        var items = queue.ToArray();
        var seq = BeginRequest(supersede: true);
        return RunGatedAsync(
            token => OpenCoreAsync(item, Array.AsReadOnly(items), Array.FindIndex(items, m => m.Id == item.Id), seq, token),
            ct);
    }

    public Task NextAsync(CancellationToken ct)
    {
        var seq = BeginRequest(supersede: false);
        return RunGatedAsync(async token =>
        {
            await EnsureRepeatLoadedAsync(token).ConfigureAwait(false);
            await AdvanceAsync(1, seq, token).ConfigureAwait(false);
        }, ct);
    }

    public Task PreviousAsync(CancellationToken ct)
    {
        var seq = BeginRequest(supersede: false);
        return RunGatedAsync(async token =>
        {
            if (Current is null)
            {
                return;
            }

            if (_engine.Snapshot.PositionMs > RestartThresholdMs)
            {
                await _engine.SeekAsync(0, token).ConfigureAwait(false);
                return;
            }

            await EnsureRepeatLoadedAsync(token).ConfigureAwait(false);
            await AdvanceAsync(-1, seq, token).ConfigureAwait(false);
        }, ct);
    }

    public Task SkipAsync(long deltaMs, CancellationToken ct) =>
        RunGatedAsync(async token =>
        {
            if (Current is null)
            {
                return;
            }

            var snapshot = _engine.Snapshot;
            var target = snapshot.PositionMs + deltaMs;
            if (snapshot.DurationMs > 0)
            {
                target = Math.Min(target, snapshot.DurationMs);
            }

            await _engine.SeekAsync(Math.Max(0, target), token).ConfigureAwait(false);
        }, ct);

    public Task SetRepeatAsync(RepeatMode mode, CancellationToken ct) =>
        RunGatedAsync(async token =>
        {
            var defaults = await _defaults.GetAsync(token).ConfigureAwait(false);
            _repeatLoaded = true;
            if (Repeat != mode)
            {
                _repeat = mode;
                _dirty = true;
            }

            await _defaults.SaveAsync(defaults with { Repeat = mode }, token).ConfigureAwait(false);
        }, ct);

    public Task SetAbRepeatAsync(AbRepeat? range, CancellationToken ct) =>
        RunGatedAsync(_ =>
        {
            if (range is not null)
            {
                ValidateAbRange(range);
            }

            if (!Equals(AbRepeat, range))
            {
                SetAb(range);
                _dirty = true;
            }

            return Task.CompletedTask;
        }, ct);

    public Task CloseAsync(CancellationToken ct)
    {
        BeginRequest(supersede: true);
        return RunGatedAsync(async token =>
        {
            await _recorder.FlushAsync(token).ConfigureAwait(false);
            await _engine.StopAsync(token).ConfigureAwait(false);
            SetCurrent(null, Queue, -1);
        }, ct);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _engine.SnapshotChanged -= OnSnapshotChanged;
        _lifetime.Cancel();
        _lifetime.Dispose();
    }

    private async Task RunGatedAsync(Func<CancellationToken, Task> body, CancellationToken ct)
    {
        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            await body(ct).ConfigureAwait(false);
        }
        finally
        {
            var changed = _dirty;
            _dirty = false;
            _gate.Release();
            if (changed)
            {
                Changed?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    private async Task EnsureRepeatLoadedAsync(CancellationToken ct)
    {
        if (!_repeatLoaded)
        {
            await LoadDefaultsAsync(ct).ConfigureAwait(false);
        }
    }

    // The saved repeat mode seeds the coordinator once; afterwards the coordinator owns it.
    private async Task<GlobalDefaults> LoadDefaultsAsync(CancellationToken ct)
    {
        var defaults = await _defaults.GetAsync(ct).ConfigureAwait(false);
        if (!_repeatLoaded)
        {
            _repeatLoaded = true;
            if (Repeat != defaults.Repeat)
            {
                _repeat = defaults.Repeat;
                _dirty = true;
            }
        }

        return defaults;
    }

    private async Task OpenCoreAsync(
        MediaItem item, IReadOnlyList<MediaItem> queue, int index, long seq, CancellationToken ct)
    {
        var cts = RegisterOpen(seq, ct);
        try
        {
            var token = cts.Token;
            await _recorder.FlushAsync(token).ConfigureAwait(false);
            SetCurrent(item, queue, index);

            var state = await _stateStore.GetAsync(item.Id, token).ConfigureAwait(false);
            var defaults = await LoadDefaultsAsync(token).ConfigureAwait(false);
            var settings = PlaybackSettingsResolver.Resolve(state, defaults);
            await _engine.OpenAsync(item, settings, startPaused: false, token).ConfigureAwait(false);
        }
        finally
        {
            lock (_openLock)
            {
                if (ReferenceEquals(_runningOpen, cts))
                {
                    _runningOpen = null;
                }
            }

            cts.Dispose();
        }
    }

    // Moves within the queue. Only Folder repeat wraps around the ends.
    private async Task AdvanceAsync(int step, long seq, CancellationToken ct)
    {
        if (Current is null)
        {
            return;
        }

        var queue = Queue;
        var target = _index < 0 ? (step > 0 ? 0 : -1) : _index + step;
        if (target < 0 || target >= queue.Count)
        {
            if (Repeat != RepeatMode.Folder || queue.Count == 0)
            {
                return;
            }

            target = target < 0 ? queue.Count - 1 : 0;
        }

        if (target == _index)
        {
            await RestartAsync(ct).ConfigureAwait(false);
            return;
        }

        await OpenCoreAsync(queue[target], queue, target, seq, ct).ConfigureAwait(false);
    }

    private async Task RestartAsync(CancellationToken ct)
    {
        await _engine.SeekAsync(0, ct).ConfigureAwait(false);
        await _engine.PlayAsync(ct).ConfigureAwait(false);
    }

    private void SetCurrent(MediaItem? item, IReadOnlyList<MediaItem> queue, int index)
    {
        Volatile.Write(ref _current, item);
        Volatile.Write(ref _queue, queue);
        _index = index;
        SetAb(null);
        lock (_eventLock)
        {
            _endedSeen = false;
        }

        _dirty = true;
    }

    private void SetAb(AbRepeat? range)
    {
        lock (_eventLock)
        {
            Volatile.Write(ref _abRepeat, range);
            _abJumpPending = false;
        }
    }

    private void ValidateAbRange(AbRepeat range)
    {
        var duration = _engine.Snapshot.DurationMs;
        if (range.StartMs < 0 || range.EndMs > duration || range.EndMs - range.StartMs < MinAbRangeMs)
        {
            throw new ArgumentException(
                $"A-B range {range.StartMs}..{range.EndMs} must lie inside 0..{duration} and be at least {MinAbRangeMs} ms long.",
                nameof(range));
        }
    }

    // A newer open or close invalidates every earlier request, including one already running on the engine.
    private long BeginRequest(bool supersede)
    {
        lock (_openLock)
        {
            if (supersede)
            {
                _openSeq++;
                _runningOpen?.Cancel();
            }

            return _openSeq;
        }
    }

    private CancellationTokenSource RegisterOpen(long seq, CancellationToken ct)
    {
        var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        lock (_openLock)
        {
            if (seq != _openSeq)
            {
                cts.Dispose();
                throw new OperationCanceledException("Superseded by a newer request.");
            }

            _runningOpen = cts;
        }

        return cts;
    }

    // Runs on the engine's thread: only cheap filtering here, the work goes through the gate.
    private void OnSnapshotChanged(object? sender, PlayerSnapshot snapshot)
    {
        if (_disposed)
        {
            return;
        }

        var ended = false;
        var abJump = false;
        lock (_eventLock)
        {
            if (snapshot.Status == PlayerStatus.Ended)
            {
                ended = !_endedSeen;
                _endedSeen = true;
            }
            else
            {
                _endedSeen = false;
            }

            var ab = _abRepeat;
            if (ab is not null)
            {
                if (snapshot.PositionMs < ab.EndMs || _time.GetElapsedTime(_abJumpStamp) > AbJumpRearm)
                {
                    _abJumpPending = false;
                }

                if (snapshot.PositionMs >= ab.EndMs && snapshot.Status == PlayerStatus.Playing && !_abJumpPending)
                {
                    _abJumpPending = true;
                    _abJumpStamp = _time.GetTimestamp();
                    abJump = true;
                }
            }
        }

        if (ended)
        {
            _ = Task.Run(() => HandleSnapshotAsync(snapshot, isEnd: true));
        }

        if (abJump)
        {
            _ = Task.Run(() => HandleSnapshotAsync(snapshot, isEnd: false));
        }
    }

    private async Task HandleSnapshotAsync(PlayerSnapshot snapshot, bool isEnd)
    {
        try
        {
            var seq = BeginRequest(supersede: false);
            await RunGatedAsync(async token =>
            {
                var current = Current;
                if (current is null || snapshot.Current != current.Id)
                {
                    return;
                }

                if (isEnd)
                {
                    await EnsureRepeatLoadedAsync(token).ConfigureAwait(false);
                    if (Repeat == RepeatMode.File)
                    {
                        await RestartAsync(token).ConfigureAwait(false);
                    }
                    else
                    {
                        await AdvanceAsync(1, seq, token).ConfigureAwait(false);
                    }

                    return;
                }

                var ab = AbRepeat;
                if (ab is not null && snapshot.PositionMs >= ab.EndMs)
                {
                    await _engine.SeekAsync(ab.StartMs, token).ConfigureAwait(false);
                }
            }, _lifetime.Token).ConfigureAwait(false);
        }
        catch (Exception)
        {
            // Engine events must never fault the engine or crash the process; the next snapshot retries.
        }
    }
}
