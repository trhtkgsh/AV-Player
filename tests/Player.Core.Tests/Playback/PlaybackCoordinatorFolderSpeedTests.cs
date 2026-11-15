using Player.Core.Logic.Playback;
using Player.Core.Models;
using Player.Core.Tests.Playback.Fakes;

namespace Player.Core.Tests.Playback;

/// <summary>
/// The coordinator resolves the opening speed as: the file's speed, else the folder's speed, else the global default.
/// The shared rig cannot pass a folder store, so this class wires its own coordinator from the same fakes.
/// </summary>
public sealed class PlaybackCoordinatorFolderSpeedTests : IDisposable
{
    private readonly FakePlayerEngine _engine = new();
    private readonly FakePlaybackStateStore _states = new();
    private readonly FakePlaybackStateRecorder _recorder = new();
    private readonly FakeGlobalDefaultsStore _defaults = new();
    private readonly FakeFolderSettingsStore _folders = new();
    private readonly Microsoft.Extensions.Time.Testing.FakeTimeProvider _time = new();
    private readonly IReadOnlyList<MediaItem> _queue;
    private readonly PlaybackCoordinator _coordinator;

    public PlaybackCoordinatorFolderSpeedTests()
    {
        _queue = new[]
        {
            TestData.Item(1, "v1", bucketId: 10),
            TestData.Item(2, "v2", bucketId: 10),
            TestData.Item(3, "v3", bucketId: 20),
        };
        _defaults.Value = new GlobalDefaults { Speed = 1.25f };
        _coordinator = new PlaybackCoordinator(_engine, _states, _recorder, _defaults, _time, null, _folders);
    }

    public void Dispose() => _coordinator.Dispose();

    private Task OpenAsync(int index = 0) => _coordinator.OpenAsync(_queue[index], _queue, CancellationToken.None);

    private void SetFileSpeed(int index, float speed) =>
        _states.States[_queue[index].Id] = new PlaybackState(_queue[index].Id) { Speed = speed };

    private void SetFolderSpeed(long bucketId, float? speed, bool favorite = false) =>
        _folders.Rows[bucketId] = new FolderSettings(bucketId) { Speed = speed, Favorite = favorite };

    private float OpenedSpeed(int openIndex = 0) => _engine.Opens[openIndex].Settings.Speed;

    [Fact]
    public async Task OpenAsync_FileHasSpeed_OpensWithFileSpeed()
    {
        SetFileSpeed(0, 1.75f);
        SetFolderSpeed(10, 0.5f);

        await OpenAsync();

        Assert.Equal(1.75f, OpenedSpeed());
    }

    [Fact]
    public async Task OpenAsync_FileHasNoSpeedAndFolderHasOne_OpensWithFolderSpeed()
    {
        SetFolderSpeed(10, 0.5f);

        await OpenAsync();

        Assert.Equal(0.5f, OpenedSpeed());
    }

    [Fact]
    public async Task OpenAsync_StateWithoutSpeedAndFolderHasOne_OpensWithFolderSpeed()
    {
        _states.States[_queue[0].Id] = new PlaybackState(_queue[0].Id) { PositionMs = 5_000 };
        SetFolderSpeed(10, 3f);

        await OpenAsync();

        Assert.Equal(3f, OpenedSpeed());
    }

    [Fact]
    public async Task OpenAsync_NeitherFileNorFolderHasSpeed_OpensWithGlobalDefault()
    {
        await OpenAsync();

        Assert.Equal(1.25f, OpenedSpeed());
    }

    [Fact]
    public async Task OpenAsync_FolderRowWithoutSpeed_OpensWithGlobalDefault()
    {
        SetFolderSpeed(10, null, favorite: true);

        await OpenAsync();

        Assert.Equal(1.25f, OpenedSpeed());
    }

    [Fact]
    public async Task OpenAsync_FolderSpeedOfAnotherFolder_IsNotUsed()
    {
        SetFolderSpeed(20, 0.5f);

        await OpenAsync();

        Assert.Equal(1.25f, OpenedSpeed());
    }

    [Fact]
    public async Task OpenAsync_Always_AsksTheStoreWithTheBucketIdOfTheItem()
    {
        await OpenAsync(2);

        Assert.Equal(new long[] { 20 }, _folders.Asked);
    }

    [Fact]
    public async Task OpenAsync_StoreThrows_OpensTheItemWithGlobalDefault()
    {
        SetFolderSpeed(10, 0.5f);
        _folders.GetException = new InvalidOperationException("the folder store is broken");

        await OpenAsync();

        Assert.Equal(1.25f, OpenedSpeed());
        Assert.Equal(_queue[0], _coordinator.Current);
    }

    [Fact]
    public async Task OpenAsync_StoreThrowsAndFileHasSpeed_OpensWithFileSpeed()
    {
        SetFileSpeed(0, 1.75f);
        _folders.GetException = new InvalidOperationException("the folder store is broken");

        await OpenAsync();

        Assert.Equal(1.75f, OpenedSpeed());
    }

    [Fact]
    public async Task OpenAsync_TokenAlreadyCancelled_Cancels()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        SetFolderSpeed(10, 0.5f);

        var open = _coordinator.OpenAsync(_queue[0], _queue, cts.Token);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => open);
        Assert.Empty(_engine.Opens);
        Assert.Null(_coordinator.Current);
    }

    [Fact]
    public async Task OpenAsync_TokenCancelledWhileTheStoreIsAsked_CancelsAndDoesNotOpenEngine()
    {
        using var cts = new CancellationTokenSource();
        _folders.GetHook = ct =>
        {
            cts.Cancel();
            ct.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        };

        var open = _coordinator.OpenAsync(_queue[0], _queue, cts.Token);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => open);
        Assert.Empty(_engine.Opens);
        Assert.Null(_coordinator.Current);
    }

    [Fact]
    public async Task OpenAsync_WithoutFolderStore_OpensWithFileOrGlobalSpeed()
    {
        using var coordinator = new PlaybackCoordinator(_engine, _states, _recorder, _defaults, _time);
        SetFileSpeed(1, 1.75f);

        await coordinator.OpenAsync(_queue[0], _queue, CancellationToken.None);
        await coordinator.OpenAsync(_queue[1], _queue, CancellationToken.None);

        Assert.Equal(1.25f, OpenedSpeed(0));
        Assert.Equal(1.75f, OpenedSpeed(1));
        Assert.Empty(_folders.Asked);
    }

    [Fact]
    public async Task OpenAsync_WithNullFolderStore_OpensWithGlobalDefault()
    {
        using var coordinator = new PlaybackCoordinator(_engine, _states, _recorder, _defaults, _time, null, null);

        await coordinator.OpenAsync(_queue[0], _queue, CancellationToken.None);

        Assert.Equal(1.25f, OpenedSpeed());
        Assert.Equal(_queue[0], coordinator.Current);
    }

    [Fact]
    public async Task NextAsync_NextItemInAnotherFolder_OpensItWithItsOwnFoldersSpeed()
    {
        SetFolderSpeed(10, 0.5f);
        SetFolderSpeed(20, 3f);
        await OpenAsync(1);

        await _coordinator.NextAsync(CancellationToken.None);

        Assert.Equal(0.5f, OpenedSpeed(0));
        Assert.Equal(3f, OpenedSpeed(1));
        Assert.Equal(new long[] { 10, 20 }, _folders.Asked);
    }

    [Fact]
    public async Task NextAsync_NextItemInAFolderWithoutSpeed_OpensItWithGlobalDefault()
    {
        SetFolderSpeed(10, 0.5f);
        await OpenAsync(1);

        await _coordinator.NextAsync(CancellationToken.None);

        Assert.Equal(0.5f, OpenedSpeed(0));
        Assert.Equal(1.25f, OpenedSpeed(1));
    }

    [Fact]
    public async Task PreviousAsync_PreviousItemInAnotherFolder_OpensItWithItsOwnFoldersSpeed()
    {
        SetFolderSpeed(10, 0.5f);
        SetFolderSpeed(20, 3f);
        await OpenAsync(2);

        await _coordinator.PreviousAsync(CancellationToken.None);

        Assert.Equal(3f, OpenedSpeed(0));
        Assert.Equal(0.5f, OpenedSpeed(1));
    }
}
