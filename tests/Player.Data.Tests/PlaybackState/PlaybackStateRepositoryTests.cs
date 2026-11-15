using Player.Core.Models;
using Player.Data.Database;
using Player.Data.PlaybackStateStorage;
using Player.Data.Tests.Support;

namespace Player.Data.Tests.PlaybackStateStorage;

public sealed class PlaybackStateRepositoryTests : TempDatabaseTestBase
{
    private static CancellationToken None => CancellationToken.None;

    private static readonly MediaFileId Id1 = new(1, 1_000, 1_700_000_000);
    private static readonly MediaFileId Id2 = new(2, 2_000, 1_700_000_001);

    private DbConnectionProvider CreateStateProvider() => CreateProvider(new PlaybackStateMigration());

    private static PlaybackState MakeFull(MediaFileId id) => new(id)
    {
        PositionMs = 12_345,
        DurationMs = 600_000,
        Finished = true,
        LastPlayedUtc = 1_700_000_500,
        Speed = 1.25f,
        Decoder = DecoderMode.Software,
        Aspect = AspectMode.Crop,
        AudioTrackId = 3,
        AudioDelayMs = -250,
        SubtitleTrackId = 2,
        SubtitleDelayMs = 400,
        ExternalSubtitleUri = "content://subs/1.srt",
    };

    private static async Task<int> CountRowsAsync(DbConnectionProvider provider, string table)
    {
        var connection = await OpenAsync(provider);
        return await connection.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM {table}");
    }

    private static async Task InsertRawAsync(DbConnectionProvider provider, string key, string? overridesJson)
    {
        var connection = await OpenAsync(provider);
        await connection.ExecuteAsync(
            "INSERT INTO playback_state (file_key, media_store_id, position_ms, duration_ms, finished, last_played_utc, overrides_json) " +
            "VALUES (?, 1, 777, 5000, 0, 10, ?)",
            key,
            overridesJson);
    }

    private static CancellationToken Cancelled()
    {
        var source = new CancellationTokenSource();
        source.Cancel();
        return source.Token;
    }

    [Fact]
    public async Task Migration_Applied_CreatesExactColumns()
    {
        var provider = CreateStateProvider();
        var connection = await OpenAsync(provider);

        var columns = (await connection.GetTableInfoAsync("playback_state")).Select(c => c.Name).ToList();

        Assert.Equal(
            new[] { "duration_ms", "file_key", "finished", "last_played_utc", "media_store_id", "overrides_json", "position_ms" },
            columns.Order(StringComparer.Ordinal).ToArray());
    }

    [Fact]
    public async Task Migration_Applied_CreatesIndexOnMediaStoreId()
    {
        var provider = CreateStateProvider();
        var connection = await OpenAsync(provider);

        var count = await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM sqlite_master WHERE type = 'index' AND tbl_name = 'playback_state' AND sql LIKE '%(media_store_id)%'");

        Assert.Equal(1, count);
    }

    [Fact]
    public async Task Migration_AppliedTwice_DoesNotThrow()
    {
        var provider = CreateStateProvider();
        await OpenAsync(provider);

        var exception = await Record.ExceptionAsync(() => new PlaybackStateMigration().ApplyAsync(provider, None));

        Assert.Null(exception);
    }

    [Fact]
    public async Task GetAsync_UnknownId_ReturnsNull()
    {
        var repository = new PlaybackStateRepository(CreateStateProvider());

        var state = await repository.GetAsync(Id1, None);

        Assert.Null(state);
    }

    [Fact]
    public async Task SaveAsync_AllFieldsSet_RoundTripsEqualRecord()
    {
        var repository = new PlaybackStateRepository(CreateStateProvider());
        var saved = MakeFull(Id1);

        await repository.SaveAsync(saved, None);
        var loaded = await repository.GetAsync(Id1, None);

        Assert.Equal(saved, loaded);
    }

    [Fact]
    public async Task SaveAsync_AllFieldsSet_ReadsEveryMemberBack()
    {
        var repository = new PlaybackStateRepository(CreateStateProvider());
        await repository.SaveAsync(MakeFull(Id1), None);

        var loaded = await repository.GetAsync(Id1, None);

        Assert.NotNull(loaded);
        Assert.Equal(Id1, loaded.Id);
        Assert.Equal(12_345, loaded.PositionMs);
        Assert.Equal(600_000, loaded.DurationMs);
        Assert.Equal(1_700_000_500, loaded.LastPlayedUtc);
        Assert.Equal(1.25f, loaded.Speed);
        Assert.Equal(DecoderMode.Software, loaded.Decoder);
        Assert.Equal(AspectMode.Crop, loaded.Aspect);
        Assert.Equal(3, loaded.AudioTrackId);
        Assert.Equal(-250, loaded.AudioDelayMs);
        Assert.Equal(2, loaded.SubtitleTrackId);
        Assert.Equal(400, loaded.SubtitleDelayMs);
        Assert.Equal("content://subs/1.srt", loaded.ExternalSubtitleUri);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task SaveAsync_FinishedValue_RoundTrips(bool finished)
    {
        var repository = new PlaybackStateRepository(CreateStateProvider());
        await repository.SaveAsync(new PlaybackState(Id1) { Finished = finished }, None);

        var loaded = await repository.GetAsync(Id1, None);

        Assert.NotNull(loaded);
        Assert.Equal(finished, loaded.Finished);
    }

    [Fact]
    public async Task SaveAsync_NoOverrides_ReadsBackAllOverridesNull()
    {
        var repository = new PlaybackStateRepository(CreateStateProvider());
        var saved = new PlaybackState(Id1) { PositionMs = 5, DurationMs = 10, LastPlayedUtc = 3 };
        await repository.SaveAsync(saved, None);

        var loaded = await repository.GetAsync(Id1, None);

        Assert.Equal(saved, loaded);
        Assert.NotNull(loaded);
        Assert.Null(loaded.Speed);
        Assert.Null(loaded.Decoder);
        Assert.Null(loaded.Aspect);
        Assert.Null(loaded.AudioTrackId);
        Assert.Null(loaded.AudioDelayMs);
        Assert.Null(loaded.SubtitleTrackId);
        Assert.Null(loaded.SubtitleDelayMs);
        Assert.Null(loaded.ExternalSubtitleUri);
    }

    [Fact]
    public async Task SaveAsync_NoOverrides_StoresSqlNullInOverridesColumn()
    {
        var provider = CreateStateProvider();
        var repository = new PlaybackStateRepository(provider);
        await repository.SaveAsync(new PlaybackState(Id1) { PositionMs = 5 }, None);

        var connection = await OpenAsync(provider);
        var nullCount = await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM playback_state WHERE overrides_json IS NULL");

        Assert.Equal(1, nullCount);
    }

    [Fact]
    public async Task SaveAsync_SubtitleTrackMinusOne_SurvivesAsMinusOne()
    {
        var repository = new PlaybackStateRepository(CreateStateProvider());
        await repository.SaveAsync(new PlaybackState(Id1) { SubtitleTrackId = -1 }, None);

        var loaded = await repository.GetAsync(Id1, None);

        Assert.Equal(-1, loaded?.SubtitleTrackId);
    }

    [Fact]
    public async Task SaveAsync_AudioDelayZero_SurvivesAsZero()
    {
        var repository = new PlaybackStateRepository(CreateStateProvider());
        await repository.SaveAsync(new PlaybackState(Id1) { AudioDelayMs = 0 }, None);

        var loaded = await repository.GetAsync(Id1, None);

        Assert.Equal(0L, loaded?.AudioDelayMs);
    }

    [Fact]
    public async Task SaveAsync_SubtitleDelayZero_SurvivesAsZero()
    {
        var repository = new PlaybackStateRepository(CreateStateProvider());
        await repository.SaveAsync(new PlaybackState(Id1) { SubtitleDelayMs = 0 }, None);

        var loaded = await repository.GetAsync(Id1, None);

        Assert.Equal(0L, loaded?.SubtitleDelayMs);
    }

    [Fact]
    public async Task SaveAsync_AudioTrackZero_SurvivesAsZero()
    {
        var repository = new PlaybackStateRepository(CreateStateProvider());
        await repository.SaveAsync(new PlaybackState(Id1) { AudioTrackId = 0 }, None);

        var loaded = await repository.GetAsync(Id1, None);

        Assert.Equal(0, loaded?.AudioTrackId);
    }

    [Theory]
    [InlineData(0.25f)]
    [InlineData(1.25f)]
    [InlineData(1.05f)]
    [InlineData(3.95f)]
    [InlineData(4f)]
    public async Task SaveAsync_Speed_RoundTripsExactly(float speed)
    {
        var repository = new PlaybackStateRepository(CreateStateProvider());
        await repository.SaveAsync(new PlaybackState(Id1) { Speed = speed }, None);

        var loaded = await repository.GetAsync(Id1, None);

        Assert.Equal(speed, loaded?.Speed);
    }

    [Fact]
    public async Task SaveAsync_ExternalSubtitleUriWithSpecialCharacters_RoundTrips()
    {
        var uri = "C:\\subs\\\"quoted\" \u05E9\u05DC\u05D5\u05DD \u05E2\u05D5\u05DC\u05DD.srt";
        var repository = new PlaybackStateRepository(CreateStateProvider());
        await repository.SaveAsync(new PlaybackState(Id1) { ExternalSubtitleUri = uri }, None);

        var loaded = await repository.GetAsync(Id1, None);

        Assert.Equal(uri, loaded?.ExternalSubtitleUri);
    }

    [Fact]
    public async Task SaveAsync_SameIdTwice_KeepsOneRow()
    {
        var provider = CreateStateProvider();
        var repository = new PlaybackStateRepository(provider);
        await repository.SaveAsync(new PlaybackState(Id1) { PositionMs = 1 }, None);
        await repository.SaveAsync(new PlaybackState(Id1) { PositionMs = 2 }, None);

        Assert.Equal(1, await CountRowsAsync(provider, "playback_state"));
    }

    [Fact]
    public async Task SaveAsync_SameIdTwice_ReadsNewValues()
    {
        var repository = new PlaybackStateRepository(CreateStateProvider());
        await repository.SaveAsync(new PlaybackState(Id1) { PositionMs = 1, Finished = false }, None);
        var second = new PlaybackState(Id1) { PositionMs = 2, DurationMs = 9, Finished = true, LastPlayedUtc = 8 };
        await repository.SaveAsync(second, None);

        var loaded = await repository.GetAsync(Id1, None);

        Assert.Equal(second, loaded);
    }

    [Fact]
    public async Task SaveAsync_OverrideSetThenCleared_ReadsBackNull()
    {
        var repository = new PlaybackStateRepository(CreateStateProvider());
        await repository.SaveAsync(new PlaybackState(Id1) { Speed = 2f, SubtitleTrackId = 4 }, None);
        await repository.SaveAsync(new PlaybackState(Id1) { Speed = null, SubtitleTrackId = 4 }, None);

        var loaded = await repository.GetAsync(Id1, None);

        Assert.NotNull(loaded);
        Assert.Null(loaded.Speed);
        Assert.Equal(4, loaded.SubtitleTrackId);
    }

    [Fact]
    public async Task GetAsync_OverridesJsonIsNotJson_ReturnsPositionWithNullOverrides()
    {
        var provider = CreateStateProvider();
        await InsertRawAsync(provider, Id1.ToKey(), "not json");
        var repository = new PlaybackStateRepository(provider);

        var loaded = await repository.GetAsync(Id1, None);

        Assert.NotNull(loaded);
        Assert.Equal(777, loaded.PositionMs);
        Assert.Null(loaded.Speed);
        Assert.Null(loaded.Decoder);
        Assert.Null(loaded.Aspect);
        Assert.Null(loaded.AudioTrackId);
        Assert.Null(loaded.AudioDelayMs);
        Assert.Null(loaded.SubtitleTrackId);
        Assert.Null(loaded.SubtitleDelayMs);
        Assert.Null(loaded.ExternalSubtitleUri);
    }

    [Fact]
    public async Task GetAsync_OverridesJsonHasUnknownMember_KeepsKnownSpeed()
    {
        var provider = CreateStateProvider();
        await InsertRawAsync(provider, Id1.ToKey(), "{\"Speed\":1.5,\"Decoder\":\"Gpu\",\"Unknown\":7}");
        var repository = new PlaybackStateRepository(provider);

        var loaded = await repository.GetAsync(Id1, None);

        Assert.NotNull(loaded);
        Assert.Equal(1.5f, loaded.Speed);
    }

    [Fact]
    public async Task GetAsync_OverridesJsonHasUnknownDecoderName_ReadsDecoderNull()
    {
        var provider = CreateStateProvider();
        await InsertRawAsync(provider, Id1.ToKey(), "{\"Speed\":1.5,\"Decoder\":\"Gpu\",\"Unknown\":7}");
        var repository = new PlaybackStateRepository(provider);

        var loaded = await repository.GetAsync(Id1, None);

        Assert.NotNull(loaded);
        Assert.Null(loaded.Decoder);
    }

    [Fact]
    public async Task GetManyAsync_RowWithGarbageKey_IsSkippedWithoutThrowing()
    {
        var provider = CreateStateProvider();
        await InsertRawAsync(provider, "garbage", null);
        var repository = new PlaybackStateRepository(provider);
        await repository.SaveAsync(new PlaybackState(Id1) { PositionMs = 9 }, None);

        var result = await repository.GetManyAsync(new[] { Id1 }, None);

        Assert.Single(result);
        Assert.Equal(9, result[Id1].PositionMs);
    }

    [Fact]
    public async Task GetManyAsync_EmptyList_ReturnsEmptyDictionary()
    {
        var repository = new PlaybackStateRepository(CreateStateProvider());

        var result = await repository.GetManyAsync(Array.Empty<MediaFileId>(), None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetManyAsync_UnknownIds_AreAbsent()
    {
        var repository = new PlaybackStateRepository(CreateStateProvider());
        await repository.SaveAsync(new PlaybackState(Id1) { PositionMs = 1 }, None);

        var result = await repository.GetManyAsync(new[] { Id1, Id2 }, None);

        Assert.Single(result);
        Assert.True(result.ContainsKey(Id1));
        Assert.False(result.ContainsKey(Id2));
    }

    [Fact]
    public async Task GetManyAsync_DuplicateIds_ReturnsEntryOnce()
    {
        var repository = new PlaybackStateRepository(CreateStateProvider());
        await repository.SaveAsync(new PlaybackState(Id1) { PositionMs = 1 }, None);

        var result = await repository.GetManyAsync(new[] { Id1, Id1, Id1 }, None);

        Assert.Single(result);
        Assert.Equal(1, result[Id1].PositionMs);
    }

    [Fact]
    public async Task GetManyAsync_MoreThanOneChunkOfIds_ReturnsAllWithRightPositions()
    {
        const int count = 1_200;
        var provider = CreateStateProvider();
        var repository = new PlaybackStateRepository(provider);
        var ids = Enumerable.Range(0, count).Select(i => new MediaFileId(i, 100 + i, 5_000 + i)).ToList();
        foreach (var id in ids)
        {
            await repository.SaveAsync(new PlaybackState(id) { PositionMs = id.MediaStoreId * 10 }, None);
        }

        var result = await repository.GetManyAsync(ids, None);

        Assert.Equal(count, result.Count);
        Assert.All(ids, id => Assert.Equal(id.MediaStoreId * 10, result[id].PositionMs));
    }

    [Fact]
    public async Task DeleteAsync_ExistingId_RemovesOnlyThatId()
    {
        var repository = new PlaybackStateRepository(CreateStateProvider());
        await repository.SaveAsync(new PlaybackState(Id1), None);
        await repository.SaveAsync(new PlaybackState(Id2), None);

        await repository.DeleteAsync(Id1, None);

        Assert.Null(await repository.GetAsync(Id1, None));
        Assert.NotNull(await repository.GetAsync(Id2, None));
    }

    [Fact]
    public async Task DeleteAsync_MissingId_DoesNotThrow()
    {
        var repository = new PlaybackStateRepository(CreateStateProvider());

        var exception = await Record.ExceptionAsync(() => repository.DeleteAsync(Id1, None));

        Assert.Null(exception);
    }

    [Fact]
    public async Task GetAsync_CancelledToken_ThrowsOperationCanceled()
    {
        var repository = new PlaybackStateRepository(CreateStateProvider());

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => repository.GetAsync(Id1, Cancelled()));
    }

    [Fact]
    public async Task GetManyAsync_CancelledToken_ThrowsOperationCanceled()
    {
        var repository = new PlaybackStateRepository(CreateStateProvider());

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => repository.GetManyAsync(new[] { Id1 }, Cancelled()));
    }

    [Fact]
    public async Task SaveAsync_CancelledToken_ThrowsOperationCanceled()
    {
        var repository = new PlaybackStateRepository(CreateStateProvider());

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => repository.SaveAsync(new PlaybackState(Id1), Cancelled()));
    }

    [Fact]
    public async Task SaveAsync_CancelledToken_WritesNothing()
    {
        var provider = CreateStateProvider();
        await OpenAsync(provider);
        var repository = new PlaybackStateRepository(provider);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => repository.SaveAsync(new PlaybackState(Id1), Cancelled()));

        Assert.Equal(0, await CountRowsAsync(provider, "playback_state"));
    }

    [Fact]
    public async Task DeleteAsync_CancelledToken_ThrowsOperationCanceled()
    {
        var repository = new PlaybackStateRepository(CreateStateProvider());

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => repository.DeleteAsync(Id1, Cancelled()));
    }

    [Fact]
    public void Constructor_NullProvider_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new PlaybackStateRepository(null!));
    }

    [Fact]
    public async Task GetManyAsync_NullIds_ThrowsArgumentNullException()
    {
        var repository = new PlaybackStateRepository(CreateStateProvider());

        await Assert.ThrowsAsync<ArgumentNullException>(() => repository.GetManyAsync(null!, None));
    }

    [Fact]
    public async Task SaveAsync_NullState_ThrowsArgumentNullException()
    {
        var repository = new PlaybackStateRepository(CreateStateProvider());

        await Assert.ThrowsAsync<ArgumentNullException>(() => repository.SaveAsync(null!, None));
    }

    [Fact]
    public async Task SaveAsync_SeveralSaves_LeavesSettingsTableEmpty()
    {
        var provider = CreateStateProvider();
        var repository = new PlaybackStateRepository(provider);
        await repository.SaveAsync(MakeFull(Id1), None);
        await repository.SaveAsync(MakeFull(Id2), None);
        await repository.SaveAsync(new PlaybackState(Id1) { PositionMs = 3 }, None);

        Assert.Equal(0, await CountRowsAsync(provider, "settings"));
    }
}
