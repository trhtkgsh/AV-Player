using Player.Core.Models;
using static Player.Core.Tests.Playback.CoordinatorRig;

namespace Player.Core.Tests.Playback;

public sealed class PlaybackCoordinatorRepeatAndLifetimeTests : IDisposable
{
    private readonly CoordinatorRig _r = new();

    public void Dispose() => _r.Dispose();

    [Fact]
    public async Task SetRepeatAsync_Mode_SavesDefaultsWithRepeatUpdatesRepeatAndRaisesChanged()
    {
        _r.Defaults.Value = new GlobalDefaults { Speed = 2f };

        await _r.Coordinator.SetRepeatAsync(RepeatMode.Folder, CancellationToken.None);

        Assert.Equal(new GlobalDefaults { Speed = 2f, Repeat = RepeatMode.Folder }, Assert.Single(_r.Defaults.Saved));
        Assert.Equal(RepeatMode.Folder, _r.Coordinator.Repeat);
        Assert.Equal(1, _r.ChangedCount);
    }

    [Fact]
    public async Task SetRepeatAsync_SaveThrows_RepeatIsUnchangedAndNoChangedIsRaised()
    {
        _r.Defaults.SaveException = new InvalidOperationException("save");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _r.Coordinator.SetRepeatAsync(RepeatMode.File, CancellationToken.None));

        Assert.Equal(RepeatMode.None, _r.Coordinator.Repeat);
        Assert.Equal(0, _r.ChangedCount);
    }

    [Fact]
    public async Task OpenAsync_FirstUse_SeedsRepeatFromStoredDefaults()
    {
        _r.Defaults.Value = new GlobalDefaults { Repeat = RepeatMode.Folder };
        Assert.Equal(RepeatMode.None, _r.Coordinator.Repeat);

        await _r.OpenAsync(0);

        Assert.Equal(RepeatMode.Folder, _r.Coordinator.Repeat);
        Assert.Equal(1, _r.ChangedCount);
    }

    [Fact]
    public async Task NextAsync_FirstUseWithNothingOpen_SeedsRepeatFromStoredDefaults()
    {
        _r.Defaults.Value = new GlobalDefaults { Repeat = RepeatMode.File };

        await _r.Coordinator.NextAsync(CancellationToken.None);

        Assert.Equal(RepeatMode.File, _r.Coordinator.Repeat);
    }

    [Fact]
    public async Task DefaultsChanged_AfterSeeding_UpdatesRepeatAndRaisesChanged()
    {
        _r.Defaults.Value = new GlobalDefaults { Repeat = RepeatMode.Folder };
        await _r.OpenAsync(0);
        var before = _r.ChangedCount;

        _r.Defaults.RaiseChanged(new GlobalDefaults { Repeat = RepeatMode.File });

        Assert.Equal(RepeatMode.File, _r.Coordinator.Repeat);
        Assert.Equal(before + 1, _r.ChangedCount);
    }

    [Fact]
    public async Task CloseAsync_Open_FlushesStopsClearsCurrentAndRaisesChanged()
    {
        await _r.OpenAsync(0);
        var changed = _r.ChangedCount;

        await _r.Coordinator.CloseAsync(CancellationToken.None);

        Assert.Equal(2, _r.Recorder.FlushCount);
        Assert.Equal(new[] { "Open:v1", "Stop" }, _r.Engine.Calls);
        Assert.Null(_r.Coordinator.Current);
        Assert.Equal(changed + 1, _r.ChangedCount);
    }

    [Fact]
    public async Task CloseAsync_NothingOpen_RaisesNoChanged()
    {
        await _r.Coordinator.CloseAsync(CancellationToken.None);

        Assert.Equal(0, _r.ChangedCount);
    }

    [Fact]
    public async Task OpenAsync_ChangedSubscriberThrows_CommandDoesNotFault()
    {
        _r.Coordinator.Changed += (_, _) => throw new InvalidOperationException("subscriber");

        await _r.OpenAsync(0);

        Assert.Equal(_r.Queue[0], _r.Coordinator.Current);
    }

    [Fact]
    public async Task OpenAsync_ChangedSubscriberCallsCurrentAndNextAsync_DoesNotDeadlock()
    {
        _r.ReenterNextOnFirstChanged();

        await _r.OpenAsync(0);

        Assert.Equal(_r.Queue[0], _r.CurrentSeenInChanged);
        Assert.True(_r.NestedNextFinished);
        Assert.Equal(_r.Queue[1], _r.Coordinator.Current);
    }

    [Fact]
    public void Dispose_CalledTwice_DoesNotThrow()
    {
        _r.Coordinator.Dispose();

        var ex = Record.Exception(() => _r.Coordinator.Dispose());

        Assert.Null(ex);
    }

    [Fact]
    public async Task Commands_AfterDispose_ThrowObjectDisposedException()
    {
        _r.Coordinator.Dispose();

        await Assert.ThrowsAsync<ObjectDisposedException>(() => _r.OpenAsync(0));
        await Assert.ThrowsAsync<ObjectDisposedException>(() => _r.Coordinator.NextAsync(CancellationToken.None));
        await Assert.ThrowsAsync<ObjectDisposedException>(() => _r.Coordinator.CloseAsync(CancellationToken.None));
    }

    [Fact]
    public async Task EndedSnapshot_AfterDispose_IsNotHandled()
    {
        _r.Defaults.Value = new GlobalDefaults { Repeat = RepeatMode.File };
        await _r.OpenAsync(0);

        _r.Coordinator.Dispose();
        _r.Engine.Raise(Snap(PlayerStatus.Ended, _r.Queue[0], 60_000, 60_000));

        Assert.Equal(0, _r.Engine.SubscriberCount);
        Assert.Equal(new[] { "Open:v1" }, _r.Engine.Calls);
    }

    [Fact]
    public async Task EndedSnapshot_HandlingThrows_ExceptionNeverReachesTheEngineEvent()
    {
        _r.Defaults.Value = new GlobalDefaults { Repeat = RepeatMode.File };
        await _r.OpenAsync(0);
        _r.Engine.SeekException = new InvalidOperationException("seek");

        var ex = Record.Exception(() => _r.Engine.Raise(Snap(PlayerStatus.Ended, _r.Queue[0], 60_000, 60_000)));
        await WaitUntilAsync(() => _r.Engine.Count("Seek:0") >= 1, "the failing seek");
        await _r.BarrierAsync();

        Assert.Null(ex);
        Assert.Equal(_r.Queue[0], _r.Coordinator.Current);
    }
}
