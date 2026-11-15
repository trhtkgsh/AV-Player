using Microsoft.Extensions.Time.Testing;
using Player.Core.Logic.StateRecording;
using Player.Core.Models;

namespace Player.Core.Tests.StateRecording;

/// <summary>
/// A fake clock that counts the timers the recorder creates. A failed save starts its retry timer on a
/// background task, so a test waits for the timer before advancing the clock: that makes the order certain.
/// </summary>
internal sealed class TimerCountingTime : FakeTimeProvider
{
    private readonly object _gate = new();
    private readonly List<(int Target, TaskCompletionSource Source)> _waiters = new();
    private int _created;

    public TimerCountingTime()
        : base(RecorderRig.Start)
    {
    }

    public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
    {
        var timer = base.CreateTimer(callback, state, dueTime, period);
        TaskCompletionSource[] ready;
        lock (_gate)
        {
            _created++;
            ready = _waiters.Where(w => w.Target <= _created).Select(w => w.Source).ToArray();
            _waiters.RemoveAll(w => w.Target <= _created);
        }

        foreach (var source in ready)
        {
            source.TrySetResult();
        }

        return timer;
    }

    /// <summary>Completes when at least <paramref name="count"/> timers were created in total.</summary>
    public Task WaitForTimersAsync(int count)
    {
        lock (_gate)
        {
            if (_created >= count)
            {
                return Task.CompletedTask;
            }

            var source = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            _waiters.Add((count, source));
            return source.Task.WaitAsync(RecordingStateStore.HangGuard);
        }
    }
}

public sealed class PlaybackStateRecorderRetryTests
{
    private static readonly MediaFileId A = TestData.Id(1);
    private static readonly MediaFileId B = TestData.Id(2);
    private static readonly TimeSpan Interval = PlaybackStateRecorder.DebounceInterval;

    private static (RecorderRig Rig, TimerCountingTime Time) NewRig()
    {
        var time = new TimerCountingTime();
        return (new RecorderRig(time), time);
    }

    /// <summary>Tracks A, makes the next save fail and pauses at the position; waits until the retry timer runs.</summary>
    private static async Task PauseWithFailingSaveAsync(RecorderRig rig, TimerCountingTime time, long positionMs)
    {
        await rig.TrackAsync(A);
        rig.Store.FailNextSaves(1);
        rig.Publish(PlayerStatus.Paused, A, positionMs, 600_000);
        await time.WaitForTimersAsync(1);
    }

    // ---- timer retry ----

    [Fact]
    public async Task Retry_FailedSaveBeforeTheClockAdvances_StoresNothing()
    {
        var (rig, time) = NewRig();
        using (rig)
        {
            await PauseWithFailingSaveAsync(rig, time, 12_345);

            Assert.Equal(1, rig.Store.SaveCalls);
            Assert.Empty(rig.Store.Saves);
            Assert.Null(rig.Store.Stored(A));
        }
    }

    [Fact]
    public async Task Retry_FailedSaveAndNoNewEvent_StoresPausedPositionAfterOneInterval()
    {
        var (rig, time) = NewRig();
        using (rig)
        {
            await PauseWithFailingSaveAsync(rig, time, 12_345);

            time.Advance(Interval);
            await rig.Store.WaitForSavesAsync(1);

            var saved = Assert.Single(rig.Store.Saves);
            Assert.Equal(A, saved.Id);
            Assert.Equal(12_345, saved.PositionMs);
        }
    }

    [Fact]
    public async Task Retry_SaveKeepsFailing_AttemptsGrowAboutOnePerInterval()
    {
        const int intervals = 5;
        var (rig, time) = NewRig();
        using (rig)
        {
            await rig.TrackAsync(A);
            rig.Store.FailNextSaves(int.MaxValue);
            rig.Publish(PlayerStatus.Paused, A, 12_345, 600_000);

            for (var i = 1; i <= intervals; i++)
            {
                await time.WaitForTimersAsync(i);
                time.Advance(Interval);
            }

            await time.WaitForTimersAsync(intervals + 1); // the retry after the last interval failed too

            Assert.InRange(rig.Store.SaveCalls, intervals, intervals + 2);
            Assert.Empty(rig.Store.Saves);
        }
    }

    [Fact]
    public async Task Retry_SuccessfulRetryThenMoreIntervalsWithNoChange_SavesNothingMore()
    {
        var (rig, time) = NewRig();
        using (rig)
        {
            await PauseWithFailingSaveAsync(rig, time, 12_345);
            time.Advance(Interval);
            await rig.Store.WaitForSavesAsync(1);

            time.Advance(Interval);
            time.Advance(Interval);
            time.Advance(Interval);
            await rig.FlushAsync();

            Assert.Equal(2, rig.Store.SaveCalls);
            Assert.Single(rig.Store.Saves);
        }
    }

    // ---- FlushAsync ----

    [Fact]
    public async Task FlushAsync_SaveInFlightFailsWhileWaiting_RetriesOnceAndStoresTheState()
    {
        var (rig, _) = NewRig();
        using (rig)
        {
            await rig.TrackAsync(A);
            var gate = rig.Store.HoldNextSave();
            rig.Store.FailNextSaves(1);
            rig.Publish(PlayerStatus.Paused, A, 7000, 600_000);
            await gate.Entered;

            var flush = rig.FlushAsync();
            Assert.False(flush.IsCompleted);
            gate.Release();
            await flush;

            var saved = Assert.Single(rig.Store.Saves);
            Assert.Equal(7000, saved.PositionMs);
            Assert.Equal(2, rig.Store.SaveCalls);
        }
    }

    [Fact]
    public async Task FlushAsync_EveryAttemptFails_CompletesWithoutThrowingAndStoresNothing()
    {
        var (rig, time) = NewRig();
        using (rig)
        {
            await rig.TrackAsync(A);
            rig.Store.FailNextSaves(int.MaxValue);
            rig.Publish(PlayerStatus.Paused, A, 7000, 600_000);
            await time.WaitForTimersAsync(1);

            await rig.FlushAsync();

            Assert.Empty(rig.Store.Saves);
            Assert.Null(rig.Store.Stored(A));
        }
    }

    [Fact]
    public async Task FlushAsync_AfterFlushFailedEverywhere_NextFlushStoresTheLatestState()
    {
        var (rig, time) = NewRig();
        using (rig)
        {
            await rig.TrackAsync(A);
            rig.Store.FailNextSaves(int.MaxValue);
            rig.Publish(PlayerStatus.Paused, A, 7000, 600_000);
            await time.WaitForTimersAsync(1);
            await rig.FlushAsync();
            rig.Store.FailNextSaves(0);

            rig.Publish(PlayerStatus.Playing, A, 8000, 600_000);
            await rig.FlushAsync();

            Assert.Equal(8000, rig.Store.Stored(A)?.PositionMs);
        }
    }

    [Fact]
    public async Task Retry_AfterFlushFailedEverywhere_TimerStoresTheLatestState()
    {
        var (rig, time) = NewRig();
        using (rig)
        {
            await rig.TrackAsync(A);
            rig.Store.FailNextSaves(int.MaxValue);
            rig.Publish(PlayerStatus.Paused, A, 7000, 600_000);
            await time.WaitForTimersAsync(1);
            await rig.FlushAsync();
            rig.Store.FailNextSaves(0);

            time.Advance(Interval);
            await rig.Store.WaitForSavesAsync(1);

            Assert.Equal(7000, rig.Store.Stored(A)?.PositionMs);
        }
    }

    // ---- file no longer tracked ----

    [Fact]
    public async Task Retry_FileNoLongerTrackedWhenItsSaveFails_StoresItsLastPositionAfterOneInterval()
    {
        var (rig, time) = NewRig();
        using (rig)
        {
            await rig.TrackAsync(A);
            var gate = rig.Store.HoldNextSave();
            rig.Store.FailNextSaves(1);
            rig.Publish(PlayerStatus.Paused, A, 5000, 600_000);
            await gate.Entered;
            rig.Publish(PlayerStatus.Opening, B, 0, 0);
            gate.Release();
            await time.WaitForTimersAsync(1);

            time.Advance(Interval);
            await rig.Store.WaitForSavesAsync(1);

            Assert.Equal(5000, rig.Store.Stored(A)?.PositionMs);
        }
    }

    [Fact]
    public async Task Retry_FileNoLongerTrackedWhenItsSaveFails_DoesNotTouchTheNewFile()
    {
        var (rig, time) = NewRig();
        using (rig)
        {
            await rig.TrackAsync(A);
            var gate = rig.Store.HoldNextSave();
            rig.Store.FailNextSaves(1);
            rig.Publish(PlayerStatus.Paused, A, 5000, 600_000);
            await gate.Entered;
            rig.Publish(PlayerStatus.Opening, B, 0, 0);
            gate.Release();
            await time.WaitForTimersAsync(1);
            time.Advance(Interval);
            await rig.Store.WaitForSavesAsync(1);

            await rig.FlushAsync();

            Assert.Empty(rig.Store.SavesFor(B));
            Assert.Null(rig.Store.Stored(B));
        }
    }

    // ---- superseded retry ----

    [Fact]
    public async Task Retry_FileReopenedAfterItsSaveFailed_NeverWritesTheOldPositionAgain()
    {
        var (rig, time) = NewRig();
        using (rig)
        {
            await rig.TrackAsync(A);
            var gate = rig.Store.HoldNextSave();
            rig.Store.FailNextSaves(1);
            rig.Publish(PlayerStatus.Paused, A, 5000, 600_000); // this save fails
            await gate.Entered;
            rig.Publish(PlayerStatus.Opening, B, 0, 0);
            rig.Publish(PlayerStatus.Opening, A, 0, 0); // a new session for A
            rig.Publish(PlayerStatus.Paused, A, 9000, 600_000);
            gate.Release();
            await rig.Store.WaitForSavesAsync(1);

            time.Advance(Interval);
            time.Advance(Interval);
            time.Advance(Interval);
            await rig.FlushAsync();

            Assert.Equal(9000, rig.Store.Stored(A)?.PositionMs);
            Assert.DoesNotContain(rig.Store.SavesFor(A), s => s.PositionMs == 5000);
        }
    }
}
