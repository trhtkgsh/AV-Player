using Player.Core.Contracts;
using Player.Core.Models;

namespace Player.Core.Tests.Playback.Fakes;

internal sealed record OpenCall(MediaItem Item, EffectivePlaybackSettings Settings, bool StartPaused);

/// <summary>
/// Records every call in order. A successful open publishes a Playing snapshot of the opened item (position at the
/// start position, duration 0) without raising an event. Reads of <see cref="Snapshot"/> are counted so a test can
/// wait until an engine-event handler has looked at the engine.
/// </summary>
internal sealed class FakePlayerEngine : IPlayerEngine
{
    private readonly object _lock = new();
    private readonly List<string> _calls = new();
    private readonly List<OpenCall> _opens = new();
    private EventHandler<PlayerSnapshot>? _handlers;
    private int _subscribers;
    private int _snapshotReads;
    private PlayerSnapshot _snapshot = PlayerSnapshot.Idle;

    /// <summary>Awaited by <see cref="OpenAsync"/> after the call was recorded. May block or throw.</summary>
    public Func<MediaItem, Task>? OpenHook { get; set; }

    public Exception? SeekException { get; set; }
    public Exception? PlayException { get; set; }

    public PlayerSnapshot Snapshot
    {
        get
        {
            Interlocked.Increment(ref _snapshotReads);
            return Volatile.Read(ref _snapshot);
        }
        set => Volatile.Write(ref _snapshot, value);
    }

    public int SnapshotReads => Volatile.Read(ref _snapshotReads);

    public int SubscriberCount
    {
        get
        {
            lock (_lock)
            {
                return _subscribers;
            }
        }
    }

    public IReadOnlyList<string> Calls
    {
        get
        {
            lock (_lock)
            {
                return _calls.ToArray();
            }
        }
    }

    public IReadOnlyList<OpenCall> Opens
    {
        get
        {
            lock (_lock)
            {
                return _opens.ToArray();
            }
        }
    }

    public IReadOnlyList<TrackInfo> AudioTracks { get; } = Array.Empty<TrackInfo>();
    public IReadOnlyList<TrackInfo> SubtitleTracks { get; } = Array.Empty<TrackInfo>();

    public event EventHandler<PlayerSnapshot>? SnapshotChanged
    {
        add
        {
            lock (_lock)
            {
                _handlers += value;
                _subscribers++;
            }
        }
        remove
        {
            lock (_lock)
            {
                _handlers -= value;
                _subscribers--;
            }
        }
    }

    public event EventHandler? TracksChanged;

    public int? CurrentAudioTrackId { get; set; }
    public int? CurrentSubtitleTrackId { get; set; }
    public long AudioDelayMs { get; set; }
    public long SubtitleDelayMs { get; set; }
    public SubtitleStyle SubtitleStyle { get; set; } = SubtitleStyle.Default;

    public void RaiseTracksChanged() => TracksChanged?.Invoke(this, EventArgs.Empty);

    public int Count(string call) => Calls.Count(c => c == call);

    public int CountPrefix(string prefix) => Calls.Count(c => c.StartsWith(prefix, StringComparison.Ordinal));

    /// <summary>Sets <see cref="Snapshot"/> and raises <see cref="SnapshotChanged"/> on the calling thread.</summary>
    public void Raise(PlayerSnapshot snapshot)
    {
        Snapshot = snapshot;
        RaiseOnly(snapshot);
    }

    /// <summary>Raises the event with a snapshot without touching <see cref="Snapshot"/>.</summary>
    public void RaiseOnly(PlayerSnapshot snapshot)
    {
        EventHandler<PlayerSnapshot>? handlers;
        lock (_lock)
        {
            handlers = _handlers;
        }

        handlers?.Invoke(this, snapshot);
    }

    public async Task OpenAsync(MediaItem item, EffectivePlaybackSettings settings, bool startPaused, CancellationToken ct)
    {
        lock (_lock)
        {
            _calls.Add($"Open:{item.DisplayName}");
            _opens.Add(new OpenCall(item, settings, startPaused));
        }

        CurrentAudioTrackId = settings.AudioTrackId;
        CurrentSubtitleTrackId = settings.SubtitleTrackId;
        AudioDelayMs = settings.AudioDelayMs;
        SubtitleDelayMs = settings.SubtitleDelayMs;
        SubtitleStyle = settings.SubtitleStyle;

        var hook = OpenHook;
        if (hook is not null)
        {
            await hook(item).ConfigureAwait(false);
        }

        ct.ThrowIfCancellationRequested();
        Snapshot = PlayerSnapshot.Idle with
        {
            Status = PlayerStatus.Playing,
            Current = item.Id,
            PositionMs = settings.StartPositionMs,
        };
    }

    public Task PlayAsync(CancellationToken ct)
    {
        Record("Play");
        return PlayException is { } ex ? Task.FromException(ex) : Task.CompletedTask;
    }

    public Task PauseAsync(CancellationToken ct)
    {
        Record("Pause");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken ct)
    {
        Record("Stop");
        return Task.CompletedTask;
    }

    public Task SeekAsync(long positionMs, CancellationToken ct)
    {
        Record($"Seek:{positionMs}");
        return SeekException is { } ex ? Task.FromException(ex) : Task.CompletedTask;
    }

    public Task SetSpeedAsync(float speed, CancellationToken ct) => Task.CompletedTask;

    public Task SetVolumeAsync(int percent, CancellationToken ct) => Task.CompletedTask;

    public Task SetDecoderAsync(DecoderMode mode, CancellationToken ct) => Task.CompletedTask;

    public Task SetVideoEnabledAsync(bool enabled, CancellationToken ct) => Task.CompletedTask;

    public Task SelectAudioTrackAsync(int trackId, CancellationToken ct)
    {
        CurrentAudioTrackId = trackId;
        return Task.CompletedTask;
    }

    public Task SetAudioDelayAsync(long delayMs, CancellationToken ct)
    {
        AudioDelayMs = delayMs;
        return Task.CompletedTask;
    }

    public Task SelectSubtitleTrackAsync(int trackId, CancellationToken ct)
    {
        CurrentSubtitleTrackId = trackId;
        return Task.CompletedTask;
    }

    public Task SetSubtitleDelayAsync(long delayMs, CancellationToken ct)
    {
        SubtitleDelayMs = delayMs;
        return Task.CompletedTask;
    }

    public Task AddSubtitleFileAsync(string localFilePath, CancellationToken ct) => Task.CompletedTask;

    public Task SetSubtitleStyleAsync(SubtitleStyle style, CancellationToken ct)
    {
        SubtitleStyle = style;
        return Task.CompletedTask;
    }

    private void Record(string call)
    {
        lock (_lock)
        {
            _calls.Add(call);
        }
    }
}
