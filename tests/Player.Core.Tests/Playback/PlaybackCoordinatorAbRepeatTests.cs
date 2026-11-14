using Player.Core.Models;
using static Player.Core.Tests.Playback.CoordinatorRig;

namespace Player.Core.Tests.Playback;

public sealed class PlaybackCoordinatorAbRepeatTests : IDisposable
{
    private const long Duration = 60_000;
    private static readonly AbRepeat Range = new(10_000, 20_000);

    private readonly CoordinatorRig _r = new();

    public void Dispose() => _r.Dispose();

    private PlayerSnapshot At(PlayerStatus status, long positionMs) =>
        Snap(status, _r.Queue[0], positionMs, Duration);

    private async Task OpenWithRangeAsync()
    {
        await _r.OpenAsync(0);
        _r.Engine.Snapshot = At(PlayerStatus.Playing, 0);
        await _r.Coordinator.SetAbRepeatAsync(Range, CancellationToken.None);
    }

    [Fact]
    public async Task SetAbRepeatAsync_NothingOpen_ThrowsInvalidOperation()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _r.Coordinator.SetAbRepeatAsync(new AbRepeat(0, 5_000), CancellationToken.None));
    }

    [Fact]
    public async Task SetAbRepeatAsync_NullAndNothingOpen_DoesNotThrow()
    {
        await _r.Coordinator.SetAbRepeatAsync(null, CancellationToken.None);

        Assert.Null(_r.Coordinator.AbRepeat);
    }

    [Theory]
    [InlineData(-1, 2_000)]
    [InlineData(1_000, 1_999)]
    [InlineData(5_000, 4_000)]
    [InlineData(59_000, 60_001)]
    public async Task SetAbRepeatAsync_InvalidRange_ThrowsArgumentException(long start, long end)
    {
        await _r.OpenAsync(0);
        _r.Engine.Snapshot = At(PlayerStatus.Playing, 0);

        await Assert.ThrowsAsync<ArgumentException>(
            () => _r.Coordinator.SetAbRepeatAsync(new AbRepeat(start, end), CancellationToken.None));

        Assert.Null(_r.Coordinator.AbRepeat);
    }

    [Theory]
    [InlineData(0, 1_000)]
    [InlineData(59_000, 60_000)]
    public async Task SetAbRepeatAsync_BoundaryRange_IsAccepted(long start, long end)
    {
        await _r.OpenAsync(0);
        _r.Engine.Snapshot = At(PlayerStatus.Playing, 0);

        await _r.Coordinator.SetAbRepeatAsync(new AbRepeat(start, end), CancellationToken.None);

        Assert.Equal(new AbRepeat(start, end), _r.Coordinator.AbRepeat);
    }

    [Fact]
    public async Task SetAbRepeatAsync_EngineStillReportsDurationZero_AcceptsEndBeyondZero()
    {
        await _r.OpenAsync(0);

        await _r.Coordinator.SetAbRepeatAsync(new AbRepeat(0, 5_000), CancellationToken.None);

        Assert.Equal(new AbRepeat(0, 5_000), _r.Coordinator.AbRepeat);
    }

    [Fact]
    public async Task SetAbRepeatAsync_Null_ClearsRange()
    {
        await OpenWithRangeAsync();

        await _r.Coordinator.SetAbRepeatAsync(null, CancellationToken.None);

        Assert.Null(_r.Coordinator.AbRepeat);
    }

    [Fact]
    public async Task SetAbRepeatAsync_ValidRange_RaisesChanged()
    {
        await _r.OpenAsync(0);
        _r.Engine.Snapshot = At(PlayerStatus.Playing, 0);
        var before = _r.ChangedCount;

        await _r.Coordinator.SetAbRepeatAsync(Range, CancellationToken.None);

        Assert.Equal(before + 1, _r.ChangedCount);
    }

    [Fact]
    public async Task AbRepeat_PlayingAtOrPastEnd_SeeksToStart()
    {
        await OpenWithRangeAsync();

        _r.Engine.Raise(At(PlayerStatus.Playing, 20_000));
        await WaitUntilAsync(() => _r.Engine.Count("Seek:10000") == 1, "the A-B jump");

        Assert.Equal(new[] { "Open:v1", "Seek:10000" }, _r.Engine.Calls);
    }

    [Fact]
    public async Task AbRepeat_RepeatedSnapshotsPastEnd_SeekAgainOnlyAfterOneSecondOnFakeTime()
    {
        await OpenWithRangeAsync();
        _r.Engine.Raise(At(PlayerStatus.Playing, 20_000));
        await WaitUntilAsync(() => _r.Engine.Count("Seek:10000") == 1, "the first A-B jump");

        _r.Engine.Raise(At(PlayerStatus.Playing, 20_100));
        _r.Engine.Raise(At(PlayerStatus.Playing, 20_200));
        _r.Time.Advance(TimeSpan.FromMilliseconds(1_100));
        _r.Engine.Raise(At(PlayerStatus.Playing, 20_300));
        await WaitUntilAsync(() => _r.Engine.Count("Seek:10000") >= 2, "the jump after the re-arm time");
        await _r.BarrierAsync();

        Assert.Equal(2, _r.Engine.Count("Seek:10000"));
    }

    [Fact]
    public async Task AbRepeat_PositionDropsBelowEnd_ArmsTheNextJump()
    {
        await OpenWithRangeAsync();
        _r.Engine.Raise(At(PlayerStatus.Playing, 20_000));
        await WaitUntilAsync(() => _r.Engine.Count("Seek:10000") == 1, "the first A-B jump");

        _r.Engine.Raise(At(PlayerStatus.Playing, 15_000));
        _r.Engine.Raise(At(PlayerStatus.Playing, 20_000));
        await WaitUntilAsync(() => _r.Engine.Count("Seek:10000") == 2, "the second A-B jump");
        await _r.BarrierAsync();

        Assert.Equal(2, _r.Engine.Count("Seek:10000"));
    }

    [Fact]
    public async Task AbRepeat_PausedPastEnd_DoesNotJump()
    {
        await OpenWithRangeAsync();

        _r.Engine.Raise(At(PlayerStatus.Paused, 20_000));
        _r.Engine.Raise(At(PlayerStatus.Playing, 20_000));
        await WaitUntilAsync(() => _r.Engine.CountPrefix("Seek:") >= 1, "the jump of the playing snapshot");
        await _r.BarrierAsync();

        Assert.Equal(1, _r.Engine.CountPrefix("Seek:"));
    }

    [Fact]
    public async Task AbRepeat_EndedSnapshot_SeeksToStartAndPlaysInsteadOfAdvancing()
    {
        _r.Defaults.Value = new GlobalDefaults { Repeat = RepeatMode.Folder };
        await OpenWithRangeAsync();

        _r.Engine.Raise(At(PlayerStatus.Ended, Duration));
        await WaitUntilAsync(() => _r.Engine.Count("Play") == 1, "the A-B restart");

        Assert.Equal(new[] { "Open:v1", "Seek:10000", "Play" }, _r.Engine.Calls);
        Assert.Equal(_r.Queue[0], _r.Coordinator.Current);
    }

    [Fact]
    public async Task OpenAsync_AnotherItem_ClearsAbRepeat()
    {
        await OpenWithRangeAsync();

        await _r.OpenAsync(1);

        Assert.Null(_r.Coordinator.AbRepeat);
    }
}
