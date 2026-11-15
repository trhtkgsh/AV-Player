using Player.Data.Tests.Support;
using SQLite;

namespace Player.Data.Tests.Database;

public sealed class DbConnectionProviderTests : TempDatabaseTestBase
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    [Fact]
    public async Task GetConnectionAsync_CalledTwice_ReturnsSameConnectionInstance()
    {
        var provider = CreateProvider();

        var first = await provider.GetConnectionAsync(CancellationToken.None);
        var second = await provider.GetConnectionAsync(CancellationToken.None);

        Assert.IsType<SQLiteAsyncConnection>(first);
        Assert.Same(first, second);
    }

    [Fact]
    public async Task GetConnectionAsync_FirstCall_CreatesDatabaseFileAtGivenPath()
    {
        var provider = CreateProvider();

        await provider.GetConnectionAsync(CancellationToken.None);

        Assert.True(File.Exists(DatabasePath));
    }

    [Fact]
    public async Task GetConnectionAsync_FirstCall_CreatesSettingsTableWithExpectedColumns()
    {
        var provider = CreateProvider();

        var connection = await OpenAsync(provider);
        var columns = (await connection.GetTableInfoAsync("settings")).Select(c => c.Name).ToList();

        Assert.Equal(new[] { "json", "key", "updated_utc" }, columns.OrderBy(n => n, StringComparer.Ordinal));
    }

    [Fact]
    public async Task GetConnectionAsync_MigrationsRegisteredOutOfOrder_RunsThemInAscendingOrder()
    {
        var log = new List<int>();
        var provider = CreateProvider(
            new RecordingMigration(30, log),
            new RecordingMigration(10, log),
            new RecordingMigration(20, log));

        await provider.GetConnectionAsync(CancellationToken.None);

        Assert.Equal(new[] { 10, 20, 30 }, log);
    }

    [Fact]
    public async Task GetConnectionAsync_CalledManyTimes_RunsEachMigrationOnce()
    {
        var log = new List<int>();
        var first = new RecordingMigration(10, log);
        var second = new RecordingMigration(20, log);
        var provider = CreateProvider(first, second);

        for (var i = 0; i < 5; i++)
        {
            await provider.GetConnectionAsync(CancellationToken.None);
        }

        Assert.Equal(1, first.RunCount);
        Assert.Equal(1, second.RunCount);
    }

    [Fact]
    public async Task GetConnectionAsync_TwentyConcurrentFirstCalls_RunsEachMigrationOnce()
    {
        var log = new List<int>();
        var first = new RecordingMigration(10, log);
        var second = new RecordingMigration(20, log);
        var provider = CreateProvider(first, second);

        var connections = await Task.WhenAll(
            Enumerable.Range(0, 20).Select(_ => Task.Run(() => provider.GetConnectionAsync(CancellationToken.None))))
            .WaitAsync(Timeout);

        Assert.Equal(1, first.RunCount);
        Assert.Equal(1, second.RunCount);
        Assert.Equal(new[] { 10, 20 }, log);
        Assert.Equal(20, connections.Length);
    }

    [Fact]
    public async Task GetConnectionAsync_TwentyConcurrentFirstCalls_ReturnsSameConnectionToAll()
    {
        var provider = CreateProvider(new RecordingMigration(10, new List<int>()));

        var connections = await Task.WhenAll(
            Enumerable.Range(0, 20).Select(_ => Task.Run(() => provider.GetConnectionAsync(CancellationToken.None))))
            .WaitAsync(Timeout);

        Assert.All(connections, c => Assert.Same(connections[0], c));
    }

    [Fact]
    public async Task GetConnectionAsync_MigrationRequestsConnectionFromProvider_GetsOpenConnectionWithoutDeadlock()
    {
        object? seen = null;
        var migration = new RecordingMigration(10, new List<int>(), async db =>
        {
            seen = await db.GetConnectionAsync(CancellationToken.None);
        });
        var provider = CreateProvider(migration);

        var connection = await provider.GetConnectionAsync(CancellationToken.None).WaitAsync(Timeout);

        Assert.Same(connection, seen);
    }

    [Fact]
    public async Task GetConnectionAsync_MigrationThrows_ThrowsToCaller()
    {
        var provider = CreateProvider(new RecordingMigration(
            10, new List<int>(), _ => throw new InvalidOperationException("migration failed")));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => provider.GetConnectionAsync(CancellationToken.None));

        Assert.Equal("migration failed", ex.Message);
    }

    [Fact]
    public async Task GetConnectionAsync_MigrationThrewBefore_ThrowsAgainWithoutRerunningMigration()
    {
        var migration = new RecordingMigration(
            10, new List<int>(), _ => throw new InvalidOperationException("migration failed"));
        var provider = CreateProvider(migration);
        await Assert.ThrowsAsync<InvalidOperationException>(() => provider.GetConnectionAsync(CancellationToken.None));

        await Assert.ThrowsAsync<InvalidOperationException>(() => provider.GetConnectionAsync(CancellationToken.None));

        Assert.Equal(1, migration.RunCount);
    }

    [Fact]
    public async Task GetConnectionAsync_TokenAlreadyCancelled_ThrowsOperationCanceledException()
    {
        var provider = CreateProvider();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => provider.GetConnectionAsync(cts.Token));
    }

    [Fact]
    public async Task GetConnectionAsync_FirstCall_EnablesWriteAheadLogging()
    {
        var provider = CreateProvider();

        var connection = await OpenAsync(provider);
        var mode = await connection.ExecuteScalarAsync<string>("PRAGMA journal_mode");

        Assert.Equal("wal", mode);
    }
}
