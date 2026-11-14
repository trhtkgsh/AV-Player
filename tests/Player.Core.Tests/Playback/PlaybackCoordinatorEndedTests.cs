using Player.Core.Models;
using static Player.Core.Tests.Playback.CoordinatorRig;

namespace Player.Core.Tests.Playback;

public sealed class PlaybackCoordinatorEndedTests : IDisposable
{
    private readonly CoordinatorRig _r = new();

    public void Dispose() => _r.Dispose();

    private async Task OpenAtAsync(int index, RepeatMode repeat)
    {
        _r.Defaults.Value = new GlobalDefaults { Repeat = repeat };
        await _r.OpenAsync(index);
    }

    private PlayerSnapshot Ended(int index) => Snap(PlayerStatus.Ended, _r.Queue[index], 60_000, 60_000);

    [Fact]
    public async Task EndedSnapshot_RepeatFile_SeeksToZeroThenPlaysAndKeepsCurrent()
    {
        await OpenAtAsync(1, RepeatMode.File);

        _r.Engine.Raise(Ended(1));
        await WaitUntilAsync(() => _r.Engine.Count("Play") == 1, "the restart");

        Assert.Equal(new[] { "Open:v2", "Seek:0", "Play" }, _r.Engine.Calls);
        Assert.Equal(_r.Queue[1], _r.Coordinator.Current);
    }

    [Fact]
    public async Task EndedSnapshot_RepeatFolder_OpensNextItem()
    {
        await OpenAtAsync(0, RepeatMode.Folder);

        _r.Engine.Raise(Ended(0));
        await WaitUntilAsync(() => _r.Coordinator.Current == _r.Queue[1], "the next item to open");

        Assert.Equal(new[] { "Open:v1", "Open:v2" }, _r.Engine.Calls);
    }

    [Fact]
    public async Task EndedSnapshot_RepeatFolderOnLastItem_WrapsToFirstItem()
    {
        await OpenAtAsync(2, RepeatMode.Folder);

        _r.Engine.Raise(Ended(2));
        await WaitUntilAsync(() => _r.Coordinator.Current == _r.Queue[0], "the first item to open");

        Assert.Equal(new[] { "Open:v3", "Open:v1" }, _r.Engine.Calls);
    }

    [Fact]
    public async Task EndedSnapshot_RepeatNone_OpensNextItem()
    {
        await OpenAtAsync(0, RepeatMode.None);

        _r.Engine.Raise(Ended(0));
        await WaitUntilAsync(() => _r.Coordinator.Current == _r.Queue[1], "the next item to open");

        Assert.Equal(new[] { "Open:v1", "Open:v2" }, _r.Engine.Calls);
    }

    [Fact]
    public async Task EndedSnapshot_RepeatNoneOnLastItem_OpensNothing()
    {
        await OpenAtAsync(2, RepeatMode.None);
        var reads = _r.Engine.SnapshotReads;

        _r.Engine.Raise(Ended(2));
        await _r.WaitForHandlerAsync(reads);

        Assert.Equal(new[] { "Open:v3" }, _r.Engine.Calls);
        Assert.Equal(_r.Queue[2], _r.Coordinator.Current);
    }

    [Fact]
    public async Task EndedSnapshot_SeveralInARow_ReactOnce()
    {
        await OpenAtAsync(0, RepeatMode.File);

        _r.Engine.Raise(Ended(0));
        _r.Engine.Raise(Ended(0));
        _r.Engine.Raise(Ended(0));
        await WaitUntilAsync(() => _r.Engine.Count("Play") >= 1, "the restart");
        await _r.BarrierAsync();

        Assert.Equal(new[] { "Open:v1", "Seek:0", "Play" }, _r.Engine.Calls);
    }

    [Fact]
    public async Task EndedSnapshot_AfterOtherStatusForSameItem_IsHandledAgain()
    {
        await OpenAtAsync(0, RepeatMode.File);

        _r.Engine.Raise(Ended(0));
        await WaitUntilAsync(() => _r.Engine.Count("Play") == 1, "the first restart");
        _r.Engine.Raise(Snap(PlayerStatus.Playing, _r.Queue[0], 100, 60_000));
        _r.Engine.Raise(Ended(0));
        await WaitUntilAsync(() => _r.Engine.Count("Play") == 2, "the second restart");
        await _r.BarrierAsync();

        Assert.Equal(new[] { "Open:v1", "Seek:0", "Play", "Seek:0", "Play" }, _r.Engine.Calls);
    }

    [Fact]
    public async Task EndedSnapshot_EngineNoLongerEnded_DoesNothing()
    {
        await OpenAtAsync(0, RepeatMode.File);
        var reads = _r.Engine.SnapshotReads;
        _r.Engine.Snapshot = Snap(PlayerStatus.Playing, _r.Queue[0], 5_000, 60_000);

        _r.Engine.RaiseOnly(Ended(0));
        await _r.WaitForHandlerAsync(reads);

        Assert.Equal(new[] { "Open:v1" }, _r.Engine.Calls);
    }

    [Fact]
    public async Task EndedSnapshot_EngineSnapshotOfOtherItem_DoesNothing()
    {
        await OpenAtAsync(0, RepeatMode.File);
        var reads = _r.Engine.SnapshotReads;
        _r.Engine.Snapshot = Ended(1);

        _r.Engine.RaiseOnly(Ended(0));
        await _r.WaitForHandlerAsync(reads);

        Assert.Equal(new[] { "Open:v1" }, _r.Engine.Calls);
        Assert.Equal(_r.Queue[0], _r.Coordinator.Current);
    }

    [Fact]
    public async Task EndedSnapshot_NextItemFailsToOpen_CurrentIsUnchanged()
    {
        await OpenAtAsync(0, RepeatMode.None);
        _r.Engine.OpenHook = _ => throw new InvalidOperationException("engine");

        _r.Engine.Raise(Ended(0));
        await WaitUntilAsync(() => _r.Engine.Count("Open:v2") == 1, "the failing open");
        await _r.BarrierAsync();

        Assert.Equal(_r.Queue[0], _r.Coordinator.Current);
    }

    [Fact]
    public async Task EndedSnapshot_AfterFailedReaction_IsHandledAgain()
    {
        await OpenAtAsync(0, RepeatMode.None);
        var attempts = 0;
        _r.Engine.OpenHook = item =>
            Interlocked.Increment(ref attempts) == 1 ? throw new InvalidOperationException("engine") : Task.CompletedTask;

        // The failed reaction re-arms after the gate is free, so keep raising until the retry happened.
        await WaitUntilAsync(
            () =>
            {
                if (_r.Coordinator.Current == _r.Queue[1])
                {
                    return true;
                }

                _r.Engine.Raise(Ended(0));
                return false;
            },
            "the second Ended snapshot to be handled again");

        Assert.Equal(new[] { "Open:v1", "Open:v2", "Open:v2" }, _r.Engine.Calls);
    }

    [Fact]
    public async Task NextAsync_WhileAutomaticNextForSameEndedFileRuns_DoesNotSkipAnItem()
    {
        await OpenAtAsync(0, RepeatMode.None);
        var hold = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _r.Engine.OpenHook = item => item == _r.Queue[1] ? hold.Task : Task.CompletedTask;

        _r.Engine.Raise(Ended(0));
        await WaitUntilAsync(() => _r.Engine.Count("Open:v2") == 1, "the automatic next to reach the engine");
        var next = _r.Coordinator.NextAsync(CancellationToken.None);
        hold.SetResult();
        await next;

        Assert.Equal(_r.Queue[1], _r.Coordinator.Current);
        Assert.Equal(new[] { "Open:v1", "Open:v2" }, _r.Engine.Calls);
    }
}
