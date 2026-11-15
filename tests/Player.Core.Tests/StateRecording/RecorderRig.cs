using Microsoft.Extensions.Time.Testing;
using Player.Core.Logic.StateRecording;
using Player.Core.Models;
using Player.Core.Tests.Playback.Fakes;

namespace Player.Core.Tests.StateRecording;

/// <summary>
/// A recorder wired to fakes. The recorder works on background tasks, so a test never sleeps: it calls
/// <see cref="FlushAsync"/> (which also triggers a save) or waits on the store with
/// <see cref="RecordingStateStore.WaitForSavesAsync"/> when it must not trigger one.
/// </summary>
internal sealed class RecorderRig : IDisposable
{
    public static readonly DateTimeOffset Start = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    public RecorderRig()
    {
        Time = new FakeTimeProvider(Start);
        Recorder = new PlaybackStateRecorder(Engine, Store, Lifecycle, Time);
    }

    public FakePlayerEngine Engine { get; } = new();
    public RecordingStateStore Store { get; } = new();
    public FakeAppLifecycle Lifecycle { get; } = new();
    public FakeTimeProvider Time { get; }
    public PlaybackStateRecorder Recorder { get; }

    public long NowUnixSeconds => Time.GetUtcNow().ToUnixTimeSeconds();

    /// <summary>Raises a snapshot of <paramref name="id"/> on the engine.</summary>
    public void Publish(PlayerStatus status, MediaFileId id, long positionMs, long durationMs) =>
        Engine.Raise(PlayerSnapshot.Idle with
        {
            Status = status,
            Current = id,
            PositionMs = positionMs,
            DurationMs = durationMs,
        });

    /// <summary>Raises the engine snapshot for "nothing is open".</summary>
    public void PublishNothingOpen() => Engine.Raise(PlayerSnapshot.Idle);

    public Task FlushAsync() => Recorder.FlushAsync(CancellationToken.None);

    /// <summary>
    /// Starts tracking the file and waits until its saved state is loaded. The snapshot changes nothing, so
    /// nothing is saved and the file is clean afterwards.
    /// </summary>
    public async Task TrackAsync(MediaFileId id)
    {
        Publish(PlayerStatus.Opening, id, 0, 0);
        await FlushAsync();
    }

    public void Dispose() => Recorder.Dispose();
}
