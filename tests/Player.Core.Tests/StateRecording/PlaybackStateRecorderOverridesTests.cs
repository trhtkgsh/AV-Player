using Player.Core.Models;

namespace Player.Core.Tests.StateRecording;

public sealed class PlaybackStateRecorderOverridesTests
{
    private static readonly MediaFileId A = TestData.Id(1);
    private static readonly MediaFileId B = TestData.Id(2);

    private static PlaybackState SavedA() => new(A)
    {
        PositionMs = 60_000,
        DurationMs = 500_000,
        Speed = 1.5f,
        SubtitleTrackId = -1,
        ExternalSubtitleUri = "content://subs/a.srt",
    };

    // ---- loading and overrides ----

    [Fact]
    public async Task SnapshotChanged_FileHasSavedState_KeepsSavedOverrides()
    {
        using var rig = new RecorderRig();
        rig.Store.Preload(SavedA());

        rig.Publish(PlayerStatus.Opening, A, 60_000, 0);
        rig.Publish(PlayerStatus.Playing, A, 60_250, 600_000);
        await rig.FlushAsync();

        var saved = Assert.Single(rig.Store.Saves);
        Assert.Equal(1.5f, saved.Speed);
        Assert.Equal(-1, saved.SubtitleTrackId);
        Assert.Equal("content://subs/a.srt", saved.ExternalSubtitleUri);
    }

    [Fact]
    public async Task SnapshotChanged_FileHasSavedState_WritesNewPositionAndDuration()
    {
        using var rig = new RecorderRig();
        rig.Store.Preload(SavedA());

        rig.Publish(PlayerStatus.Opening, A, 60_000, 0);
        rig.Publish(PlayerStatus.Playing, A, 60_250, 600_000);
        await rig.FlushAsync();

        var saved = Assert.Single(rig.Store.Saves);
        Assert.Equal(60_250, saved.PositionMs);
        Assert.Equal(600_000, saved.DurationMs);
    }

    [Fact]
    public async Task Update_WhileLoadIsHeld_NothingIsSavedUntilTheLoadFinishes()
    {
        using var rig = new RecorderRig();
        rig.Store.Preload(SavedA());
        var gate = rig.Store.HoldNextGet();

        rig.Publish(PlayerStatus.Opening, A, 60_000, 0);
        rig.Publish(PlayerStatus.Playing, A, 60_250, 600_000);
        rig.Recorder.Update(s => s with { AudioDelayMs = 150 });
        await gate.Entered;

        Assert.Equal(0, rig.Store.SaveCalls);
        gate.Release();
        await rig.FlushAsync();
    }

    [Fact]
    public async Task Update_WhileLoadIsHeld_SavesStoredOverridesUpdateAndLatestPosition()
    {
        using var rig = new RecorderRig();
        rig.Store.Preload(SavedA());
        var gate = rig.Store.HoldNextGet();
        rig.Publish(PlayerStatus.Opening, A, 60_000, 0);
        rig.Publish(PlayerStatus.Playing, A, 60_250, 600_000);
        rig.Recorder.Update(s => s with { AudioDelayMs = 150 });
        await gate.Entered;

        gate.Release();
        await rig.FlushAsync();

        var saved = Assert.Single(rig.Store.Saves);
        Assert.Equal(1.5f, saved.Speed);
        Assert.Equal(-1, saved.SubtitleTrackId);
        Assert.Equal("content://subs/a.srt", saved.ExternalSubtitleUri);
        Assert.Equal(150, saved.AudioDelayMs);
        Assert.Equal(60_250, saved.PositionMs);
        Assert.Equal(600_000, saved.DurationMs);
    }

    // ---- switching files ----

    private static async Task<RecorderRig> SwitchFromAToBAsync()
    {
        var rig = new RecorderRig();
        rig.Store.Preload(new PlaybackState(A) { Speed = 1.5f });
        rig.Store.Preload(new PlaybackState(B) { AudioDelayMs = 99 });
        rig.Publish(PlayerStatus.Playing, A, 1000, 600_000);
        rig.Publish(PlayerStatus.Playing, B, 5000, 300_000);
        await rig.FlushAsync();
        return rig;
    }

    [Fact]
    public async Task SnapshotChanged_SwitchFromAToB_SavesAWithItsLastPositionBeforeB()
    {
        using var rig = await SwitchFromAToBAsync();

        var saves = rig.Store.Saves;
        Assert.Equal(2, saves.Count);
        Assert.Equal(A, saves[0].Id);
        Assert.Equal(1000, saves[0].PositionMs);
        Assert.Equal(B, saves[1].Id);
        Assert.Equal(5000, saves[1].PositionMs);
    }

    [Fact]
    public async Task SnapshotChanged_SwitchFromAToB_DoesNotWriteAOverridesIntoB()
    {
        using var rig = await SwitchFromAToBAsync();

        var savedB = Assert.Single(rig.Store.SavesFor(B));
        Assert.Null(savedB.Speed);
        Assert.Equal(99, savedB.AudioDelayMs);
    }

    [Fact]
    public async Task SnapshotChanged_SwitchFromAToB_DoesNotWriteBOverridesIntoA()
    {
        using var rig = await SwitchFromAToBAsync();

        var savedA = Assert.Single(rig.Store.SavesFor(A));
        Assert.Equal(1.5f, savedA.Speed);
        Assert.Null(savedA.AudioDelayMs);
    }

    // ---- Update ----

    [Fact]
    public async Task Update_FileTracked_SavesImmediately()
    {
        using var rig = new RecorderRig();
        await rig.TrackAsync(A);

        rig.Recorder.Update(s => s with { Speed = 2f });
        await rig.Store.WaitForSavesAsync(1);

        var saved = Assert.Single(rig.Store.Saves);
        Assert.Equal(2f, saved.Speed);
    }

    [Theory]
    [InlineData("Id")]
    [InlineData("PositionMs")]
    [InlineData("DurationMs")]
    [InlineData("Finished")]
    [InlineData("LastPlayedUtc")]
    public async Task Update_ChangeAlsoReturnsOtherField_FieldIsNotAffected(string field)
    {
        using var rig = new RecorderRig();
        await rig.TrackAsync(A);
        rig.Publish(PlayerStatus.Playing, A, 1000, 600_000);

        rig.Recorder.Update(s => Tamper(field, s) with { Speed = 2f });
        await rig.FlushAsync();

        var saved = rig.Store.Saves[^1];
        Assert.Equal(2f, saved.Speed);
        switch (field)
        {
            case "Id":
                Assert.Equal(A, saved.Id);
                Assert.Empty(rig.Store.SavesFor(B));
                break;
            case "PositionMs":
                Assert.Equal(1000, saved.PositionMs);
                break;
            case "DurationMs":
                Assert.Equal(600_000, saved.DurationMs);
                break;
            case "Finished":
                Assert.False(saved.Finished);
                break;
            case "LastPlayedUtc":
                Assert.Equal(rig.NowUnixSeconds, saved.LastPlayedUtc);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(field));
        }
    }

    private static PlaybackState Tamper(string field, PlaybackState state) => field switch
    {
        "Id" => state with { Id = B },
        "PositionMs" => state with { PositionMs = 9 },
        "DurationMs" => state with { DurationMs = 9 },
        "Finished" => state with { Finished = true },
        "LastPlayedUtc" => state with { LastPlayedUtc = 12_345 },
        _ => throw new ArgumentOutOfRangeException(nameof(field)),
    };

    [Fact]
    public async Task Update_NoFileTracked_DoesNothingAndDoesNotThrow()
    {
        using var rig = new RecorderRig();

        var exception = Record.Exception(() => rig.Recorder.Update(s => s with { Speed = 2f }));
        await rig.FlushAsync();

        Assert.Null(exception);
        Assert.Equal(0, rig.Store.SaveCalls);
    }

    [Fact]
    public async Task Update_ChangeThrows_ExceptionPropagatesToTheCaller()
    {
        using var rig = new RecorderRig();
        await rig.TrackAsync(A);

        Assert.Throws<InvalidOperationException>(
            () => rig.Recorder.Update(_ => throw new InvalidOperationException("boom")));
    }

    [Fact]
    public async Task Update_ChangeThrows_LeavesStateUnchanged()
    {
        using var rig = new RecorderRig();
        await rig.TrackAsync(A);
        Assert.Throws<InvalidOperationException>(
            () => rig.Recorder.Update(_ => throw new InvalidOperationException("boom")));

        await rig.FlushAsync();

        Assert.Equal(0, rig.Store.SaveCalls);
    }

    // ---- failed saves ----

    private static async Task<RecorderRig> FailingPausedSaveAsync()
    {
        var rig = new RecorderRig();
        await rig.TrackAsync(A);
        rig.Publish(PlayerStatus.Playing, A, 1000, 600_000);
        rig.Store.FailNextSaves(1);
        rig.Publish(PlayerStatus.Paused, A, 2000, 600_000);
        await rig.Store.WaitForSaveCallsAsync(1);
        return rig;
    }

    [Fact]
    public async Task FlushAsync_AfterFailedSave_WritesLatestState()
    {
        using var rig = new RecorderRig();
        await rig.TrackAsync(A);
        rig.Publish(PlayerStatus.Playing, A, 1000, 600_000);
        rig.Store.FailNextSaves(1);
        rig.Publish(PlayerStatus.Paused, A, 2000, 600_000);

        // The first flush may overlap the failing write and then not retry it. The second one runs after
        // the failure was handled, so it retries for sure.
        await rig.FlushAsync();
        await rig.FlushAsync();

        var saved = Assert.Single(rig.Store.Saves);
        Assert.Equal(2000, saved.PositionMs);
    }

    [Fact]
    public async Task SnapshotChanged_PausedAfterFailedSave_WritesLatestState()
    {
        using var rig = await FailingPausedSaveAsync();

        rig.Publish(PlayerStatus.Playing, A, 3000, 600_000);
        rig.Publish(PlayerStatus.Paused, A, 4000, 600_000);
        await rig.FlushAsync();

        var saved = Assert.Single(rig.Store.Saves);
        Assert.Equal(4000, saved.PositionMs);
    }

    [Fact]
    public async Task Backgrounded_AfterFailedSave_WritesLatestState()
    {
        using var rig = await FailingPausedSaveAsync();

        rig.Publish(PlayerStatus.Playing, A, 3000, 600_000);
        rig.Lifecycle.RaiseBackgrounded();
        await rig.FlushAsync();

        var saved = Assert.Single(rig.Store.Saves);
        Assert.Equal(3000, saved.PositionMs);
    }

    [Fact]
    public async Task SnapshotChanged_SaveFails_DoesNotThrowOutOfTheEngineEvent()
    {
        using var rig = new RecorderRig();
        await rig.TrackAsync(A);
        rig.Publish(PlayerStatus.Playing, A, 1000, 600_000);
        rig.Store.FailNextSaves(10);

        var exception = Record.Exception(() => rig.Publish(PlayerStatus.Paused, A, 2000, 600_000));
        await rig.FlushAsync();

        Assert.Null(exception);
    }

    [Fact]
    public async Task Backgrounded_SaveFails_DoesNotThrowOutOfTheLifecycleEvent()
    {
        using var rig = new RecorderRig();
        await rig.TrackAsync(A);
        rig.Publish(PlayerStatus.Playing, A, 1000, 600_000);
        rig.Store.FailNextSaves(10);

        var exception = Record.Exception(() => rig.Lifecycle.RaiseBackgrounded());
        await rig.FlushAsync();

        Assert.Null(exception);
    }

    [Fact]
    public async Task Update_SaveFails_DoesNotThrow()
    {
        using var rig = new RecorderRig();
        await rig.TrackAsync(A);
        rig.Store.FailNextSaves(10);

        var exception = Record.Exception(() => rig.Recorder.Update(s => s with { Speed = 2f }));
        await rig.FlushAsync();

        Assert.Null(exception);
    }

    [Fact]
    public async Task FlushAsync_SaveFails_DoesNotThrow()
    {
        using var rig = new RecorderRig();
        await rig.TrackAsync(A);
        rig.Publish(PlayerStatus.Playing, A, 1000, 600_000);
        rig.Store.FailNextSaves(10);

        var exception = await Record.ExceptionAsync(rig.FlushAsync);

        Assert.Null(exception);
    }

    // ---- failed loads ----

    [Fact]
    public async Task FlushAsync_LoadFailedOnce_RetriesTheLoadAndKeepsSavedOverrides()
    {
        using var rig = new RecorderRig();
        rig.Store.Preload(new PlaybackState(A) { Speed = 2f });
        rig.Store.FailNextGets(1);
        rig.Publish(PlayerStatus.Playing, A, 1000, 600_000);

        // The first flush may overlap the failing load and then not retry it. The second one runs after
        // the failure was handled, so it retries for sure.
        await rig.FlushAsync();
        await rig.FlushAsync();

        var saved = Assert.Single(rig.Store.Saves);
        Assert.Equal(2f, saved.Speed);
        Assert.Equal(1000, saved.PositionMs);
        Assert.Equal(600_000, saved.DurationMs);
    }

    [Fact]
    public async Task FlushAsync_LoadFailedOnce_StoredStateStillHasTheOverrides()
    {
        using var rig = new RecorderRig();
        rig.Store.Preload(new PlaybackState(A) { Speed = 2f });
        rig.Store.FailNextGets(1);
        rig.Publish(PlayerStatus.Playing, A, 1000, 600_000);

        await rig.FlushAsync();
        await rig.FlushAsync();

        var stored = rig.Store.Stored(A);
        Assert.NotNull(stored);
        Assert.Equal(2f, stored.Speed);
    }

    [Fact]
    public async Task SnapshotChanged_LoadKeepsFailing_WritesNothingForTheFile()
    {
        using var rig = new RecorderRig();
        rig.Store.Preload(new PlaybackState(A) { Speed = 2f });
        rig.Store.FailNextGets(1000);

        rig.Publish(PlayerStatus.Playing, A, 1000, 600_000);
        await rig.FlushAsync();
        rig.Publish(PlayerStatus.Paused, A, 2000, 600_000);
        await rig.FlushAsync();
        rig.Lifecycle.RaiseBackgrounded();
        rig.Recorder.Update(s => s with { AudioDelayMs = 5 });
        await rig.FlushAsync();

        Assert.Equal(0, rig.Store.SaveCalls);
        Assert.Equal(2f, rig.Store.Stored(A)!.Speed);
    }

    // ---- FlushAsync ----

    [Fact]
    public async Task FlushAsync_CancelledTokenWhileWorkIsPending_ThrowsOperationCanceled()
    {
        using var rig = new RecorderRig();
        var gate = rig.Store.HoldNextGet();
        rig.Publish(PlayerStatus.Playing, A, 1000, 600_000);
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => rig.Recorder.FlushAsync(cts.Token));

        gate.Release();
        await rig.FlushAsync();
    }

    [Fact]
    public async Task FlushAsync_SaveIsHeld_CompletesOnlyAfterTheSaveIsReleased()
    {
        using var rig = new RecorderRig();
        await rig.TrackAsync(A);
        rig.Publish(PlayerStatus.Playing, A, 1000, 600_000);
        var gate = rig.Store.HoldNextSave();

        var flush = rig.FlushAsync();
        await gate.Entered;
        Assert.False(flush.IsCompleted);
        gate.Release();
        await flush;

        var saved = Assert.Single(rig.Store.Saves);
        Assert.Equal(1000, saved.PositionMs);
    }

    [Fact]
    public async Task FlushAsync_CalledTwiceAtTheSameTime_BothCompleteAndTheStateIsSavedOnce()
    {
        using var rig = new RecorderRig();
        await rig.TrackAsync(A);
        rig.Publish(PlayerStatus.Playing, A, 1000, 600_000);

        var first = rig.FlushAsync();
        var second = rig.FlushAsync();
        await Task.WhenAll(first, second);

        Assert.Single(rig.Store.Saves);
    }

    // ---- Dispose ----

    private static async Task<RecorderRig> DirtyTrackedRigAsync()
    {
        var rig = new RecorderRig();
        await rig.TrackAsync(A);
        rig.Publish(PlayerStatus.Playing, A, 1000, 600_000);
        return rig;
    }

    [Fact]
    public async Task Dispose_Called_UnsubscribesFromEngineAndLifecycle()
    {
        using var rig = await DirtyTrackedRigAsync();
        Assert.Equal(1, rig.Engine.SubscriberCount);

        rig.Recorder.Dispose();

        Assert.Equal(0, rig.Engine.SubscriberCount);
        Assert.Equal(0, rig.Lifecycle.BackgroundedSubscribers);
        Assert.Equal(0, rig.Lifecycle.StoppingSubscribers);
    }

    [Fact]
    public async Task SnapshotChanged_AfterDispose_CausesNoStoreCall()
    {
        using var rig = await DirtyTrackedRigAsync();
        var gets = rig.Store.GetCalls;
        rig.Recorder.Dispose();

        rig.Publish(PlayerStatus.Paused, A, 2000, 600_000);
        rig.Publish(PlayerStatus.Playing, B, 100, 300_000);

        Assert.Equal(0, rig.Store.SaveCalls);
        Assert.Equal(gets, rig.Store.GetCalls);
    }

    [Fact]
    public async Task LifecycleEvents_AfterDispose_CauseNoSave()
    {
        using var rig = await DirtyTrackedRigAsync();
        rig.Recorder.Dispose();

        rig.Lifecycle.RaiseBackgrounded();
        rig.Lifecycle.RaiseStopping();

        Assert.Equal(0, rig.Store.SaveCalls);
    }

    [Fact]
    public async Task Update_AfterDispose_CausesNoSave()
    {
        using var rig = await DirtyTrackedRigAsync();
        rig.Recorder.Dispose();

        rig.Recorder.Update(s => s with { Speed = 2f });

        Assert.Equal(0, rig.Store.SaveCalls);
    }

    [Fact]
    public async Task Debounce_ClockPasses5sAfterDispose_CausesNoSave()
    {
        using var rig = await DirtyTrackedRigAsync();
        rig.Recorder.Dispose();

        rig.Time.Advance(TimeSpan.FromSeconds(10));

        Assert.Equal(0, rig.Store.SaveCalls);
    }

    [Fact]
    public async Task FlushAsync_AfterDispose_ReturnsACompletedTask()
    {
        using var rig = await DirtyTrackedRigAsync();
        rig.Recorder.Dispose();

        var flush = rig.Recorder.FlushAsync(CancellationToken.None);

        Assert.True(flush.IsCompletedSuccessfully);
        Assert.Equal(0, rig.Store.SaveCalls);
    }

    [Fact]
    public void Dispose_CalledTwice_DoesNotThrow()
    {
        using var rig = new RecorderRig();
        rig.Recorder.Dispose();

        var exception = Record.Exception(rig.Recorder.Dispose);

        Assert.Null(exception);
    }
}
