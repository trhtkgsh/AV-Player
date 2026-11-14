using System.Diagnostics;
using Microsoft.Extensions.Time.Testing;
using Player.Core.Logic.Playback;
using Player.Core.Models;
using Player.Core.Tests.Playback.Fakes;

namespace Player.Core.Tests.Playback;

/// <summary>
/// A coordinator wired to fakes, with a queue of three items (v1, v2, v3).
/// The engine-event handlers run on background tasks, so tests wait for effects with <see cref="WaitUntilAsync"/>.
/// </summary>
internal sealed class CoordinatorRig : IDisposable
{
    private int _changed;
    private int _reentered;

    public CoordinatorRig()
    {
        Queue = new[]
        {
            TestData.Item(1, "v1"),
            TestData.Item(2, "v2"),
            TestData.Item(3, "v3"),
        };
        Coordinator = new PlaybackCoordinator(Engine, States, Recorder, Defaults, Time);
        Coordinator.Changed += (_, _) => Interlocked.Increment(ref _changed);
    }

    public FakePlayerEngine Engine { get; } = new();
    public FakePlaybackStateStore States { get; } = new();
    public FakePlaybackStateRecorder Recorder { get; } = new();
    public FakeGlobalDefaultsStore Defaults { get; } = new();
    public FakeTimeProvider Time { get; } = new();
    public PlaybackCoordinator Coordinator { get; }
    public IReadOnlyList<MediaItem> Queue { get; }

    public int ChangedCount => Volatile.Read(ref _changed);

    /// <summary>Set by <see cref="ReenterNextOnFirstChanged"/>.</summary>
    public MediaItem? CurrentSeenInChanged { get; private set; }

    /// <summary>Set by <see cref="ReenterNextOnFirstChanged"/>: false means the nested call did not finish in time.</summary>
    public bool? NestedNextFinished { get; private set; }

    public Task OpenAsync(int index = 0) => Coordinator.OpenAsync(Queue[index], Queue, CancellationToken.None);

    public Task BarrierAsync() => Coordinator.SetAbRepeatAsync(null, CancellationToken.None);

    public static PlayerSnapshot Snap(PlayerStatus status, MediaItem item, long positionMs = 0, long durationMs = 0) =>
        PlayerSnapshot.Idle with
        {
            Status = status,
            Current = item.Id,
            PositionMs = positionMs,
            DurationMs = durationMs,
        };

    /// <summary>
    /// Waits until a handler that was started after <paramref name="readsBefore"/> has read the engine snapshot,
    /// then until the gate is free again, so the handler has finished.
    /// </summary>
    public async Task WaitForHandlerAsync(int readsBefore)
    {
        await WaitUntilAsync(() => Engine.SnapshotReads > readsBefore, "the engine event handler to read the snapshot");
        await BarrierAsync();
    }

    /// <summary>On the first Changed event, reads Current and runs NextAsync and waits for it, from inside the event.</summary>
    public void ReenterNextOnFirstChanged()
    {
        Coordinator.Changed += (_, _) =>
        {
            if (Interlocked.Exchange(ref _reentered, 1) != 0)
            {
                return;
            }

            CurrentSeenInChanged = Coordinator.Current;
            NestedNextFinished = Coordinator.NextAsync(CancellationToken.None).Wait(TimeSpan.FromSeconds(5));
        };
    }

    /// <summary>Polls until the condition holds. Fails the test after 5 s. The wait is a failure bound, not a guess.</summary>
    public static async Task WaitUntilAsync(Func<bool> condition, string description = "the expected effect")
    {
        var watch = Stopwatch.StartNew();
        while (!condition())
        {
            if (watch.Elapsed > TimeSpan.FromSeconds(5))
            {
                Assert.Fail($"Timed out after 5 s waiting for {description}.");
            }

            await Task.Delay(2);
        }
    }

    public void Dispose() => Coordinator.Dispose();
}
