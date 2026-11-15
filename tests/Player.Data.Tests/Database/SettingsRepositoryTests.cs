using Microsoft.Extensions.Time.Testing;
using Player.Data.Database;
using Player.Data.Tests.Support;

namespace Player.Data.Tests.Database;

public sealed class SettingsRepositoryTests : TempDatabaseTestBase
{
    private static readonly DateTimeOffset StartTime = new(2026, 1, 2, 3, 4, 5, TimeSpan.Zero);

    private static CancellationToken None => CancellationToken.None;

    private static async Task<long> ReadUpdatedUtcAsync(DbConnectionProvider provider, string key)
    {
        var connection = await OpenAsync(provider);
        return await connection.ExecuteScalarAsync<long>("SELECT updated_utc FROM settings WHERE key = ?", key);
    }

    private static async Task<int> CountRowsAsync(DbConnectionProvider provider)
    {
        var connection = await OpenAsync(provider);
        return await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM settings");
    }

    [Fact]
    public async Task GetJsonAsync_KeyMissing_ReturnsNull()
    {
        var repository = new SettingsRepository(CreateProvider());

        var value = await repository.GetJsonAsync("missing", None);

        Assert.Null(value);
    }

    [Fact]
    public async Task GetJsonAsync_AfterSet_ReturnsSameText()
    {
        var repository = new SettingsRepository(CreateProvider());
        await repository.SetJsonAsync("k", "{\"a\":1}", None);

        var value = await repository.GetJsonAsync("k", None);

        Assert.Equal("{\"a\":1}", value);
    }

    [Fact]
    public async Task GetJsonAsync_AfterSetWithHebrewText_ReturnsSameText()
    {
        var repository = new SettingsRepository(CreateProvider());
        const string hebrew = "{\"name\":\"שלום עולם\"}";
        await repository.SetJsonAsync("שפה", hebrew, None);

        var value = await repository.GetJsonAsync("שפה", None);

        Assert.Equal(hebrew, value);
    }

    [Fact]
    public async Task GetJsonAsync_AfterSetWith100000Characters_ReturnsSameText()
    {
        var repository = new SettingsRepository(CreateProvider());
        var large = new string('x', 50_000) + new string('ש', 50_000);
        await repository.SetJsonAsync("big", large, None);

        var value = await repository.GetJsonAsync("big", None);

        Assert.Equal(100_000, value!.Length);
        Assert.Equal(large, value);
    }

    [Fact]
    public async Task SetJsonAsync_SecondSetSameKey_ReplacesValue()
    {
        var repository = new SettingsRepository(CreateProvider());
        await repository.SetJsonAsync("k", "first", None);

        await repository.SetJsonAsync("k", "second", None);

        Assert.Equal("second", await repository.GetJsonAsync("k", None));
    }

    [Fact]
    public async Task SetJsonAsync_SecondSetSameKey_LeavesExactlyOneRow()
    {
        var provider = CreateProvider();
        var repository = new SettingsRepository(provider);
        await repository.SetJsonAsync("k", "first", None);

        await repository.SetJsonAsync("k", "second", None);

        Assert.Equal(1, await CountRowsAsync(provider));
    }

    [Fact]
    public async Task SetJsonAsync_TwoDifferentKeys_KeepValuesIndependent()
    {
        var provider = CreateProvider();
        var repository = new SettingsRepository(provider);

        await repository.SetJsonAsync("a", "one", None);
        await repository.SetJsonAsync("b", "two", None);

        Assert.Equal("one", await repository.GetJsonAsync("a", None));
        Assert.Equal("two", await repository.GetJsonAsync("b", None));
        Assert.Equal(2, await CountRowsAsync(provider));
    }

    [Fact]
    public async Task SetJsonAsync_FakeTimeProvider_StoresUnixSecondsOfInjectedTime()
    {
        var provider = CreateProvider();
        var repository = new SettingsRepository(provider, new FakeTimeProvider(StartTime));

        await repository.SetJsonAsync("k", "v", None);

        Assert.Equal(StartTime.ToUnixTimeSeconds(), await ReadUpdatedUtcAsync(provider, "k"));
    }

    [Fact]
    public async Task SetJsonAsync_LaterWriteAfterTimeAdvanced_UpdatesUpdatedUtc()
    {
        var provider = CreateProvider();
        var time = new FakeTimeProvider(StartTime);
        var repository = new SettingsRepository(provider, time);
        await repository.SetJsonAsync("k", "v", None);
        time.Advance(TimeSpan.FromSeconds(90));

        await repository.SetJsonAsync("k", "v2", None);

        Assert.Equal(StartTime.ToUnixTimeSeconds() + 90, await ReadUpdatedUtcAsync(provider, "k"));
    }

    [Fact]
    public async Task GetJsonAsync_NewProviderOnSameFile_ReturnsPreviouslyWrittenValue()
    {
        var firstProvider = CreateProvider();
        await new SettingsRepository(firstProvider).SetJsonAsync("k", "persisted שלום", None);
        var firstConnection = await OpenAsync(firstProvider);
        await firstConnection.CloseAsync();

        var secondProvider = CreateProvider();
        var value = await new SettingsRepository(secondProvider).GetJsonAsync("k", None);

        Assert.Equal("persisted שלום", value);
    }

    [Fact]
    public async Task SetJsonAsync_TokenAlreadyCancelled_ThrowsOperationCanceledException()
    {
        var repository = new SettingsRepository(CreateProvider());
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => repository.SetJsonAsync("k", "v", cts.Token));
    }

    [Fact]
    public async Task SetJsonAsync_TokenAlreadyCancelledOnOpenConnection_WritesNothing()
    {
        var provider = CreateProvider();
        var repository = new SettingsRepository(provider);
        await OpenAsync(provider);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => repository.SetJsonAsync("k", "v", cts.Token));

        Assert.Equal(0, await CountRowsAsync(provider));
    }

    [Fact]
    public async Task GetJsonAsync_TokenAlreadyCancelled_ThrowsOperationCanceledException()
    {
        var repository = new SettingsRepository(CreateProvider());
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => repository.GetJsonAsync("k", cts.Token));
    }
}
