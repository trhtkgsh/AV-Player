using Player.Core.Models;
using Player.Data.Database;
using Player.Data.FolderSettingsStorage;
using Player.Data.Tests.Support;

namespace Player.Data.Tests.FolderSettingsStorageTests;

public sealed class FolderSettingsStoreTests : TempDatabaseTestBase
{
    private static CancellationToken None => CancellationToken.None;

    private static CancellationToken Cancelled
    {
        get
        {
            using var source = new CancellationTokenSource();
            source.Cancel();
            return source.Token;
        }
    }

    private SettingsRepository _repository = null!;
    private DbConnectionProvider? _provider;

    private FolderSettingsStore NewStore()
    {
        _provider ??= CreateProvider();
        _repository = new SettingsRepository(_provider);
        return new FolderSettingsStore(_repository);
    }

    private async Task StoreRawAsync(string json)
    {
        NewStore();
        await _repository.SetJsonAsync(FolderSettingsStore.SettingsKey, json, None);
    }

    private async Task<IReadOnlyDictionary<long, FolderSettings>> LoadRawAsync(string json)
    {
        await StoreRawAsync(json);
        return await NewStore().GetAllAsync(None);
    }

    private Task<string?> RawRowAsync() => _repository.GetJsonAsync(FolderSettingsStore.SettingsKey, None);

    private static FolderSettings Folder(long id, float? speed = null, bool favorite = false) =>
        new(id) { Speed = speed, Favorite = favorite };

    // ------------------------------------------------------------ Empty store

    [Fact]
    public async Task GetAsync_EmptyStore_ReturnsNull()
    {
        var store = NewStore();

        Assert.Null(await store.GetAsync(7, None));
    }

    [Fact]
    public async Task GetAllAsync_EmptyStore_ReturnsEmpty()
    {
        var store = NewStore();

        Assert.Empty(await store.GetAllAsync(None));
    }

    // ------------------------------------------------------------ Round trip

    [Fact]
    public async Task GetAsync_AfterSavingSpeedOnly_ReturnsTheSpeedAndNoFavorite()
    {
        var store = NewStore();
        await store.SaveAsync(Folder(7, speed: 1.5f), None);

        var loaded = await store.GetAsync(7, None);

        Assert.Equal(Folder(7, speed: 1.5f), loaded);
        Assert.False(loaded!.Favorite);
    }

    [Fact]
    public async Task GetAsync_AfterSavingFavoriteOnly_ReturnsFavoriteAndNoSpeed()
    {
        var store = NewStore();
        await store.SaveAsync(Folder(7, favorite: true), None);

        var loaded = await store.GetAsync(7, None);

        Assert.Equal(Folder(7, favorite: true), loaded);
        Assert.Null(loaded!.Speed);
    }

    [Fact]
    public async Task GetAsync_AfterSavingSpeedAndFavorite_ReturnsBoth()
    {
        var store = NewStore();
        await store.SaveAsync(Folder(7, 2.25f, true), None);

        Assert.Equal(Folder(7, 2.25f, true), await store.GetAsync(7, None));
    }

    [Fact]
    public async Task GetAsync_SaveTwiceForSameFolder_KeepsTheLastValue()
    {
        var store = NewStore();
        await store.SaveAsync(Folder(7, 2f, true), None);
        await store.SaveAsync(Folder(7, 0.5f), None);

        Assert.Equal(Folder(7, 0.5f), await store.GetAsync(7, None));
    }

    [Fact]
    public async Task GetAsync_OtherFolder_ReturnsNull()
    {
        var store = NewStore();
        await store.SaveAsync(Folder(7, 2f), None);

        Assert.Null(await store.GetAsync(8, None));
    }

    [Fact]
    public async Task GetAsync_SecondStoreOnTheSameFile_ReadsWhatTheFirstSaved()
    {
        var first = NewStore();
        await first.SaveAsync(Folder(1, 1.5f), None);
        await first.SaveAsync(Folder(2, favorite: true), None);
        await first.SaveAsync(Folder(3, 3f, true), None);

        var second = NewStore();

        Assert.Equal(Folder(1, 1.5f), await second.GetAsync(1, None));
        Assert.Equal(Folder(2, favorite: true), await second.GetAsync(2, None));
        Assert.Equal(Folder(3, 3f, true), await second.GetAsync(3, None));
        Assert.Equal(3, (await second.GetAllAsync(None)).Count);
    }

    // ------------------------------------------------------------ Removal

    [Fact]
    public async Task SaveAsync_NoSpeedAndNoFavorite_RemovesTheEntry()
    {
        var store = NewStore();
        await store.SaveAsync(Folder(7, 2f, true), None);

        await store.SaveAsync(Folder(7), None);

        Assert.Null(await store.GetAsync(7, None));
        Assert.Empty(await store.GetAllAsync(None));
        Assert.Null(await NewStore().GetAsync(7, None));
    }

    [Fact]
    public async Task SaveAsync_NoSpeedAndNoFavorite_RowNoLongerContainsTheKey()
    {
        var store = NewStore();
        await store.SaveAsync(Folder(7, 2f), None);
        await store.SaveAsync(Folder(8, favorite: true), None);
        Assert.Contains("\"7\"", await RawRowAsync());

        await store.SaveAsync(Folder(7), None);

        var row = await RawRowAsync();
        Assert.DoesNotContain("\"7\"", row);
        Assert.Contains("\"8\"", row);
    }

    [Fact]
    public async Task SaveAsync_RemoveOneOfTwoEntries_KeepsTheOther()
    {
        var store = NewStore();
        await store.SaveAsync(Folder(1, 2f), None);
        await store.SaveAsync(Folder(2, favorite: true), None);

        await store.SaveAsync(Folder(1), None);

        var all = await NewStore().GetAllAsync(None);
        Assert.Equal(Folder(2, favorite: true), Assert.Single(all).Value);
    }

    [Fact]
    public async Task SaveAsync_EmptyEntryForUnknownFolder_StoresNothing()
    {
        var store = NewStore();

        await store.SaveAsync(Folder(7), None);

        Assert.Empty(await store.GetAllAsync(None));
        Assert.Null(await RawRowAsync());
    }

    // ------------------------------------------------------------ Speed range

    [Theory]
    [InlineData(0.25f, 0.25f)]
    [InlineData(1.5f, 1.5f)]
    [InlineData(4.0f, 4.0f)]
    [InlineData(0.1f, 0.25f)]
    [InlineData(0f, 0.25f)]
    [InlineData(-3f, 0.25f)]
    [InlineData(4.5f, 4.0f)]
    [InlineData(100f, 4.0f)]
    [InlineData(float.MaxValue, 4.0f)]
    public async Task SaveAsync_Speed_IsClampedToTheAllowedRange(float speed, float expected)
    {
        var store = NewStore();
        await store.SaveAsync(Folder(7, speed), None);

        Assert.Equal(expected, (await store.GetAsync(7, None))!.Speed);
        Assert.Equal(expected, (await NewStore().GetAsync(7, None))!.Speed);
    }

    [Theory]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    [InlineData(float.NegativeInfinity)]
    public async Task SaveAsync_NonFiniteSpeedWithFavorite_MeansNoSpeed(float speed)
    {
        var store = NewStore();
        await store.SaveAsync(Folder(7, speed, favorite: true), None);

        Assert.Equal(Folder(7, favorite: true), await store.GetAsync(7, None));
        Assert.Equal(Folder(7, favorite: true), await NewStore().GetAsync(7, None));
    }

    [Theory]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    [InlineData(float.NegativeInfinity)]
    public async Task SaveAsync_NonFiniteSpeedWithoutFavorite_StoresNoEntry(float speed)
    {
        var store = NewStore();
        var raised = 0;
        store.Changed += (_, _) => raised++;

        await store.SaveAsync(Folder(7, speed), None);

        Assert.Null(await store.GetAsync(7, None));
        Assert.Empty(await store.GetAllAsync(None));
        Assert.Equal(0, raised);
    }

    [Fact]
    public async Task GetAsync_StoredSpeedOutsideTheRange_IsClampedOnLoad()
    {
        var all = await LoadRawAsync("{\"1\":{\"Speed\":10},\"2\":{\"Speed\":0}}");

        Assert.Equal(4.0f, all[1].Speed);
        Assert.Equal(0.25f, all[2].Speed);
    }

    // ------------------------------------------------------------ GetAllAsync

    [Fact]
    public async Task GetAllAsync_AfterSaves_ReturnsEveryEntry()
    {
        var store = NewStore();
        await store.SaveAsync(Folder(1, 2f), None);
        await store.SaveAsync(Folder(2, favorite: true), None);

        var all = await store.GetAllAsync(None);

        Assert.Equal(2, all.Count);
        Assert.Equal(Folder(1, 2f), all[1]);
        Assert.Equal(Folder(2, favorite: true), all[2]);
    }

    [Fact]
    public async Task GetAllAsync_ChangingTheReturnedMap_DoesNotChangeTheStore()
    {
        var store = NewStore();
        await store.SaveAsync(Folder(1, 2f), None);
        var all = await store.GetAllAsync(None);

        if (all is IDictionary<long, FolderSettings> mutable)
        {
            mutable.Clear();
            mutable[99] = Folder(99, 1f);
        }

        var again = await store.GetAllAsync(None);
        Assert.Equal(Folder(1, 2f), Assert.Single(again).Value);
        Assert.Null(await store.GetAsync(99, None));
    }

    [Fact]
    public async Task GetAllAsync_SaveAfterwards_DoesNotChangeTheEarlierResult()
    {
        var store = NewStore();
        await store.SaveAsync(Folder(1, 2f), None);
        var earlier = await store.GetAllAsync(None);

        await store.SaveAsync(Folder(2, 3f), None);

        Assert.Single(earlier);
        Assert.Equal(2, (await store.GetAllAsync(None)).Count);
    }

    // ------------------------------------------------------------ Changed

    [Fact]
    public async Task Changed_OneEffectiveSave_IsRaisedOnce()
    {
        var store = NewStore();
        var raised = 0;
        object? sender = null;
        store.Changed += (s, _) =>
        {
            raised++;
            sender = s;
        };

        await store.SaveAsync(Folder(7, 2f), None);

        Assert.Equal(1, raised);
        Assert.Same(store, sender);
    }

    [Fact]
    public async Task Changed_SaveOfTheSameValueAgain_IsNotRaised()
    {
        var store = NewStore();
        await store.SaveAsync(Folder(7, 2f, true), None);
        var raised = 0;
        store.Changed += (_, _) => raised++;

        await store.SaveAsync(Folder(7, 2f, true), None);

        Assert.Equal(0, raised);
    }

    [Fact]
    public async Task Changed_SaveThatClampsToTheStoredValue_IsNotRaised()
    {
        var store = NewStore();
        await store.SaveAsync(Folder(7, 4f), None);
        var raised = 0;
        store.Changed += (_, _) => raised++;

        await store.SaveAsync(Folder(7, 9f), None);

        Assert.Equal(0, raised);
    }

    [Fact]
    public async Task Changed_EmptyEntryForUnknownFolder_IsNotRaised()
    {
        var store = NewStore();
        var raised = 0;
        store.Changed += (_, _) => raised++;

        await store.SaveAsync(Folder(7), None);

        Assert.Equal(0, raised);
    }

    [Fact]
    public async Task Changed_RemovalOfAnEntry_IsRaisedOnce()
    {
        var store = NewStore();
        await store.SaveAsync(Folder(7, 2f), None);
        var raised = 0;
        store.Changed += (_, _) => raised++;

        await store.SaveAsync(Folder(7), None);

        Assert.Equal(1, raised);
    }

    [Fact]
    public async Task Changed_SubscriberThrows_SaveStillSucceeds()
    {
        var store = NewStore();
        store.Changed += (_, _) => throw new InvalidOperationException("subscriber failed");

        await store.SaveAsync(Folder(7, 2f), None);

        Assert.Equal(Folder(7, 2f), await store.GetAsync(7, None));
    }

    [Fact]
    public async Task Changed_WhenRaised_StoreAlreadyHoldsTheNewValue()
    {
        var store = NewStore();
        FolderSettings? seen = null;
        store.Changed += (_, _) => seen = store.GetAsync(7, None).GetAwaiter().GetResult();

        await store.SaveAsync(Folder(7, 2f), None);

        Assert.Equal(Folder(7, 2f), seen);
    }

    // ------------------------------------------------------------ Tolerant load

    [Fact]
    public async Task GetAllAsync_NoRow_ReturnsEmpty()
    {
        Assert.Empty(await NewStore().GetAllAsync(None));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("{}")]
    [InlineData("null")]
    public async Task GetAllAsync_EmptyRow_ReturnsEmpty(string json)
    {
        Assert.Empty(await LoadRawAsync(json));
    }

    [Theory]
    [InlineData("{not json")]
    [InlineData("garbage")]
    [InlineData("{\"1\":{\"Speed\":")]
    public async Task GetAllAsync_BrokenJson_ReturnsEmpty(string json)
    {
        Assert.Empty(await LoadRawAsync(json));
    }

    [Theory]
    [InlineData("[]")]
    [InlineData("[1,2,3]")]
    [InlineData("[{\"Speed\":2}]")]
    [InlineData("\"text\"")]
    [InlineData("42")]
    public async Task GetAllAsync_JsonThatIsNotAnObject_ReturnsEmpty(string json)
    {
        Assert.Empty(await LoadRawAsync(json));
    }

    [Fact]
    public async Task SaveAsync_AfterBrokenRow_ReplacesItWithAValidRow()
    {
        await StoreRawAsync("{not json");
        var store = NewStore();

        await store.SaveAsync(Folder(7, 2f), None);

        Assert.Equal(Folder(7, 2f), await NewStore().GetAsync(7, None));
    }

    [Fact]
    public async Task GetAllAsync_KeyThatIsNotANumber_SkipsOnlyThatEntry()
    {
        var all = await LoadRawAsync(
            "{\"abc\":{\"Speed\":2},\"\":{\"Speed\":2},\"1.5\":{\"Speed\":2},\"5\":{\"Speed\":1.5},\"6\":{\"Favorite\":true}}");

        Assert.Equal(new long[] { 5, 6 }, all.Keys.OrderBy(k => k).ToArray());
        Assert.Equal(1.5f, all[5].Speed);
        Assert.True(all[6].Favorite);
    }

    [Fact]
    public async Task GetAllAsync_SpeedOfTheWrongJsonKind_KeepsFavoriteAndOtherEntries()
    {
        var all = await LoadRawAsync(
            "{\"1\":{\"Speed\":\"fast\",\"Favorite\":true},\"2\":{\"Speed\":1.5},\"3\":{\"Speed\":\"fast\"}}");

        Assert.Equal(Folder(1, favorite: true), all[1]);
        Assert.Equal(Folder(2, 1.5f), all[2]);
        Assert.False(all.ContainsKey(3));
    }

    [Theory]
    [InlineData("\"yes\"")]
    [InlineData("1")]
    [InlineData("null")]
    [InlineData("[true]")]
    public async Task GetAllAsync_FavoriteOfTheWrongJsonKind_KeepsSpeedAndOtherEntries(string favorite)
    {
        var all = await LoadRawAsync($"{{\"1\":{{\"Speed\":2,\"Favorite\":{favorite}}},\"2\":{{\"Favorite\":true}}}}");

        Assert.Equal(Folder(1, 2f), all[1]);
        Assert.Equal(Folder(2, favorite: true), all[2]);
    }

    [Theory]
    [InlineData("\"text\"")]
    [InlineData("5")]
    [InlineData("true")]
    [InlineData("null")]
    [InlineData("[1]")]
    public async Task GetAllAsync_EntryThatIsNotAnObject_SkipsOnlyThatEntry(string entry)
    {
        var all = await LoadRawAsync($"{{\"1\":{entry},\"2\":{{\"Speed\":2}}}}");

        Assert.Equal(Folder(2, 2f), Assert.Single(all).Value);
    }

    [Fact]
    public async Task GetAllAsync_StoredEntryWithoutSpeedAndFavorite_IsNotKept()
    {
        var all = await LoadRawAsync("{\"1\":{},\"2\":{\"Favorite\":false},\"3\":{\"Speed\":null},\"4\":{\"Speed\":2}}");

        Assert.Equal(new long[] { 4 }, all.Keys.ToArray());
    }

    [Fact]
    public async Task GetAllAsync_StoredEntryWithUnknownMembers_IsStillRead()
    {
        var all = await LoadRawAsync("{\"1\":{\"Speed\":2,\"Colour\":\"red\"}}");

        Assert.Equal(Folder(1, 2f), all[1]);
    }

    // ------------------------------------------------------------ Bucket ids

    [Theory]
    [InlineData(-1L)]
    [InlineData(-5L)]
    [InlineData(long.MinValue)]
    [InlineData(0L)]
    [InlineData(long.MaxValue)]
    public async Task SaveAsync_UnusualBucketId_RoundTripsThroughANewStore(long bucketId)
    {
        await NewStore().SaveAsync(Folder(bucketId, 1.5f, true), None);

        var loaded = await NewStore().GetAsync(bucketId, None);

        Assert.Equal(Folder(bucketId, 1.5f, true), loaded);
    }

    [Fact]
    public async Task GetAllAsync_NegativeAndPositiveBucketIds_AreDifferentEntries()
    {
        var store = NewStore();
        await store.SaveAsync(Folder(-5, 1f), None);
        await store.SaveAsync(Folder(5, 2f), None);

        var all = await NewStore().GetAllAsync(None);

        Assert.Equal(1f, all[-5].Speed);
        Assert.Equal(2f, all[5].Speed);
    }

    // ------------------------------------------------------------ Concurrency

    [Fact]
    public async Task SaveAsync_TwentyConcurrentSavesOfDifferentFolders_KeepEveryEntry()
    {
        var store = NewStore();
        var raised = 0;
        store.Changed += (_, _) => Interlocked.Increment(ref raised);

        await Task.WhenAll(Enumerable.Range(1, 20).Select(i =>
            Task.Run(() => store.SaveAsync(Folder(i, SpeedOf(i), i % 2 == 0), None))));

        var all = await store.GetAllAsync(None);
        Assert.Equal(20, all.Count);
        Assert.Equal(20, raised);
        var reloaded = await NewStore().GetAllAsync(None);
        Assert.Equal(20, reloaded.Count);
        for (var i = 1; i <= 20; i++)
        {
            Assert.Equal(Folder(i, SpeedOf(i), i % 2 == 0), reloaded[i]);
        }
    }

    // A speed inside 0.25 .. 4.0 that differs from folder to folder and is exact in binary.
    private static float SpeedOf(int i) => 0.25f * ((i % 16) + 1);

    [Fact]
    public async Task SaveAsync_ConcurrentSavesBeforeFirstLoad_DoNotLoseTheStoredEntries()
    {
        await NewStore().SaveAsync(Folder(100, 3f), None);
        var store = NewStore();

        await Task.WhenAll(Enumerable.Range(1, 20).Select(i =>
            Task.Run(() => store.SaveAsync(Folder(i, favorite: true), None))));

        var reloaded = await NewStore().GetAllAsync(None);
        Assert.Equal(21, reloaded.Count);
        Assert.Equal(Folder(100, 3f), reloaded[100]);
    }

    // ------------------------------------------------------------ Cancellation

    [Fact]
    public async Task SaveAsync_CancelledToken_ThrowsOperationCanceledAndChangesNothing()
    {
        var store = NewStore();
        var raised = 0;
        store.Changed += (_, _) => raised++;

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => store.SaveAsync(Folder(7, 2f), Cancelled));

        Assert.Equal(0, raised);
        Assert.Null(await store.GetAsync(7, None));
        Assert.Null(await NewStore().GetAsync(7, None));
    }

    [Fact]
    public async Task GetAsync_CancelledTokenOnFirstRead_ThrowsOperationCanceled()
    {
        var store = NewStore();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => store.GetAsync(7, Cancelled));
    }

    [Fact]
    public async Task GetAllAsync_CancelledTokenOnFirstRead_ThrowsOperationCanceled()
    {
        var store = NewStore();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => store.GetAllAsync(Cancelled));
    }

    [Fact]
    public async Task GetAsync_AfterACancelledRead_StillWorks()
    {
        var store = NewStore();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => store.GetAsync(7, Cancelled));

        await store.SaveAsync(Folder(7, 2f), None);

        Assert.Equal(Folder(7, 2f), await store.GetAsync(7, None));
    }

    [Fact]
    public async Task SaveAsync_NullValue_Throws()
    {
        var store = NewStore();

        await Assert.ThrowsAsync<ArgumentNullException>(() => store.SaveAsync(null!, None));
    }

    [Fact]
    public void Constructor_NullRepository_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new FolderSettingsStore(null!));
    }

    [Fact]
    public void SettingsKey_IsFolderSettings()
    {
        Assert.Equal("folder_settings", FolderSettingsStore.SettingsKey);
    }
}
