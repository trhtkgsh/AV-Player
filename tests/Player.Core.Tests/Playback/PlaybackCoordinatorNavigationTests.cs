using Player.Core.Models;
using static Player.Core.Tests.Playback.CoordinatorRig;

namespace Player.Core.Tests.Playback;

public sealed class PlaybackCoordinatorNavigationTests : IDisposable
{
    private readonly CoordinatorRig _r = new();

    public void Dispose() => _r.Dispose();

    private async Task OpenAtAsync(int index, RepeatMode repeat = RepeatMode.None)
    {
        _r.Defaults.Value = new GlobalDefaults { Repeat = repeat };
        await _r.OpenAsync(index);
    }

    [Fact]
    public async Task NextAsync_MiddleItem_OpensNextItem()
    {
        await OpenAtAsync(1);

        await _r.Coordinator.NextAsync(CancellationToken.None);

        Assert.Equal(_r.Queue[2], _r.Coordinator.Current);
        Assert.Equal(new[] { "Open:v2", "Open:v3" }, _r.Engine.Calls);
    }

    [Fact]
    public async Task PreviousAsync_MiddleItem_OpensPreviousItem()
    {
        await OpenAtAsync(1);

        await _r.Coordinator.PreviousAsync(CancellationToken.None);

        Assert.Equal(_r.Queue[0], _r.Coordinator.Current);
        Assert.Equal(new[] { "Open:v2", "Open:v1" }, _r.Engine.Calls);
    }

    [Theory]
    [InlineData(RepeatMode.None)]
    [InlineData(RepeatMode.File)]
    public async Task NextAsync_LastItemWithoutFolderRepeat_DoesNothing(RepeatMode repeat)
    {
        await OpenAtAsync(2, repeat);
        var changed = _r.ChangedCount;

        await _r.Coordinator.NextAsync(CancellationToken.None);

        Assert.Equal(_r.Queue[2], _r.Coordinator.Current);
        Assert.Equal(new[] { "Open:v3" }, _r.Engine.Calls);
        Assert.Equal(changed, _r.ChangedCount);
    }

    [Theory]
    [InlineData(RepeatMode.None)]
    [InlineData(RepeatMode.File)]
    public async Task PreviousAsync_FirstItemWithoutFolderRepeat_DoesNothing(RepeatMode repeat)
    {
        await OpenAtAsync(0, repeat);
        var changed = _r.ChangedCount;

        await _r.Coordinator.PreviousAsync(CancellationToken.None);

        Assert.Equal(_r.Queue[0], _r.Coordinator.Current);
        Assert.Equal(new[] { "Open:v1" }, _r.Engine.Calls);
        Assert.Equal(changed, _r.ChangedCount);
    }

    [Fact]
    public async Task NextAsync_LastItemWithFolderRepeat_WrapsToFirstItem()
    {
        await OpenAtAsync(2, RepeatMode.Folder);

        await _r.Coordinator.NextAsync(CancellationToken.None);

        Assert.Equal(_r.Queue[0], _r.Coordinator.Current);
        Assert.Equal(new[] { "Open:v3", "Open:v1" }, _r.Engine.Calls);
    }

    [Fact]
    public async Task PreviousAsync_FirstItemWithFolderRepeat_WrapsToLastItem()
    {
        await OpenAtAsync(0, RepeatMode.Folder);

        await _r.Coordinator.PreviousAsync(CancellationToken.None);

        Assert.Equal(_r.Queue[2], _r.Coordinator.Current);
        Assert.Equal(new[] { "Open:v1", "Open:v3" }, _r.Engine.Calls);
    }

    [Fact]
    public async Task NextAsync_SingleItemQueueWithFolderRepeat_RestartsFileAndKeepsCurrent()
    {
        _r.Defaults.Value = new GlobalDefaults { Repeat = RepeatMode.Folder };
        var single = new[] { _r.Queue[0] };
        await _r.Coordinator.OpenAsync(_r.Queue[0], single, CancellationToken.None);

        await _r.Coordinator.NextAsync(CancellationToken.None);

        Assert.Equal(new[] { "Open:v1", "Seek:0", "Play" }, _r.Engine.Calls);
        Assert.Equal(_r.Queue[0], _r.Coordinator.Current);
    }

    [Fact]
    public async Task NextAsync_NothingOpen_DoesNothing()
    {
        await _r.Coordinator.NextAsync(CancellationToken.None);

        Assert.Empty(_r.Engine.Calls);
        Assert.Null(_r.Coordinator.Current);
        Assert.Equal(0, _r.ChangedCount);
    }

    [Fact]
    public async Task PreviousAsync_PositionAboveThreshold_SeeksToZeroAndKeepsItem()
    {
        await OpenAtAsync(1);
        _r.Engine.Snapshot = Snap(PlayerStatus.Playing, _r.Queue[1], 3_001, 60_000);

        await _r.Coordinator.PreviousAsync(CancellationToken.None);

        Assert.Equal(new[] { "Open:v2", "Seek:0" }, _r.Engine.Calls);
        Assert.Equal(_r.Queue[1], _r.Coordinator.Current);
    }

    [Fact]
    public async Task PreviousAsync_PositionExactlyAtThreshold_OpensPreviousItem()
    {
        await OpenAtAsync(1);
        _r.Engine.Snapshot = Snap(PlayerStatus.Playing, _r.Queue[1], 3_000, 60_000);

        await _r.Coordinator.PreviousAsync(CancellationToken.None);

        Assert.Equal(new[] { "Open:v2", "Open:v1" }, _r.Engine.Calls);
        Assert.Equal(_r.Queue[0], _r.Coordinator.Current);
    }

    [Fact]
    public async Task PreviousAsync_EngineEnded_SeeksToZeroAndThenPlays()
    {
        await OpenAtAsync(1);
        _r.Engine.Snapshot = Snap(PlayerStatus.Ended, _r.Queue[1], 60_000, 60_000);

        await _r.Coordinator.PreviousAsync(CancellationToken.None);

        Assert.Equal(new[] { "Open:v2", "Seek:0", "Play" }, _r.Engine.Calls);
        Assert.Equal(_r.Queue[1], _r.Coordinator.Current);
    }

    [Theory]
    [InlineData(5_000, 10_000, 2_000, 7_000)]
    [InlineData(5_000, 10_000, 5_000, 10_000)]
    [InlineData(5_000, 10_000, 9_000, 10_000)]
    [InlineData(5_000, 10_000, -5_000, 0)]
    [InlineData(5_000, 10_000, -9_000, 0)]
    [InlineData(1_000, 0, 5_000, 6_000)]
    [InlineData(1_000, 0, -5_000, 0)]
    public async Task SkipAsync_Delta_SeeksToClampedPosition(long position, long duration, long delta, long expected)
    {
        await OpenAtAsync(0);
        _r.Engine.Snapshot = Snap(PlayerStatus.Playing, _r.Queue[0], position, duration);

        await _r.Coordinator.SkipAsync(delta, CancellationToken.None);

        Assert.Equal(new[] { "Open:v1", $"Seek:{expected}" }, _r.Engine.Calls);
    }

    [Fact]
    public async Task SkipAsync_NothingOpen_DoesNothing()
    {
        await _r.Coordinator.SkipAsync(5_000, CancellationToken.None);

        Assert.Empty(_r.Engine.Calls);
    }
}
