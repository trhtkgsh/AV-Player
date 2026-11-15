using Player.Data.Database;
using Player.Data.Metadata;
using Player.Data.Tests.Support;

namespace Player.Data.Tests.Metadata;

public sealed class MediaMetadataRepositoryTests : TempDatabaseTestBase
{
    private static CancellationToken None => CancellationToken.None;

    private DbConnectionProvider CreateMetadataProvider() => CreateProvider(new MediaMetadataMigration());

    private static MediaMetadataRow MakeRow(string key, bool failed = false, string? thumbnail = "thumbs/a.jpg") => new()
    {
        FileKey = key,
        MediaStoreId = 42,
        DurationMs = 123_456,
        Width = 1920,
        Height = 1080,
        ThumbnailPath = thumbnail,
        Failed = failed,
        CreatedUtc = 1_700_000_000,
    };

    private static async Task SeedAsync(MediaMetadataRepository repository, int count)
    {
        for (var i = 0; i < count; i++)
        {
            await repository.UpsertAsync(MakeRow($"key-{i}"), None);
        }
    }

    private static async Task<int> CountRowsAsync(DbConnectionProvider provider)
    {
        var connection = await OpenAsync(provider);
        return await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM media_metadata");
    }

    private static IReadOnlyCollection<string> Keys(int from, int count) =>
        Enumerable.Range(from, count).Select(i => $"key-{i}").ToList();

    [Fact]
    public async Task Migration_Applied_CreatesExactColumns()
    {
        var provider = CreateMetadataProvider();
        var connection = await OpenAsync(provider);

        var columns = (await connection.GetTableInfoAsync("media_metadata")).Select(c => c.Name).ToList();

        Assert.Equal(
            new[] { "created_utc", "duration_ms", "failed", "file_key", "height", "media_store_id", "thumbnail_path", "width" },
            columns.Order(StringComparer.Ordinal).ToArray());
    }

    [Fact]
    public async Task Migration_Applied_CreatesIndexOnMediaStoreId()
    {
        var provider = CreateMetadataProvider();
        var connection = await OpenAsync(provider);

        var count = await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM sqlite_master WHERE type = 'index' AND tbl_name = 'media_metadata' AND sql LIKE '%(media_store_id)%'");

        Assert.Equal(1, count);
    }

    [Fact]
    public async Task Migration_AppliedTwice_DoesNotThrow()
    {
        var provider = CreateMetadataProvider();
        await OpenAsync(provider);

        var exception = await Record.ExceptionAsync(() => new MediaMetadataMigration().ApplyAsync(provider, None));

        Assert.Null(exception);
    }

    [Fact]
    public async Task UpsertAsync_ThenGetMany_RoundTripsEveryField()
    {
        var repository = new MediaMetadataRepository(CreateMetadataProvider());
        await repository.UpsertAsync(MakeRow("a"), None);

        var row = Assert.Single(await repository.GetManyAsync(new[] { "a" }, None));

        Assert.Equal("a", row.FileKey);
        Assert.Equal(42, row.MediaStoreId);
        Assert.Equal(123_456, row.DurationMs);
        Assert.Equal(1920, row.Width);
        Assert.Equal(1080, row.Height);
        Assert.Equal("thumbs/a.jpg", row.ThumbnailPath);
        Assert.False(row.Failed);
        Assert.Equal(1_700_000_000, row.CreatedUtc);
    }

    [Fact]
    public async Task UpsertAsync_NullThumbnailAndFailed_RoundTrips()
    {
        var repository = new MediaMetadataRepository(CreateMetadataProvider());
        await repository.UpsertAsync(MakeRow("a", failed: true, thumbnail: null), None);

        var row = Assert.Single(await repository.GetManyAsync(new[] { "a" }, None));

        Assert.Null(row.ThumbnailPath);
        Assert.True(row.Failed);
    }

    [Fact]
    public async Task UpsertAsync_ExistingKey_ReplacesValues()
    {
        var repository = new MediaMetadataRepository(CreateMetadataProvider());
        await repository.UpsertAsync(MakeRow("a"), None);
        var updated = MakeRow("a", failed: true, thumbnail: null);
        updated.DurationMs = 7;
        updated.Width = 10;
        updated.Height = 20;
        updated.MediaStoreId = 99;
        updated.CreatedUtc = 5;

        await repository.UpsertAsync(updated, None);

        var row = Assert.Single(await repository.GetManyAsync(new[] { "a" }, None));
        Assert.Equal(7, row.DurationMs);
        Assert.Equal(10, row.Width);
        Assert.Equal(20, row.Height);
        Assert.Equal(99, row.MediaStoreId);
        Assert.Equal(5, row.CreatedUtc);
        Assert.Null(row.ThumbnailPath);
        Assert.True(row.Failed);
    }

    [Fact]
    public async Task UpsertAsync_ExistingKey_LeavesOneRow()
    {
        var provider = CreateMetadataProvider();
        var repository = new MediaMetadataRepository(provider);
        await repository.UpsertAsync(MakeRow("a"), None);

        await repository.UpsertAsync(MakeRow("a", failed: true), None);

        Assert.Equal(1, await CountRowsAsync(provider));
    }

    [Fact]
    public async Task GetManyAsync_UnknownKey_ReturnsEmpty()
    {
        var repository = new MediaMetadataRepository(CreateMetadataProvider());

        var rows = await repository.GetManyAsync(new[] { "missing" }, None);

        Assert.Empty(rows);
    }

    [Fact]
    public async Task GetManyAsync_EmptyKeyList_ReturnsEmpty()
    {
        var repository = new MediaMetadataRepository(CreateMetadataProvider());
        await repository.UpsertAsync(MakeRow("a"), None);

        var rows = await repository.GetManyAsync(Array.Empty<string>(), None);

        Assert.Empty(rows);
    }

    [Fact]
    public async Task GetManyAsync_MixOfKnownAndUnknownKeys_SkipsUnknown()
    {
        var repository = new MediaMetadataRepository(CreateMetadataProvider());
        await repository.UpsertAsync(MakeRow("a"), None);
        await repository.UpsertAsync(MakeRow("b"), None);

        var rows = await repository.GetManyAsync(new[] { "a", "missing", "b", "other" }, None);

        Assert.Equal(new[] { "a", "b" }, rows.Select(r => r.FileKey).Order(StringComparer.Ordinal).ToArray());
    }

    [Fact]
    public async Task GetManyAsync_1200Keys_Returns1200Rows()
    {
        var repository = new MediaMetadataRepository(CreateMetadataProvider());
        await SeedAsync(repository, 1200);

        var rows = await repository.GetManyAsync(Keys(0, 1200), None);

        Assert.Equal(1200, rows.Count);
        Assert.Equal(1200, rows.Select(r => r.FileKey).Distinct().Count());
    }

    [Fact]
    public async Task DeleteNotInAsync_SomeKept_DeletesAndReturnsOnlyOutsideRows()
    {
        var provider = CreateMetadataProvider();
        var repository = new MediaMetadataRepository(provider);
        await repository.UpsertAsync(MakeRow("a"), None);
        await repository.UpsertAsync(MakeRow("b"), None);
        await repository.UpsertAsync(MakeRow("c"), None);

        var deleted = await repository.DeleteNotInAsync(new[] { "a", "c", "unknown" }, None);

        Assert.Equal("b", Assert.Single(deleted).FileKey);
        var remaining = await repository.GetManyAsync(new[] { "a", "b", "c" }, None);
        Assert.Equal(new[] { "a", "c" }, remaining.Select(r => r.FileKey).Order(StringComparer.Ordinal).ToArray());
    }

    [Fact]
    public async Task DeleteNotInAsync_1200RowsAnd1100Kept_Deletes100()
    {
        var provider = CreateMetadataProvider();
        var repository = new MediaMetadataRepository(provider);
        await SeedAsync(repository, 1200);

        var deleted = await repository.DeleteNotInAsync(Keys(0, 1100), None);

        Assert.Equal(100, deleted.Count);
        Assert.Equal(Keys(1100, 100).Order(StringComparer.Ordinal).ToArray(),
            deleted.Select(r => r.FileKey).Order(StringComparer.Ordinal).ToArray());
        Assert.Equal(1100, await CountRowsAsync(provider));
    }

    [Fact]
    public async Task DeleteNotInAsync_EmptyKeepSet_DeletesEverything()
    {
        var provider = CreateMetadataProvider();
        var repository = new MediaMetadataRepository(provider);
        await SeedAsync(repository, 3);

        var deleted = await repository.DeleteNotInAsync(Array.Empty<string>(), None);

        Assert.Equal(3, deleted.Count);
        Assert.Equal(0, await CountRowsAsync(provider));
    }

    [Fact]
    public async Task DeleteNotInAsync_AllKept_DeletesNothing()
    {
        var provider = CreateMetadataProvider();
        var repository = new MediaMetadataRepository(provider);
        await SeedAsync(repository, 3);

        var deleted = await repository.DeleteNotInAsync(Keys(0, 3), None);

        Assert.Empty(deleted);
        Assert.Equal(3, await CountRowsAsync(provider));
    }

    [Fact]
    public async Task ListFailedAsync_MixedRows_ReturnsOnlyFailed()
    {
        var repository = new MediaMetadataRepository(CreateMetadataProvider());
        await repository.UpsertAsync(MakeRow("ok1"), None);
        await repository.UpsertAsync(MakeRow("bad1", failed: true), None);
        await repository.UpsertAsync(MakeRow("ok2"), None);
        await repository.UpsertAsync(MakeRow("bad2", failed: true, thumbnail: null), None);

        var failed = await repository.ListFailedAsync(None);

        Assert.Equal(new[] { "bad1", "bad2" }, failed.Select(r => r.FileKey).Order(StringComparer.Ordinal).ToArray());
        Assert.All(failed, r => Assert.True(r.Failed));
    }

    [Fact]
    public async Task ListFailedAsync_NoFailedRows_ReturnsEmpty()
    {
        var repository = new MediaMetadataRepository(CreateMetadataProvider());
        await repository.UpsertAsync(MakeRow("ok"), None);

        var failed = await repository.ListFailedAsync(None);

        Assert.Empty(failed);
    }
}
