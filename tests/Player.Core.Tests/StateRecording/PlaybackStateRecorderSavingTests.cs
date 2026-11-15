using Player.Core.Models;

namespace Player.Core.Tests.StateRecording;

public sealed class PlaybackStateRecorderSavingTests
{
    private static readonly MediaFileId A = TestData.Id(1);
    private static readonly TimeSpan Step = TimeSpan.FromMilliseconds(250);

    // ---- tracking ----

    [Fact]
    public async Task FlushAsync_NothingTracked_WritesNothingAndCompletes()
    {
        using var rig = new RecorderRig();

        await rig.FlushAsync();

        Assert.Empty(rig.Store.Saves);
        Assert.Equal(0, rig.Store.GetCalls);
        Assert.Equal(0, rig.Store.SaveCalls);
    }

    [Fact]
    public async Task SnapshotChanged_NothingOpen_SavesNothing()
    {
        using var rig = new RecorderRig();

        rig.PublishNothingOpen();
        rig.Lifecycle.RaiseBackgrounded();
        await rig.FlushAsync();

        Assert.Equal(0, rig.Store.SaveCalls);
        Assert.Equal(0, rig.Store.GetCalls);
    }

    [Theory]
    [InlineData(PlayerStatus.Opening)]
    [InlineData(PlayerStatus.Playing)]
    public async Task SnapshotChanged_OpeningOrPlaying_UpdatesPositionAndDuration(PlayerStatus status)
    {
        using var rig = new RecorderRig();

        rig.Publish(status, A, 1000, 600_000);
        await rig.FlushAsync();

        var saved = Assert.Single(rig.Store.Saves);
        Assert.Equal(A, saved.Id);
        Assert.Equal(1000, saved.PositionMs);
        Assert.Equal(600_000, saved.DurationMs);
    }

    [Fact]
    public async Task SnapshotChanged_DurationReportedAsZero_KeepsKnownDuration()
    {
        using var rig = new RecorderRig();

        rig.Publish(PlayerStatus.Playing, A, 1000, 600_000);
        rig.Publish(PlayerStatus.Opening, A, 1000, 0);
        await rig.FlushAsync();

        var saved = Assert.Single(rig.Store.Saves);
        Assert.Equal(600_000, saved.DurationMs);
    }

    [Theory]
    [InlineData(570_000, 600_000, false)]
    [InlineData(570_001, 600_000, true)]
    [InlineData(1000, 0, false)]
    public async Task SnapshotChanged_PositionNearEnd_SetsFinishedAbove95Percent(long positionMs, long durationMs, bool expected)
    {
        using var rig = new RecorderRig();

        rig.Publish(PlayerStatus.Playing, A, positionMs, durationMs);
        await rig.FlushAsync();

        var saved = Assert.Single(rig.Store.Saves);
        Assert.Equal(expected, saved.Finished);
    }

    [Fact]
    public async Task SnapshotChanged_IdleStatus_DoesNotChangePositionOrDuration()
    {
        using var rig = new RecorderRig();

        rig.Publish(PlayerStatus.Playing, A, 1000, 600_000);
        rig.Publish(PlayerStatus.Idle, A, 5, 7);
        await rig.Store.WaitForSavesAsync(1);

        var saved = Assert.Single(rig.Store.Saves);
        Assert.Equal(1000, saved.PositionMs);
        Assert.Equal(600_000, saved.DurationMs);
    }

    [Fact]
    public async Task SnapshotChanged_ErrorStatus_DoesNotChangePositionOrDuration()
    {
        using var rig = new RecorderRig();

        rig.Publish(PlayerStatus.Playing, A, 1000, 600_000);
        rig.Publish(PlayerStatus.Error, A, 5, 7);
        await rig.Store.WaitForSavesAsync(1);

        var saved = Assert.Single(rig.Store.Saves);
        Assert.Equal(1000, saved.PositionMs);
        Assert.Equal(600_000, saved.DurationMs);
    }

    // ---- debounce ----

    private static void PublishNow(RecorderRig rig) =>
        rig.Publish(PlayerStatus.Playing, A, (long)(rig.Time.GetUtcNow() - RecorderRig.Start).TotalMilliseconds, 600_000);

    /// <summary>Advances 250 ms of fake time and publishes the position, <paramref name="steps"/> times.</summary>
    private static void PlayFor(RecorderRig rig, int steps)
    {
        for (var i = 0; i < steps; i++)
        {
            rig.Time.Advance(Step);
            PublishNow(rig);
        }
    }

    [Fact]
    public async Task Debounce_ChangesFor4900ms_SavesNothingYet()
    {
        using var rig = new RecorderRig();
        await rig.TrackAsync(A);

        PublishNow(rig);
        PlayFor(rig, 19);
        rig.Time.Advance(TimeSpan.FromMilliseconds(150));

        Assert.Equal(0, rig.Store.SaveCalls);
    }

    [Fact]
    public async Task Debounce_ClockPasses5sAfterFirstChange_SavesOnce()
    {
        using var rig = new RecorderRig();
        await rig.TrackAsync(A);

        PublishNow(rig);
        PlayFor(rig, 19);
        rig.Time.Advance(TimeSpan.FromMilliseconds(350));
        await rig.Store.WaitForSavesAsync(1);

        var saved = Assert.Single(rig.Store.Saves);
        Assert.Equal(A, saved.Id);
        Assert.InRange(saved.PositionMs, 4750, 5100);
    }

    [Fact]
    public async Task Debounce_ChangeAfterASave_SavesAgain5sAfterThatChange()
    {
        using var rig = new RecorderRig();
        await rig.TrackAsync(A);
        PublishNow(rig);
        PlayFor(rig, 19);
        rig.Time.Advance(TimeSpan.FromMilliseconds(350)); // 5.1 s: first save
        await rig.Store.WaitForSavesAsync(1);

        PublishNow(rig); // first unsaved change, at 5.1 s
        PlayFor(rig, 20); // 10.1 s: the second save is due
        await rig.Store.WaitForSavesAsync(2);
        rig.Time.Advance(TimeSpan.FromMilliseconds(400)); // 10.5 s

        Assert.Equal(2, rig.Store.Saves.Count);
    }

    [Fact]
    public async Task Debounce_SameSnapshotTwice_SavesExactlyOnce()
    {
        using var rig = new RecorderRig();
        await rig.TrackAsync(A);

        rig.Publish(PlayerStatus.Playing, A, 1000, 600_000);
        rig.Publish(PlayerStatus.Playing, A, 1000, 600_000);
        rig.Time.Advance(TimeSpan.FromSeconds(10));
        await rig.Store.WaitForSavesAsync(1);
        await rig.FlushAsync();

        Assert.Single(rig.Store.Saves);
    }

    // ---- immediate saves ----

    [Fact]
    public async Task SnapshotChanged_StatusBecomesPaused_SavesWithoutAdvancingTheClock()
    {
        using var rig = new RecorderRig();

        rig.Publish(PlayerStatus.Playing, A, 1000, 600_000);
        rig.Publish(PlayerStatus.Paused, A, 2000, 600_000);
        await rig.Store.WaitForSavesAsync(1);

        var saved = Assert.Single(rig.Store.Saves);
        Assert.Equal(2000, saved.PositionMs);
    }

    [Fact]
    public async Task SnapshotChanged_StatusBecomesEnded_SavesFinishedAtTheEnd()
    {
        using var rig = new RecorderRig();

        rig.Publish(PlayerStatus.Playing, A, 500_000, 600_000);
        rig.Publish(PlayerStatus.Ended, A, 600_000, 600_000);
        await rig.Store.WaitForSavesAsync(1);

        var saved = Assert.Single(rig.Store.Saves);
        Assert.Equal(600_000, saved.PositionMs);
        Assert.True(saved.Finished);
    }

    [Fact]
    public async Task SnapshotChanged_StatusBecomesError_SavesLastKnownPosition()
    {
        using var rig = new RecorderRig();

        rig.Publish(PlayerStatus.Playing, A, 1000, 600_000);
        rig.Publish(PlayerStatus.Error, A, 0, 0);
        await rig.Store.WaitForSavesAsync(1);

        var saved = Assert.Single(rig.Store.Saves);
        Assert.Equal(1000, saved.PositionMs);
    }

    [Fact]
    public async Task SnapshotChanged_StatusBecomesIdle_SavesLastKnownPosition()
    {
        using var rig = new RecorderRig();

        rig.Publish(PlayerStatus.Playing, A, 1000, 600_000);
        rig.Publish(PlayerStatus.Idle, A, 0, 0);
        await rig.Store.WaitForSavesAsync(1);

        var saved = Assert.Single(rig.Store.Saves);
        Assert.Equal(1000, saved.PositionMs);
    }

    [Fact]
    public async Task SnapshotChanged_CurrentBecomesNull_SavesLastPositionOfTheFileNotZero()
    {
        using var rig = new RecorderRig();

        rig.Publish(PlayerStatus.Playing, A, 1000, 600_000);
        rig.PublishNothingOpen();
        await rig.Store.WaitForSavesAsync(1);

        var saved = Assert.Single(rig.Store.Saves);
        Assert.Equal(A, saved.Id);
        Assert.Equal(1000, saved.PositionMs);
    }

    [Fact]
    public async Task Backgrounded_FileIsDirty_SavesWithoutAdvancingTheClock()
    {
        using var rig = new RecorderRig();

        rig.Publish(PlayerStatus.Playing, A, 1000, 600_000);
        rig.Lifecycle.RaiseBackgrounded();
        await rig.Store.WaitForSavesAsync(1);

        var saved = Assert.Single(rig.Store.Saves);
        Assert.Equal(1000, saved.PositionMs);
    }

    [Fact]
    public async Task Stopping_FileIsDirty_SavesWithoutAdvancingTheClock()
    {
        using var rig = new RecorderRig();

        rig.Publish(PlayerStatus.Playing, A, 1000, 600_000);
        rig.Lifecycle.RaiseStopping();
        await rig.Store.WaitForSavesAsync(1);

        var saved = Assert.Single(rig.Store.Saves);
        Assert.Equal(1000, saved.PositionMs);
    }

    [Fact]
    public async Task Save_EverySave_StampsLastPlayedWithTheFakeClock()
    {
        using var rig = new RecorderRig();
        await rig.TrackAsync(A);
        rig.Publish(PlayerStatus.Playing, A, 1000, 600_000);
        rig.Time.Advance(TimeSpan.FromSeconds(2));
        var firstExpected = rig.NowUnixSeconds;
        rig.Publish(PlayerStatus.Paused, A, 2000, 600_000);
        await rig.Store.WaitForSavesAsync(1);

        rig.Time.Advance(TimeSpan.FromSeconds(100));
        var secondExpected = rig.NowUnixSeconds;
        rig.Publish(PlayerStatus.Playing, A, 3000, 600_000);
        rig.Publish(PlayerStatus.Paused, A, 4000, 600_000);
        await rig.Store.WaitForSavesAsync(2);

        var saves = rig.Store.Saves;
        Assert.Equal(firstExpected, saves[0].LastPlayedUtc);
        Assert.Equal(secondExpected, saves[1].LastPlayedUtc);
        Assert.NotEqual(saves[0].LastPlayedUtc, saves[1].LastPlayedUtc);
    }

    [Fact]
    public async Task Backgrounded_StateAlreadySavedByPause_DoesNotSaveAgain()
    {
        using var rig = new RecorderRig();
        rig.Publish(PlayerStatus.Playing, A, 1000, 600_000);
        rig.Publish(PlayerStatus.Paused, A, 2000, 600_000);
        await rig.Store.WaitForSavesAsync(1);

        rig.Lifecycle.RaiseBackgrounded();
        await rig.FlushAsync();

        Assert.Single(rig.Store.Saves);
    }
}
