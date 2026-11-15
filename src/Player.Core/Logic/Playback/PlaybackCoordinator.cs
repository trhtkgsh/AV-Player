using Player.Core.Contracts;
using Player.Core.Logic.Resolution;
using Player.Core.Models;

namespace Player.Core.Logic.Playback;

/// <summary>
/// Runs a queue on top of the engine. Public commands and engine events share one gate, so they never interleave.
/// <see cref="Changed"/> is raised after the gate is released (the one exception is a repeat change that arrives
/// from <see cref="IGlobalDefaultsStore.Changed"/>, which is raised on the store's thread).
/// Engine events are handled in <c>PlaybackCoordinator.Events.cs</c>.
/// </summary>
public sealed partial class PlaybackCoordinator : IPlaybackCoordinator, IDisposable
{
    private const long RestartThresholdMs = 3000;
    private const long MinAbRangeMs = 1000;
    private static readonly TimeSpan AbJumpRearm = TimeSpan.FromSeconds(1);

    private readonly IPlayerEngine _engine;
    private readonly IPlaybackStateStore _stateStore;
    private readonly IPlaybackStateRecorder _recorder;
    private readonly IGlobalDefaultsStore _defaults;
    private readonly TimeProvider _time;
    private readonly IVideoSurface? _videoSurface;
    private readonly IFolderSettingsStore? _folderSettings;

    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly CancellationTokenSource _lifetime = new();
    private readonly CancellationToken _lifetimeToken;
    private readonly object _eventLock = new();
    private readonly object _openLock = new();

    private MediaItem? _current;
    private IReadOnlyList<MediaItem> _queue = Array.Empty<MediaItem>();
    private int _index = -1;
    private volatile RepeatMode _repeat;
    private AbRepeat? _abRepeat;
    private volatile bool _repeatLoaded;
    private bool _dirty;
    private int _disposed;

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
        TimeProvider timeProvider,
        IVideoSurface? videoSurface = null,
        IFolderSettingsStore? folderSettings = null)
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
        _videoSurface = videoSurface;
        _folderSettings = folderSettings;
        _lifetimeToken = _lifetime.Token;
        _engine.SnapshotChanged += OnSnapshotChanged;
        _defaults.Changed += OnDefaultsChanged;
    }

    public MediaItem? Current => Volatile.Read(ref _current);
    public IReadOnlyList<MediaItem> Queue => Volatile.Read(ref _queue);
    public RepeatMode Repeat => _repeat;
    public AbRepeat? AbRepeat => Volatile.Read(ref _abRepeat);

    public event EventHandler? Changed;

    public Task OpenAsync(MediaItem item, IReadOnlyList<MediaItem> queue, CancellationToken ct)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(queue);

        // A request that is already cancelled must not cancel the open that is running.
        if (ct.IsCancellationRequested)
        {
            return Task.FromCanceled(ct);
        }

        var items = queue.ToArray();
        var seq = BeginRequest(supersede: true);
        return RunGatedAsync(
            token => OpenCoreAsync(item, Array.AsReadOnly(items), Array.FindIndex(items, m => m.Id == item.Id), seq, token),
            ct);
    }

    public Task NextAsync(CancellationToken ct)
    {
        ThrowIfDisposed();
        var issuedFor = Current?.Id;
        var seq = BeginRequest(supersede: false);
        return RunGatedAsync(async token =>
        {
            // An automatic next for the same ended file got the gate first: do not skip a second item.
            if (Current?.Id != issuedFor)
            {
                return;
            }

            await EnsureRepeatLoadedAsync(token).ConfigureAwait(false);
            await AdvanceAsync(1, seq, token).ConfigureAwait(false);
        }, ct);
    }

    public Task PreviousAsync(CancellationToken ct)
    {
        ThrowIfDisposed();
        var issuedFor = Current?.Id;
        var seq = BeginRequest(supersede: false);
        return RunGatedAsync(async token =>
        {
            if (Current is null || Current.Id != issuedFor)
            {
                return;
            }

            var snapshot = _engine.Snapshot;
            if (snapshot.PositionMs > RestartThresholdMs)
            {
                await _engine.SeekAsync(0, token).ConfigureAwait(false);
                if (snapshot.Status == PlayerStatus.Ended)
                {
                    // On an ended input a seek only stores the start position; play restarts from it.
                    await _engine.PlayAsync(token).ConfigureAwait(false);
                }

                return;
            }

            await EnsureRepeatLoadedAsync(token).ConfigureAwait(false);
            await AdvanceAsync(-1, seq, token).ConfigureAwait(false);
        }, ct);
    }

    public Task SkipAsync(long deltaMs, CancellationToken ct)
    {
        ThrowIfDisposed();
        return RunGatedAsync(async token =>
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
    }

    public Task SetRepeatAsync(RepeatMode mode, CancellationToken ct)
    {
        ThrowIfDisposed();
        return RunGatedAsync(async token =>
        {
            // Save first: the in-memory value changes only when the save succeeded.
            var defaults = await _defaults.GetAsync(token).ConfigureAwait(false);
            await _defaults.SaveAsync(defaults with { Repeat = mode }, token).ConfigureAwait(false);
            _repeatLoaded = true;
            if (ApplyRepeat(mode))
            {
                _dirty = true;
            }
        }, ct);
    }

    public Task SetAbRepeatAsync(AbRepeat? range, CancellationToken ct)
    {
        ThrowIfDisposed();
        return RunGatedAsync(_ =>
        {
            if (range is not null)
            {
                var current = Current;
                var snapshot = _engine.Snapshot;
                if (current is null || snapshot.Current != current.Id)
                {
                    throw new InvalidOperationException("A-B repeat needs an open item.");
                }

                ValidateAbRange(range, snapshot.DurationMs);
            }

            if (!Equals(AbRepeat, range))
            {
                SetAb(range);
                _dirty = true;
            }

            return Task.CompletedTask;
        }, ct);
    }

    public Task CloseAsync(CancellationToken ct)
    {
        ThrowIfDisposed();
        if (ct.IsCancellationRequested)
        {
            return Task.FromCanceled(ct);
        }

        BeginRequest(supersede: true);
        return RunGatedAsync(async token =>
        {
            await SafeFlushAsync(token).ConfigureAwait(false);
            await _engine.StopAsync(token).ConfigureAwait(false);
            if (Current is not null)
            {
                SetCurrent(null, Queue, -1);
            }
        }, ct);
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        _engine.SnapshotChanged -= OnSnapshotChanged;
        _defaults.Changed -= OnDefaultsChanged;
        _lifetime.Cancel();
        _lifetime.Dispose();
    }

    private bool IsDisposed => Volatile.Read(ref _disposed) != 0;

    private void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(IsDisposed, this);

    private async Task RunGatedAsync(Func<CancellationToken, Task> body, CancellationToken ct)
    {
        await _gate.WaitAsync(ct).ConfigureAwait(false);
        var changed = false;
        try
        {
            await body(ct).ConfigureAwait(false);
        }
        finally
        {
            changed = _dirty;
            _dirty = false;
            _gate.Release();
            if (changed)
            {
                RaiseChanged();
            }
        }
    }

    // A faulty subscriber must never fault a command or replace its exception.
    private void RaiseChanged()
    {
        try
        {
            Changed?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception)
        {
            // Swallowed on purpose.
        }
    }

    private bool ApplyRepeat(RepeatMode mode)
    {
        lock (_eventLock)
        {
            if (_repeat == mode)
            {
                return false;
            }

            _repeat = mode;
            return true;
        }
    }

    // Runs on the store's thread. Never throws.
    private void OnDefaultsChanged(object? sender, GlobalDefaults value)
    {
        try
        {
            if (value is null || IsDisposed)
            {
                return;
            }

            _repeatLoaded = true;
            if (ApplyRepeat(value.Repeat))
            {
                RaiseChanged();
            }
        }
        catch (Exception)
        {
            // A store event must never crash the store.
        }
    }

    // A failing store must not block playback: only cancellation of the given token propagates.
    private static bool IsSwallowable(Exception ex, CancellationToken ct) =>
        !(ex is OperationCanceledException && ct.IsCancellationRequested);

    private async Task SafeFlushAsync(CancellationToken ct)
    {
        try
        {
            await _recorder.FlushAsync(ct).ConfigureAwait(false);
        }
        catch (Exception ex) when (IsSwallowable(ex, ct))
        {
            // Playback goes on without the save.
        }
    }

    private async Task<PlaybackState?> SafeGetStateAsync(MediaFileId id, CancellationToken ct)
    {
        try
        {
            return await _stateStore.GetAsync(id, ct).ConfigureAwait(false);
        }
        catch (Exception ex) when (IsSwallowable(ex, ct))
        {
            return null;
        }
    }

    private async Task<FolderSettings?> SafeGetFolderSettingsAsync(long bucketId, CancellationToken ct)
    {
        if (_folderSettings is null)
        {
            return null;
        }

        try
        {
            return await _folderSettings.GetAsync(bucketId, ct).ConfigureAwait(false);
        }
        catch (Exception ex) when (IsSwallowable(ex, ct))
        {
            return null;
        }
    }

    private async Task<GlobalDefaults?> SafeGetDefaultsAsync(CancellationToken ct)
    {
        try
        {
            return await _defaults.GetAsync(ct).ConfigureAwait(false);
        }
        catch (Exception ex) when (IsSwallowable(ex, ct))
        {
            return null;
        }
    }

    private async Task EnsureRepeatLoadedAsync(CancellationToken ct)
    {
        if (!_repeatLoaded)
        {
            await LoadDefaultsAsync(ct).ConfigureAwait(false);
        }
    }

    // The saved repeat mode seeds the coordinator once; afterwards the store's Changed event and SetRepeatAsync own it.
    // A failing store yields the factory defaults and leaves the repeat mode unseeded, so the next call tries again.
    private async Task<GlobalDefaults> LoadDefaultsAsync(CancellationToken ct)
    {
        var defaults = await SafeGetDefaultsAsync(ct).ConfigureAwait(false);
        if (defaults is null)
        {
            return new GlobalDefaults();
        }

        if (!_repeatLoaded)
        {
            _repeatLoaded = true;
            if (ApplyRepeat(defaults.Repeat))
            {
                _dirty = true;
            }
        }

        return defaults;
    }

    // Current, Queue and the index change only after the engine accepted the open.
    private async Task OpenCoreAsync(
        MediaItem item, IReadOnlyList<MediaItem> queue, int index, long seq, CancellationToken ct)
    {
        var cts = RegisterOpen(seq, ct);
        try
        {
            var token = cts.Token;
            await SafeFlushAsync(token).ConfigureAwait(false);
            var state = await SafeGetStateAsync(item.Id, token).ConfigureAwait(false);
            var defaults = await LoadDefaultsAsync(token).ConfigureAwait(false);
            var folder = await SafeGetFolderSettingsAsync(item.BucketId, token).ConfigureAwait(false);
            var settings = PlaybackSettingsResolver.Resolve(state, defaults, folder);
            token.ThrowIfCancellationRequested();
            try
            {
                _videoSurface?.SetAspect(settings.Aspect);
            }
            catch (Exception)
            {
                // Core has no logging framework, and a surface failure (for example a surface that is not
                // attached yet) must not fail the open: the item still plays with the surface's current mode.
            }

            await _engine.OpenAsync(item, settings, startPaused: false, token).ConfigureAwait(false);
            SetCurrent(item, queue, index);
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

    // The engine reports duration 0 for a moment after opening: then only the lower bound and the length are checked.
    private static void ValidateAbRange(AbRepeat range, long durationMs)
    {
        if (range.StartMs < 0
            || range.EndMs - range.StartMs < MinAbRangeMs
            || (durationMs > 0 && range.EndMs > durationMs))
        {
            throw new ArgumentException(
                $"A-B range {range.StartMs}..{range.EndMs} must lie inside 0..{durationMs} and be at least {MinAbRangeMs} ms long.",
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
        var cts = CancellationTokenSource.CreateLinkedTokenSource(ct, _lifetimeToken);
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
}
