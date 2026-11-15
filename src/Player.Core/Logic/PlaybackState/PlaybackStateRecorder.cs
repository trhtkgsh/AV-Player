using Player.Core.Contracts;
using Player.Core.Models;

// Not Player.Core.Logic.PlaybackState: that namespace would hide the PlaybackState model for
// every sibling namespace under Player.Core.Logic (CS0118 in six existing files).
namespace Player.Core.Logic.StateRecording;

/// <summary>
/// Follows the engine and persists the open file's playback state.
/// Threading: engine events, <see cref="Update"/>, lifecycle events, the timer and the work
/// items all enter under one private lock that is held only for in-memory work, never for I/O.
/// Every store call (load or save) is a work item chained on one task, so loads and saves never
/// overlap and reach the store in the order they were triggered. Work items read the state
/// when they run, so a save always writes the newest state and is skipped when nothing changed.
/// The lifecycle handlers (Backgrounded, Stopping) only queue the write and return, so a caller
/// that needs the write finished must await <see cref="FlushAsync"/>.
/// Saves queued before <see cref="Dispose"/> still run.
/// </summary>
public sealed class PlaybackStateRecorder : IPlaybackStateRecorder, IDisposable
{
    public static readonly TimeSpan DebounceInterval = TimeSpan.FromSeconds(5);

    private readonly IPlayerEngine _engine;
    private readonly IPlaybackStateStore _store;
    private readonly IAppLifecycle _lifecycle;
    private readonly TimeProvider _time;

    private readonly object _gate = new();
    // Files with a save queued, running, or failed and awaiting retry; bounded by outstanding work.
    private readonly HashSet<Track> _live = [];
    private Task _tail = Task.CompletedTask;
    private Track? _track;
    private ITimer? _timer;
    private object? _timerToken;
    private bool _disposed;

    public PlaybackStateRecorder(
        IPlayerEngine engine, IPlaybackStateStore store, IAppLifecycle lifecycle, TimeProvider timeProvider)
    {
        _engine = engine;
        _store = store;
        _lifecycle = lifecycle;
        _time = timeProvider;

        _engine.SnapshotChanged += OnSnapshot;
        _lifecycle.Backgrounded += OnLifecycle;
        _lifecycle.Stopping += OnLifecycle;
    }

    /// <summary>
    /// Applies a per-file override and saves immediately. <paramref name="change"/> must be pure:
    /// it runs under the recorder's lock and runs a second time when the file's saved state is
    /// still loading (or its load failed), to replay it on top of the loaded state.
    /// </summary>
    public void Update(Func<PlaybackState, PlaybackState> change)
    {
        lock (_gate)
        {
            if (_disposed || _track is not { } track)
            {
                return;
            }

            // ApplyChange runs the change first, so a throwing change leaves everything untouched.
            // The caller owns only the override fields.
            ApplyChange(track, current => change(current) with
            {
                Id = current.Id,
                PositionMs = current.PositionMs,
                DurationMs = current.DurationMs,
                Finished = current.Finished,
                LastPlayedUtc = current.LastPlayedUtc,
            });
            SaveNow();
        }
    }

    public async Task FlushAsync(CancellationToken ct)
    {
        Task tail;
        lock (_gate)
        {
            if (_disposed)
            {
                return;
            }

            SaveNow();
            tail = _tail;
        }

        // Work items never fault, so the only way this throws is ct.
        await tail.WaitAsync(ct).ConfigureAwait(false);
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
            _engine.SnapshotChanged -= OnSnapshot;
            _lifecycle.Backgrounded -= OnLifecycle;
            _lifecycle.Stopping -= OnLifecycle;
            CancelTimer();
            _live.Clear();
        }
    }

    private void OnLifecycle(object? sender, EventArgs e)
    {
        lock (_gate)
        {
            if (!_disposed)
            {
                SaveNow();
            }
        }
    }

    private void OnSnapshot(object? sender, PlayerSnapshot snapshot)
    {
        lock (_gate)
        {
            if (_disposed)
            {
                return;
            }

            if (_track?.Id != snapshot.Current)
            {
                SwitchTo(snapshot.Current);
            }

            if (_track is not { } track)
            {
                return;
            }

            var statusChanged = track.LastStatus != snapshot.Status;
            track.LastStatus = snapshot.Status;
            if (snapshot.Status is not (PlayerStatus.Idle or PlayerStatus.Error))
            {
                ApplyPosition(track, snapshot.PositionMs, snapshot.DurationMs);
            }

            if (statusChanged && snapshot.Status is
                PlayerStatus.Paused or PlayerStatus.Ended or PlayerStatus.Idle or PlayerStatus.Error)
            {
                SaveNow();
            }
            else if (track.Loading ? !track.LoadQueued : track.IsDirty)
            {
                StartTimerIfNone(); // also the retry clock for a failed load
            }
        }
    }

    private static PlaybackState WithPosition(PlaybackState state, long positionMs, long durationMs)
    {
        // The engine reports 0 until the length is known: never overwrite a known duration.
        var duration = durationMs > 0 ? durationMs : state.DurationMs;
        return state with
        {
            PositionMs = positionMs,
            DurationMs = duration,
            Finished = duration > 0 && positionMs > 0.95 * duration,
        };
    }

    // Callers hold _gate.
    private void SwitchTo(MediaFileId? id)
    {
        CancelTimer();
        RetryUnsaved();

        if (_track is { } old)
        {
            if (old.Loading && !old.LoadQueued)
            {
                // Its load failed and was never retried: drop the queued changes, write nothing.
                old.PendingChanges.Clear();
            }
            else
            {
                EnqueueSave(old); // an in-flight load finishes first, then this save
            }
        }

        _track = null;
        if (id is { } newId)
        {
            // The retries queued above run before the new track's load. If one of them fails
            // again it must never be retried later: it would overwrite the newer state.
            foreach (var track in _live)
            {
                track.Superseded |= track.Id == newId;
            }

            _track = new Track(newId);
            EnqueueLoad(_track);
        }
    }

    // Callers hold _gate. While the saved state is not loaded, the change is applied to a
    // provisional state (to surface exceptions) and remembered for replay on the loaded state.
    private static void ApplyChange(Track track, Func<PlaybackState, PlaybackState> change)
    {
        var next = change(track.State);
        if (track.Loading)
        {
            track.State = next;
            track.PendingChanges.Add(change);
        }
        else if (next != track.State)
        {
            track.State = next;
            track.Version++;
        }
    }

    // Callers hold _gate. Position and duration are kept as latest values, not one entry per snapshot.
    private static void ApplyPosition(Track track, long positionMs, long durationMs)
    {
        var next = WithPosition(track.State, positionMs, durationMs);
        if (track.Loading)
        {
            track.State = next;
            track.HasPosition = true;
            track.PendingPosition = positionMs;
            track.PendingDuration = durationMs > 0 ? durationMs : track.PendingDuration;
        }
        else if (next != track.State)
        {
            track.State = next;
            track.Version++;
        }
    }

    // Callers hold _gate. Cancels the debounce, retries a failed load and queues a save of
    // every unsaved state.
    private void SaveNow()
    {
        CancelTimer();
        RetryUnsaved();
        if (_track is { } track)
        {
            if (track.Loading)
            {
                EnqueueLoad(track);
            }

            EnqueueSave(track);
        }
    }

    private void EnqueueLoad(Track track)
    {
        if (!track.LoadQueued)
        {
            track.LoadQueued = true;
            Enqueue(() => LoadAsync(track));
        }
    }

    private void RetryUnsaved()
    {
        foreach (var track in _live.ToArray())
        {
            if (!track.Superseded && !ReferenceEquals(track, _track))
            {
                EnqueueSave(track);
            }
        }
    }

    private void EnqueueSave(Track track)
    {
        if (track.NeedsSave)
        {
            track.SaveQueued = true;
            _live.Add(track);
            Enqueue(() => SaveAsync(track));
        }
    }

    private void StartTimerIfNone()
    {
        if (_timer is not null)
        {
            return;
        }

        var token = new object();
        _timerToken = token;
        _timer = _time.CreateTimer(OnTimer, token, DebounceInterval, Timeout.InfiniteTimeSpan);
    }

    private void CancelTimer()
    {
        _timer?.Dispose();
        _timer = null;
        _timerToken = null;
    }

    private void OnTimer(object? token)
    {
        lock (_gate)
        {
            // A cancelled timer's callback may still arrive: ignore it.
            if (_disposed || !ReferenceEquals(token, _timerToken))
            {
                return;
            }

            SaveNow();
        }
    }

    // Callers hold _gate. Always hops to the thread pool, so no store call runs on the caller.
    private void Enqueue(Func<Task> work)
    {
        var previous = _tail;
        _tail = Task.Run(async () =>
        {
            await previous.ConfigureAwait(false);
            try
            {
                await work().ConfigureAwait(false);
            }
            catch (Exception)
            {
                // Work items handle their own store errors; nothing may fault the chain.
            }
        });
    }

    private async Task LoadAsync(Track track)
    {
        PlaybackState? loaded = null;
        var succeeded = false;
        try
        {
            loaded = await _store.GetAsync(track.Id, CancellationToken.None).ConfigureAwait(false);
            succeeded = true;
        }
        catch (Exception)
        {
            // Not loaded: nothing may be written for this file until a load succeeds.
        }

        lock (_gate)
        {
            track.LoadQueued = false;
            var current = !_disposed && ReferenceEquals(track, _track);
            if (!succeeded)
            {
                if (current)
                {
                    StartTimerIfNone(); // retry in one debounce interval
                }
                else
                {
                    track.PendingChanges.Clear(); // abandoned before a load succeeded
                }

                return;
            }

            var baseline = loaded ?? new PlaybackState(track.Id);
            var state = baseline;
            foreach (var change in track.PendingChanges)
            {
                try
                {
                    state = change(state);
                }
                catch (Exception)
                {
                    // The change already ran once on the provisional state; skip it on failure.
                }
            }

            if (track.HasPosition)
            {
                state = WithPosition(state, track.PendingPosition, track.PendingDuration);
            }

            track.PendingChanges.Clear();
            track.Loading = false;
            track.State = state;
            if (state != baseline)
            {
                track.Version++;
            }

            if (current && track.IsDirty)
            {
                StartTimerIfNone();
            }
        }
    }

    private async Task SaveAsync(Track track)
    {
        PlaybackState snapshot;
        long version;
        lock (_gate)
        {
            track.SaveQueued = false;
            if (!track.IsDirty)
            {
                Release(track);
                return;
            }

            snapshot = track.State with { LastPlayedUtc = _time.GetUtcNow().ToUnixTimeSeconds() };
            track.State = snapshot;
            version = track.Version;
            track.InFlightVersion = version;
        }

        var saved = false;
        try
        {
            await _store.SaveAsync(snapshot, CancellationToken.None).ConfigureAwait(false);
            saved = true;
        }
        catch (Exception)
        {
            // Stays dirty: the next trigger retries.
        }

        lock (_gate)
        {
            track.InFlightVersion = 0;
            if (saved)
            {
                track.SavedVersion = Math.Max(track.SavedVersion, version);
            }

            Release(track);
        }
    }

    // Callers hold _gate. Stops tracking a file once nothing more can be written for it. An
    // untracked file with a failed save stays, to be retried, unless a newer track for the same
    // id exists: the stale state must never be written over it.
    private void Release(Track track)
    {
        var retry = !_disposed && !track.Superseded && track.IsDirty && !ReferenceEquals(track, _track);
        if (!track.SaveQueued && !retry)
        {
            _live.Remove(track);
        }
    }

    private sealed class Track(MediaFileId id)
    {
        public MediaFileId Id { get; } = id;
        public PlaybackState State { get; set; } = new(id);
        public PlayerStatus? LastStatus { get; set; }

        // True until a load succeeded: State is provisional and nothing is written.
        public bool Loading { get; set; } = true;
        public bool LoadQueued { get; set; }
        public List<Func<PlaybackState, PlaybackState>> PendingChanges { get; } = [];
        public bool HasPosition { get; set; }
        public long PendingPosition { get; set; }
        public long PendingDuration { get; set; }
        public long Version { get; set; }
        public long SavedVersion { get; set; }
        public bool IsDirty => Version != SavedVersion;

        public bool SaveQueued { get; set; }
        public long InFlightVersion { get; set; } // 0 when no write is running
        public bool Superseded { get; set; } // a newer track exists for the same id

        // A queued save writes the newest state when it runs, so one is enough; a version that
        // is already being written needs no second save. A loading track is saved after its load.
        public bool NeedsSave => !SaveQueued && (Loading || Version > Math.Max(SavedVersion, InFlightVersion));
    }
}
