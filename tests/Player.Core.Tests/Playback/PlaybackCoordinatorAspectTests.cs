using Player.Core.Logic.Playback;
using Player.Core.Models;
using Player.Core.Contracts;
using Player.Core.Tests.Playback.Fakes;
using static Player.Core.Tests.Playback.CoordinatorRig;

namespace Player.Core.Tests.Playback;

/// <summary>
/// The coordinator hands the resolved aspect mode to the video surface before it opens the engine.
/// The shared rig cannot pass a surface, so this class wires its own coordinator from the same fakes.
/// </summary>
public sealed class PlaybackCoordinatorAspectTests : IDisposable
{
    private readonly object _logLock = new();
    private readonly List<string> _log = new();
    private readonly FakePlayerEngine _engine = new();
    private readonly FakePlaybackStateStore _states = new();
    private readonly FakePlaybackStateRecorder _recorder = new();
    private readonly FakeGlobalDefaultsStore _defaults = new();
    private readonly Microsoft.Extensions.Time.Testing.FakeTimeProvider _time = new();
    private readonly FakeVideoSurface _surface;
    private readonly IReadOnlyList<MediaItem> _queue;
    private readonly PlaybackCoordinator _coordinator;

    public PlaybackCoordinatorAspectTests()
    {
        _queue = new[] { TestData.Item(1, "v1"), TestData.Item(2, "v2"), TestData.Item(3, "v3") };
        _surface = new FakeVideoSurface(Log);
        _engine.OpenHook = item =>
        {
            Log($"Open:{item.DisplayName}");
            return Task.CompletedTask;
        };
        _coordinator = new PlaybackCoordinator(_engine, _states, _recorder, _defaults, _time, _surface);
    }

    public void Dispose() => _coordinator.Dispose();

    private void Log(string entry)
    {
        lock (_logLock)
        {
            _log.Add(entry);
        }
    }

    private string[] LogSnapshot()
    {
        lock (_logLock)
        {
            return _log.ToArray();
        }
    }

    private Task OpenAsync(int index = 0) => _coordinator.OpenAsync(_queue[index], _queue, CancellationToken.None);

    private void SetFileAspect(int index, AspectMode aspect) =>
        _states.States[_queue[index].Id] = new PlaybackState(_queue[index].Id) { Aspect = aspect };

    private PlayerSnapshot Ended(int index) => Snap(PlayerStatus.Ended, _queue[index], 60_000, 60_000);

    [Fact]
    public async Task OpenAsync_FileHasAspect_AppliesFileAspect()
    {
        SetFileAspect(0, AspectMode.Crop);
        _defaults.Value = new GlobalDefaults { Aspect = AspectMode.Stretch };

        await OpenAsync();

        Assert.Equal(new[] { AspectMode.Crop }, _surface.AspectCalls);
    }

    [Fact]
    public async Task OpenAsync_FileHasNoAspect_AppliesGlobalDefaultAspect()
    {
        _defaults.Value = new GlobalDefaults { Aspect = AspectMode.Stretch };

        await OpenAsync();

        Assert.Equal(new[] { AspectMode.Stretch }, _surface.AspectCalls);
    }

    [Fact]
    public async Task OpenAsync_StateWithoutAspect_AppliesGlobalDefaultAspect()
    {
        _states.States[_queue[0].Id] = new PlaybackState(_queue[0].Id) { PositionMs = 5_000 };
        _defaults.Value = new GlobalDefaults { Aspect = AspectMode.Original };

        await OpenAsync();

        Assert.Equal(new[] { AspectMode.Original }, _surface.AspectCalls);
    }

    [Fact]
    public async Task OpenAsync_NoStoredStateAndFactoryDefaults_AppliesFit()
    {
        await OpenAsync();

        Assert.Equal(new[] { AspectMode.Fit }, _surface.AspectCalls);
    }

    [Fact]
    public async Task OpenAsync_Always_SetsAspectBeforeEngineOpen()
    {
        SetFileAspect(0, AspectMode.Crop);

        await OpenAsync();

        Assert.Equal(new[] { "SetAspect:Crop", "Open:v1" }, LogSnapshot());
    }

    [Fact]
    public async Task OpenAsync_Always_PassesTheSameAspectToEngineSettings()
    {
        SetFileAspect(0, AspectMode.Original);

        await OpenAsync();

        Assert.Equal(AspectMode.Original, Assert.Single(_engine.Opens).Settings.Aspect);
    }

    [Fact]
    public async Task NextAsync_AfterOpen_SetsAspectOfNextFileBeforeItsEngineOpen()
    {
        SetFileAspect(0, AspectMode.Crop);
        SetFileAspect(1, AspectMode.Stretch);
        await OpenAsync();

        await _coordinator.NextAsync(CancellationToken.None);

        Assert.Equal(new[] { AspectMode.Crop, AspectMode.Stretch }, _surface.AspectCalls);
        Assert.Equal(
            new[] { "SetAspect:Crop", "Open:v1", "SetAspect:Stretch", "Open:v2" },
            LogSnapshot());
    }

    [Fact]
    public async Task PreviousAsync_AfterOpen_SetsAspectOfPreviousFileBeforeItsEngineOpen()
    {
        SetFileAspect(0, AspectMode.Original);
        SetFileAspect(1, AspectMode.Stretch);
        await OpenAsync(1);

        await _coordinator.PreviousAsync(CancellationToken.None);

        Assert.Equal(new[] { AspectMode.Stretch, AspectMode.Original }, _surface.AspectCalls);
        Assert.Equal(
            new[] { "SetAspect:Stretch", "Open:v2", "SetAspect:Original", "Open:v1" },
            LogSnapshot());
    }

    [Fact]
    public async Task OpenAsync_CalledTwice_SetsAspectOnEveryOpen()
    {
        await OpenAsync();
        await OpenAsync();

        Assert.Equal(new[] { AspectMode.Fit, AspectMode.Fit }, _surface.AspectCalls);
    }

    [Fact]
    public async Task EndedSnapshot_RepeatFolder_SetsAspectOfNextFileBeforeItsEngineOpen()
    {
        _defaults.Value = new GlobalDefaults { Repeat = RepeatMode.Folder };
        SetFileAspect(0, AspectMode.Crop);
        SetFileAspect(1, AspectMode.Stretch);
        await OpenAsync();

        _engine.Raise(Ended(0));
        await WaitUntilAsync(() => _coordinator.Current == _queue[1], "the next item to open");

        Assert.Equal(
            new[] { "SetAspect:Crop", "Open:v1", "SetAspect:Stretch", "Open:v2" },
            LogSnapshot());
    }

    [Fact]
    public async Task EndedSnapshot_RepeatFolderOnLastItem_SetsAspectOfFirstFileOnWrap()
    {
        _defaults.Value = new GlobalDefaults { Repeat = RepeatMode.Folder };
        SetFileAspect(0, AspectMode.Original);
        SetFileAspect(2, AspectMode.Stretch);
        await OpenAsync(2);

        _engine.Raise(Ended(2));
        await WaitUntilAsync(() => _coordinator.Current == _queue[0], "the first item to open");

        Assert.Equal(new[] { AspectMode.Stretch, AspectMode.Original }, _surface.AspectCalls);
    }

    // Repeat File restarts the same input with Seek and Play. The engine is not opened again, so the coordinator
    // has no open to attach an aspect call to, and the surface keeps the mode it already has.
    [Fact]
    public async Task EndedSnapshot_RepeatFile_RestartsWithoutNewOpenAndWithoutSecondSetAspect()
    {
        _defaults.Value = new GlobalDefaults { Repeat = RepeatMode.File };
        SetFileAspect(0, AspectMode.Crop);
        await OpenAsync();

        _engine.Raise(Ended(0));
        await WaitUntilAsync(() => _engine.Count("Play") == 1, "the restart");
        await _coordinator.SetAbRepeatAsync(null, CancellationToken.None);

        Assert.Equal(new[] { "Open:v1", "Seek:0", "Play" }, _engine.Calls);
        Assert.Equal(new[] { AspectMode.Crop }, _surface.AspectCalls);
    }

    [Fact]
    public async Task OpenAsync_SurfaceThrows_OpenSucceedsAndEngineIsOpened()
    {
        _surface.SetAspectException = new InvalidOperationException("surface is not attached");

        await OpenAsync();

        Assert.Equal(new[] { "Open:v1" }, _engine.Calls);
        Assert.Equal(_queue[0], _coordinator.Current);
        Assert.Single(_surface.AspectCalls);
    }

    [Fact]
    public async Task NextAsync_SurfaceThrows_NextFileIsStillOpened()
    {
        await OpenAsync();
        _surface.SetAspectException = new InvalidOperationException("surface is not attached");

        await _coordinator.NextAsync(CancellationToken.None);

        Assert.Equal(new[] { "Open:v1", "Open:v2" }, _engine.Calls);
        Assert.Equal(_queue[1], _coordinator.Current);
    }

    [Fact]
    public async Task OpenAsync_WithoutSurface_OpensTheEngine()
    {
        using var coordinator = new PlaybackCoordinator(_engine, _states, _recorder, _defaults, _time);

        await coordinator.OpenAsync(_queue[0], _queue, CancellationToken.None);

        Assert.Equal(new[] { "Open:v1" }, _engine.Calls);
        Assert.Equal(_queue[0], coordinator.Current);
        Assert.Empty(_surface.AspectCalls);
    }

    [Fact]
    public async Task OpenAsync_WithNullSurface_OpensTheEngine()
    {
        using var coordinator = new PlaybackCoordinator(_engine, _states, _recorder, _defaults, _time, null);

        await coordinator.OpenAsync(_queue[0], _queue, CancellationToken.None);

        Assert.Equal(_queue[0], coordinator.Current);
    }

    [Fact]
    public async Task OpenAsync_TokenAlreadyCancelled_DoesNotSetAspectAndDoesNotOpenEngine()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var open = _coordinator.OpenAsync(_queue[0], _queue, cts.Token);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => open);
        Assert.Empty(_surface.AspectCalls);
        Assert.Empty(_engine.Calls);
        Assert.Null(_coordinator.Current);
    }

    [Fact]
    public async Task OpenAsync_TokenCancelledWhileLoadingState_DoesNotSetAspectAndDoesNotOpenEngine()
    {
        using var cts = new CancellationTokenSource();
        var states = new CancellingStateStore(cts);
        using var coordinator = new PlaybackCoordinator(_engine, states, _recorder, _defaults, _time, _surface);

        var open = coordinator.OpenAsync(_queue[0], _queue, cts.Token);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => open);
        Assert.Empty(_surface.AspectCalls);
        Assert.Empty(_engine.Calls);
        Assert.Null(coordinator.Current);
    }

    /// <summary>Cancels the given source when the coordinator reads the stored state.</summary>
    private sealed class CancellingStateStore : IPlaybackStateStore
    {
        private readonly CancellationTokenSource _cts;

        public CancellingStateStore(CancellationTokenSource cts)
        {
            _cts = cts;
        }

        public Task<PlaybackState?> GetAsync(MediaFileId id, CancellationToken ct)
        {
            _cts.Cancel();
            return Task.FromResult<PlaybackState?>(null);
        }

        public Task<IReadOnlyDictionary<MediaFileId, PlaybackState>> GetManyAsync(
            IReadOnlyCollection<MediaFileId> ids, CancellationToken ct) =>
            Task.FromResult<IReadOnlyDictionary<MediaFileId, PlaybackState>>(new Dictionary<MediaFileId, PlaybackState>());

        public Task SaveAsync(PlaybackState state, CancellationToken ct) => Task.CompletedTask;

        public Task DeleteAsync(MediaFileId id, CancellationToken ct) => Task.CompletedTask;
    }
}
