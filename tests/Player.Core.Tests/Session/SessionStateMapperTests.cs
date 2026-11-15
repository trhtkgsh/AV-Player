using Microsoft.Extensions.Time.Testing;
using Player.Core.Logic.Session;
using Player.Core.Models;

namespace Player.Core.Tests.Session;

public sealed class SessionStateMapperTests
{
    private const SessionActions OpenItemActions =
        SessionActions.Play | SessionActions.Pause | SessionActions.PlayPause | SessionActions.Stop
        | SessionActions.SeekTo | SessionActions.FastForward | SessionActions.Rewind;

    private static PlayerSnapshot Snapshot(
        PlayerStatus status = PlayerStatus.Playing,
        long positionMs = 0,
        long durationMs = 100_000,
        float speed = 1.0f,
        string? error = null) =>
        PlayerSnapshot.Idle with
        {
            Status = status,
            PositionMs = positionMs,
            DurationMs = durationMs,
            Speed = speed,
            Error = error,
        };

    private static SessionState Map(PlayerSnapshot snapshot, bool hasItem = true, QueueAvailability? queue = null) =>
        SessionStateMapper.Map(snapshot, hasItem, queue ?? QueueAvailability.None);

    // ---- Map: state ----

    [Fact]
    public void Map_NoItem_ReturnsEmpty()
    {
        var state = Map(Snapshot(PlayerStatus.Playing, 5_000), hasItem: false, new QueueAvailability(true, true));

        Assert.Same(SessionState.Empty, state);
        Assert.Equal(SessionPlaybackState.None, state.State);
        Assert.Equal(SessionActions.None, state.Actions);
    }

    [Theory]
    [InlineData(PlayerStatus.Playing, SessionPlaybackState.Playing)]
    [InlineData(PlayerStatus.Paused, SessionPlaybackState.Paused)]
    [InlineData(PlayerStatus.Opening, SessionPlaybackState.Buffering)]
    [InlineData(PlayerStatus.Error, SessionPlaybackState.Error)]
    [InlineData(PlayerStatus.Idle, SessionPlaybackState.Stopped)]
    [InlineData(PlayerStatus.Ended, SessionPlaybackState.Stopped)]
    public void Map_Status_MapsToSessionState(PlayerStatus status, SessionPlaybackState expected)
    {
        Assert.Equal(expected, Map(Snapshot(status)).State);
    }

    // ---- Map: speed ----

    [Fact]
    public void Map_Playing_UsesSnapshotSpeed()
    {
        Assert.Equal(1.5f, Map(Snapshot(PlayerStatus.Playing, speed: 1.5f)).Speed);
    }

    [Theory]
    [InlineData(PlayerStatus.Paused)]
    [InlineData(PlayerStatus.Opening)]
    [InlineData(PlayerStatus.Error)]
    [InlineData(PlayerStatus.Idle)]
    [InlineData(PlayerStatus.Ended)]
    public void Map_NotPlaying_SpeedIsZero(PlayerStatus status)
    {
        Assert.Equal(0f, Map(Snapshot(status, speed: 2.0f)).Speed);
    }

    // ---- Map: position ----

    [Fact]
    public void Map_PositionInsideDuration_IsKept()
    {
        Assert.Equal(42_000, Map(Snapshot(positionMs: 42_000, durationMs: 100_000)).PositionMs);
    }

    [Fact]
    public void Map_NegativePosition_ClampsToZero()
    {
        Assert.Equal(0, Map(Snapshot(positionMs: -500)).PositionMs);
    }

    [Fact]
    public void Map_PositionBeyondDuration_ClampsToDuration()
    {
        Assert.Equal(100_000, Map(Snapshot(positionMs: 100_500, durationMs: 100_000)).PositionMs);
    }

    [Fact]
    public void Map_PositionEqualsDuration_IsKept()
    {
        Assert.Equal(100_000, Map(Snapshot(positionMs: 100_000, durationMs: 100_000)).PositionMs);
    }

    [Fact]
    public void Map_DurationZero_PositionNotClampedFromAbove()
    {
        // Pinned: an unknown duration (0) disables the upper clamp.
        Assert.Equal(7_000, Map(Snapshot(positionMs: 7_000, durationMs: 0)).PositionMs);
    }

    [Fact]
    public void Map_DurationNegative_PositionNotClampedFromAbove()
    {
        // Pinned: only a duration above 0 clamps; a negative one behaves like unknown.
        Assert.Equal(7_000, Map(Snapshot(positionMs: 7_000, durationMs: -1)).PositionMs);
    }

    [Fact]
    public void Map_DurationZeroAndNegativePosition_ClampsToZero()
    {
        Assert.Equal(0, Map(Snapshot(positionMs: -1, durationMs: 0)).PositionMs);
    }

    // ---- Map: actions ----

    [Fact]
    public void Map_OpenItemNoQueue_OffersBasicActionsOnly()
    {
        Assert.Equal(OpenItemActions, Map(Snapshot()).Actions);
    }

    [Fact]
    public void Map_HasNext_AddsSkipToNext()
    {
        var actions = Map(Snapshot(), queue: new QueueAvailability(true, false)).Actions;

        Assert.Equal(OpenItemActions | SessionActions.SkipToNext, actions);
    }

    [Fact]
    public void Map_HasPrevious_AddsSkipToPrevious()
    {
        var actions = Map(Snapshot(), queue: new QueueAvailability(false, true)).Actions;

        Assert.Equal(OpenItemActions | SessionActions.SkipToPrevious, actions);
    }

    [Fact]
    public void Map_HasNextAndPrevious_AddsBoth()
    {
        var actions = Map(Snapshot(), queue: new QueueAvailability(true, true)).Actions;

        Assert.Equal(OpenItemActions | SessionActions.SkipToNext | SessionActions.SkipToPrevious, actions);
    }

    [Theory]
    [InlineData(PlayerStatus.Idle)]
    [InlineData(PlayerStatus.Opening)]
    [InlineData(PlayerStatus.Paused)]
    [InlineData(PlayerStatus.Ended)]
    [InlineData(PlayerStatus.Error)]
    public void Map_AnyStatusWithItem_OffersSameBasicActions(PlayerStatus status)
    {
        Assert.Equal(OpenItemActions, Map(Snapshot(status)).Actions);
    }

    // ---- Map: error ----

    [Fact]
    public void Map_ErrorStatus_CarriesEngineMessage()
    {
        Assert.Equal("decoder failed", Map(Snapshot(PlayerStatus.Error, error: "decoder failed")).Error);
    }

    [Fact]
    public void Map_ErrorStatusWithoutMessage_ErrorIsEmptyString()
    {
        // Pinned: a null engine message becomes "" so an Error state always has a non-null Error.
        Assert.Equal(string.Empty, Map(Snapshot(PlayerStatus.Error, error: null)).Error);
    }

    [Fact]
    public void Map_NotErrorStatus_ErrorIsNull()
    {
        Assert.Null(Map(Snapshot(PlayerStatus.Paused, error: "stale")).Error);
    }

    [Fact]
    public void Map_NullSnapshot_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            SessionStateMapper.Map(null!, true, QueueAvailability.None));
    }

    // ---- GetQueueAvailability ----

    private static List<MediaItem> Queue(int count)
    {
        var items = new List<MediaItem>();
        for (var i = 1; i <= count; i++)
        {
            items.Add(TestData.Item(mediaStoreId: i, name: $"v{i}.mp4"));
        }

        return items;
    }

    [Fact]
    public void GetQueueAvailability_EmptyQueue_None()
    {
        var result = SessionStateMapper.GetQueueAvailability(new List<MediaItem>(), TestData.Item(1), RepeatMode.None);

        Assert.Equal(QueueAvailability.None, result);
    }

    [Theory]
    [InlineData(RepeatMode.None)]
    [InlineData(RepeatMode.File)]
    [InlineData(RepeatMode.Folder)]
    public void GetQueueAvailability_SingleItem_NoneForEveryRepeatMode(RepeatMode repeat)
    {
        var queue = Queue(1);

        var result = SessionStateMapper.GetQueueAvailability(queue, queue[0], repeat);

        Assert.Equal(QueueAvailability.None, result);
    }

    [Fact]
    public void GetQueueAvailability_FirstItem_NextOnly()
    {
        var queue = Queue(3);

        var result = SessionStateMapper.GetQueueAvailability(queue, queue[0], RepeatMode.None);

        Assert.Equal(new QueueAvailability(true, false), result);
    }

    [Fact]
    public void GetQueueAvailability_LastItem_PreviousOnly()
    {
        var queue = Queue(3);

        var result = SessionStateMapper.GetQueueAvailability(queue, queue[2], RepeatMode.None);

        Assert.Equal(new QueueAvailability(false, true), result);
    }

    [Fact]
    public void GetQueueAvailability_MiddleItem_Both()
    {
        var queue = Queue(3);

        var result = SessionStateMapper.GetQueueAvailability(queue, queue[1], RepeatMode.None);

        Assert.Equal(new QueueAvailability(true, true), result);
    }

    [Fact]
    public void GetQueueAvailability_TwoItemsFirst_NextOnly()
    {
        var queue = Queue(2);

        var result = SessionStateMapper.GetQueueAvailability(queue, queue[0], RepeatMode.None);

        Assert.Equal(new QueueAvailability(true, false), result);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void GetQueueAvailability_FolderRepeat_BothAtEveryPosition(int index)
    {
        var queue = Queue(3);

        var result = SessionStateMapper.GetQueueAvailability(queue, queue[index], RepeatMode.Folder);

        Assert.Equal(new QueueAvailability(true, true), result);
    }

    [Fact]
    public void GetQueueAvailability_FileRepeatAtLastItem_PreviousOnly()
    {
        // Pinned: only Folder repeat wraps; File repeat behaves like no repeat for skipping.
        var queue = Queue(3);

        var result = SessionStateMapper.GetQueueAvailability(queue, queue[2], RepeatMode.File);

        Assert.Equal(new QueueAvailability(false, true), result);
    }

    [Fact]
    public void GetQueueAvailability_CurrentNotInQueue_Both()
    {
        var result = SessionStateMapper.GetQueueAvailability(Queue(3), TestData.Item(99), RepeatMode.None);

        Assert.Equal(new QueueAvailability(true, true), result);
    }

    [Fact]
    public void GetQueueAvailability_CurrentNotInSingleItemQueue_None()
    {
        var result = SessionStateMapper.GetQueueAvailability(Queue(1), TestData.Item(99), RepeatMode.None);

        Assert.Equal(QueueAvailability.None, result);
    }

    [Fact]
    public void GetQueueAvailability_NullCurrent_None()
    {
        // Pinned: no current item offers nothing, even for a long queue and Folder repeat.
        var result = SessionStateMapper.GetQueueAvailability(Queue(3), null, RepeatMode.Folder);

        Assert.Equal(QueueAvailability.None, result);
    }

    [Fact]
    public void GetQueueAvailability_MatchesById_NotByReference()
    {
        var queue = Queue(3);
        var sameIdOtherInstance = queue[2] with { DisplayName = "renamed.mp4" };

        var result = SessionStateMapper.GetQueueAvailability(queue, sameIdOtherInstance, RepeatMode.None);

        Assert.Equal(new QueueAvailability(false, true), result);
    }

    [Fact]
    public void GetQueueAvailability_NullQueue_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            SessionStateMapper.GetQueueAvailability(null!, TestData.Item(1), RepeatMode.None));
    }
}

public sealed class SessionPublishGateTests
{
    private const SessionActions Basic = SessionActions.Play | SessionActions.Pause;

    private sealed class Rig
    {
        public Rig()
        {
            Time = new FakeTimeProvider();
            Gate = new SessionPublishGate(Time);
        }

        public FakeTimeProvider Time { get; }

        public SessionPublishGate Gate { get; }
    }

    private static SessionState Playing(long positionMs = 0, float speed = 1f, SessionActions actions = Basic, string? error = null) =>
        new(SessionPlaybackState.Playing, positionMs, speed, actions, error);

    private static SessionState Paused(long positionMs = 0) =>
        new(SessionPlaybackState.Paused, positionMs, 0f, Basic, null);

    [Fact]
    public void ShouldPublish_FirstState_IsPublished()
    {
        var rig = new Rig();

        Assert.True(rig.Gate.ShouldPublish(Playing()));
    }

    [Fact]
    public void ShouldPublish_FirstEmptyState_IsPublished()
    {
        var rig = new Rig();

        Assert.True(rig.Gate.ShouldPublish(SessionState.Empty));
    }

    [Fact]
    public void ShouldPublish_IdenticalStateRightAfter_IsNotPublished()
    {
        var rig = new Rig();
        rig.Gate.ShouldPublish(Playing(1_000));

        Assert.False(rig.Gate.ShouldPublish(Playing(1_000)));
    }

    [Fact]
    public void ShouldPublish_PositionAdvancedAsSpeedPredicts_IsNotPublished()
    {
        var rig = new Rig();
        rig.Gate.ShouldPublish(Playing(1_000));

        rig.Time.Advance(TimeSpan.FromSeconds(10));

        Assert.False(rig.Gate.ShouldPublish(Playing(11_000)));
    }

    [Fact]
    public void ShouldPublish_PositionAdvancedAsPredictedAtDoubleSpeed_IsNotPublished()
    {
        var rig = new Rig();
        rig.Gate.ShouldPublish(Playing(1_000, speed: 2f));

        rig.Time.Advance(TimeSpan.FromSeconds(10));

        Assert.False(rig.Gate.ShouldPublish(Playing(21_000, speed: 2f)));
    }

    [Fact]
    public void ShouldPublish_PausedPositionUnchangedForLongTime_IsNotPublished()
    {
        var rig = new Rig();
        rig.Gate.ShouldPublish(Paused(5_000));

        rig.Time.Advance(TimeSpan.FromMinutes(5));

        Assert.False(rig.Gate.ShouldPublish(Paused(5_000)));
    }

    [Fact]
    public void ShouldPublish_StateChanges_IsPublished()
    {
        var rig = new Rig();
        rig.Gate.ShouldPublish(Playing(1_000));

        Assert.True(rig.Gate.ShouldPublish(new SessionState(SessionPlaybackState.Buffering, 1_000, 1f, Basic, null)));
    }

    [Fact]
    public void ShouldPublish_ActionsChange_IsPublished()
    {
        var rig = new Rig();
        rig.Gate.ShouldPublish(Playing(1_000));

        Assert.True(rig.Gate.ShouldPublish(Playing(1_000, actions: Basic | SessionActions.SkipToNext)));
    }

    [Fact]
    public void ShouldPublish_SpeedChanges_IsPublished()
    {
        var rig = new Rig();
        rig.Gate.ShouldPublish(Playing(1_000, speed: 1f));

        Assert.True(rig.Gate.ShouldPublish(Playing(1_000, speed: 1.5f)));
    }

    [Fact]
    public void ShouldPublish_ErrorChanges_IsPublished()
    {
        var rig = new Rig();
        var error = new SessionState(SessionPlaybackState.Error, 0, 0f, Basic, "a");
        rig.Gate.ShouldPublish(error);

        Assert.True(rig.Gate.ShouldPublish(error with { Error = "b" }));
    }

    [Fact]
    public void ShouldPublish_ErrorNullToEmpty_IsPublished()
    {
        var rig = new Rig();
        rig.Gate.ShouldPublish(Playing(1_000));

        Assert.True(rig.Gate.ShouldPublish(Playing(1_000, error: string.Empty)));
    }

    [Fact]
    public void ShouldPublish_PlayingSeekForwardBeyondTolerance_IsPublished()
    {
        var rig = new Rig();
        rig.Gate.ShouldPublish(Playing(1_000));

        rig.Time.Advance(TimeSpan.FromSeconds(2));

        // Extrapolated 3000, next 4501: 1501 away.
        Assert.True(rig.Gate.ShouldPublish(Playing(4_501)));
    }

    [Fact]
    public void ShouldPublish_PlayingSeekBackwardBeyondTolerance_IsPublished()
    {
        var rig = new Rig();
        rig.Gate.ShouldPublish(Playing(10_000));

        rig.Time.Advance(TimeSpan.FromSeconds(2));

        // Extrapolated 12000, next 10499: 1501 away.
        Assert.True(rig.Gate.ShouldPublish(Playing(10_499)));
    }

    [Fact]
    public void ShouldPublish_PausedSeekForwardBeyondTolerance_IsPublished()
    {
        var rig = new Rig();
        rig.Gate.ShouldPublish(Paused(10_000));

        Assert.True(rig.Gate.ShouldPublish(Paused(11_501)));
    }

    [Fact]
    public void ShouldPublish_PausedSeekBackwardBeyondTolerance_IsPublished()
    {
        var rig = new Rig();
        rig.Gate.ShouldPublish(Paused(10_000));

        Assert.True(rig.Gate.ShouldPublish(Paused(8_499)));
    }

    [Fact]
    public void ShouldPublish_ExactlyAtToleranceForward_IsNotPublished()
    {
        // Pinned: only a distance strictly above 1500 ms publishes.
        var rig = new Rig();
        rig.Gate.ShouldPublish(Paused(10_000));

        Assert.False(rig.Gate.ShouldPublish(Paused(10_000 + SessionPublishGate.JumpToleranceMs)));
    }

    [Fact]
    public void ShouldPublish_ExactlyAtToleranceBackward_IsNotPublished()
    {
        var rig = new Rig();
        rig.Gate.ShouldPublish(Paused(10_000));

        Assert.False(rig.Gate.ShouldPublish(Paused(10_000 - SessionPublishGate.JumpToleranceMs)));
    }

    [Fact]
    public void ShouldPublish_OneMsBeyondTolerance_IsPublished()
    {
        var rig = new Rig();
        rig.Gate.ShouldPublish(Paused(10_000));

        Assert.True(rig.Gate.ShouldPublish(Paused(10_000 + SessionPublishGate.JumpToleranceMs + 1)));
    }

    [Fact]
    public void JumpToleranceMs_Constant_Is1500()
    {
        Assert.Equal(1500, SessionPublishGate.JumpToleranceMs);
    }

    [Fact]
    public void ShouldPublish_AfterPublishedJump_ExtrapolatesFromNewBase()
    {
        var rig = new Rig();
        rig.Gate.ShouldPublish(Playing(1_000));
        Assert.True(rig.Gate.ShouldPublish(Playing(50_000)));

        rig.Time.Advance(TimeSpan.FromSeconds(3));

        Assert.False(rig.Gate.ShouldPublish(Playing(53_000)));
    }

    [Fact]
    public void ShouldPublish_NotPublishedState_DoesNotMoveExtrapolationBase()
    {
        var rig = new Rig();
        rig.Gate.ShouldPublish(Playing(0));

        rig.Time.Advance(TimeSpan.FromSeconds(1));
        Assert.False(rig.Gate.ShouldPublish(Playing(1_000)));

        // Base is still (0, t0): 3 s later the prediction is 3000.
        rig.Time.Advance(TimeSpan.FromSeconds(2));

        Assert.False(rig.Gate.ShouldPublish(Playing(3_000)));
    }

    [Fact]
    public void Reset_NextStateIsPublishedEvenWhenIdentical()
    {
        var rig = new Rig();
        rig.Gate.ShouldPublish(Playing(1_000));

        rig.Gate.Reset();

        Assert.True(rig.Gate.ShouldPublish(Playing(1_000)));
    }

    [Fact]
    public void Reset_BeforeAnyPublication_DoesNotThrow()
    {
        var rig = new Rig();

        var ex = Record.Exception(rig.Gate.Reset);

        Assert.Null(ex);
    }

    [Fact]
    public void ShouldPublish_NullState_Throws()
    {
        var rig = new Rig();

        Assert.Throws<ArgumentNullException>(() => rig.Gate.ShouldPublish(null!));
    }
}
