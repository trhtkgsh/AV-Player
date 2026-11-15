using System.Text.Json;
using Player.Core.Models;
using Player.Data.Database;
using Player.Data.Tests.Support;
using Player.Data.UiSettingsStorage;

namespace Player.Data.Tests.UiSettingsStorage;

// Spec 13 items 5-8: SchemaVersion switch of ExplorerFields, ItemSize, and the new overlay keys.
public sealed class UiSettingsStoreSchemaTests : TempDatabaseTestBase
{
    private static CancellationToken None => CancellationToken.None;

    private const ExplorerInfoFields OldDefault =
        ExplorerInfoFields.Duration | ExplorerInfoFields.Resolution | ExplorerInfoFields.Size;

    private SettingsRepository _repository = null!;
    private DbConnectionProvider? _provider;

    private UiSettingsStore NewStore()
    {
        _provider ??= CreateProvider();
        _repository = new SettingsRepository(_provider);
        return new UiSettingsStore(_repository);
    }

    private async Task StoreRawAsync(string json)
    {
        NewStore();
        await _repository.SetJsonAsync(UiSettingsStore.SettingsKey, json, None);
    }

    private async Task<UiSettings> LoadRawAsync(string json)
    {
        await StoreRawAsync(json);
        return await NewStore().GetAsync(None);
    }

    private async Task<string?> RowAsync() =>
        await _repository.GetJsonAsync(UiSettingsStore.SettingsKey, None);

    private async Task<JsonElement> RowMemberAsync(string name)
    {
        using var document = JsonDocument.Parse((await RowAsync())!);
        return document.RootElement.GetProperty(name).Clone();
    }

    // ---- One-time ExplorerFields switch ----

    [Fact]
    public async Task GetAsync_RowWithoutSchemaVersionHoldingFields_ReadsNone()
    {
        var value = await LoadRawAsync("{\"ExplorerFields\":\"Duration, Resolution, Size\"}");

        Assert.Equal(ExplorerInfoFields.None, value.ExplorerFields);
    }

    [Fact]
    public async Task GetAsync_OldRowRead_KeepsOtherStoredMembers()
    {
        var value = await LoadRawAsync("{\"ExplorerFields\":\"7\",\"NewTagDays\":12}");

        Assert.Equal(12, value.NewTagDays);
    }

    [Fact]
    public async Task GetAsync_OldRowReadTwice_StillNone()
    {
        await StoreRawAsync("{\"ExplorerFields\":\"7\"}");
        var store = NewStore();

        var first = await store.GetAsync(None);
        var second = await NewStore().GetAsync(None);

        Assert.Equal(ExplorerInfoFields.None, first.ExplorerFields);
        Assert.Equal(ExplorerInfoFields.None, second.ExplorerFields);
    }

    [Fact]
    public async Task GetAsync_OldRow_DoesNotWriteTheRow()
    {
        const string old = "{\"ExplorerFields\":\"7\",\"NewTagDays\":12}";
        await StoreRawAsync(old);

        await NewStore().GetAsync(None);

        Assert.Equal(old, await RowAsync());
    }

    [Fact]
    public async Task SaveAsync_AfterOldRow_WritesSchemaVersion2()
    {
        await StoreRawAsync("{\"ExplorerFields\":\"7\"}");
        var store = NewStore();
        var current = await store.GetAsync(None);

        await store.SaveAsync(current, None);

        Assert.Equal(2, (await RowMemberAsync("SchemaVersion")).GetInt32());
    }

    [Fact]
    public async Task SaveAsync_AfterOldRow_KeepsNoneOnNewStore()
    {
        await StoreRawAsync("{\"ExplorerFields\":\"7\"}");
        var store = NewStore();
        await store.SaveAsync(await store.GetAsync(None), None);

        var loaded = await NewStore().GetAsync(None);

        Assert.Equal(ExplorerInfoFields.None, loaded.ExplorerFields);
    }

    [Fact]
    public async Task GetAsync_UserTurnsFieldsOnAfterSwitch_NewStoreReadsThem()
    {
        await StoreRawAsync("{\"ExplorerFields\":\"7\"}");
        var store = NewStore();
        var current = await store.GetAsync(None);
        var chosen = ExplorerInfoFields.Date | ExplorerInfoFields.Size;
        await store.SaveAsync(current with { ExplorerFields = chosen }, None);

        var loaded = await NewStore().GetAsync(None);

        Assert.Equal(chosen, loaded.ExplorerFields);
    }

    [Fact]
    public async Task GetAsync_Version2Row_KeepsStoredFields()
    {
        var value = await LoadRawAsync("{\"SchemaVersion\":2,\"ExplorerFields\":\"Duration, Path\"}");

        Assert.Equal(ExplorerInfoFields.Duration | ExplorerInfoFields.Path, value.ExplorerFields);
    }

    [Fact]
    public async Task GetAsync_HigherVersionRow_KeepsStoredFields()
    {
        var value = await LoadRawAsync("{\"SchemaVersion\":3,\"ExplorerFields\":\"Date\"}");

        Assert.Equal(ExplorerInfoFields.Date, value.ExplorerFields);
    }

    [Theory]
    [InlineData("1")]
    [InlineData("0")]
    [InlineData("-5")]
    [InlineData("\"x\"")]
    [InlineData("\"2\"")]
    [InlineData("null")]
    [InlineData("true")]
    [InlineData("1.5")]
    [InlineData("{}")]
    public async Task GetAsync_InvalidOrOldSchemaVersion_ReadsNone(string version)
    {
        var value = await LoadRawAsync("{\"SchemaVersion\":" + version + ",\"ExplorerFields\":\"7\"}");

        Assert.Equal(ExplorerInfoFields.None, value.ExplorerFields);
    }

    [Fact]
    public async Task SaveAsync_NewStoreWithNoRow_WritesSchemaVersion2()
    {
        var store = NewStore();

        await store.SaveAsync(new UiSettings(), None);

        Assert.Equal(2, (await RowMemberAsync("SchemaVersion")).GetInt32());
    }

    [Fact]
    public async Task GetAsync_NewStoreNoRow_DoesNotWrite()
    {
        var store = NewStore();

        await store.GetAsync(None);

        Assert.Null(await RowAsync());
    }

    [Fact]
    public async Task SaveAsync_WritesCurrentFieldsNextToVersion()
    {
        var store = NewStore();
        var fields = ExplorerInfoFields.Resolution | ExplorerInfoFields.Path;

        await store.SaveAsync(new UiSettings { ExplorerFields = fields }, None);

        var loaded = await NewStore().GetAsync(None);
        Assert.Equal(fields, loaded.ExplorerFields);
    }

    // ---- ItemSize ----

    [Fact]
    public async Task GetAsync_NoRow_ItemSizeIsLarge()
    {
        var value = await NewStore().GetAsync(None);

        Assert.Equal(ExplorerItemSize.Large, value.ItemSize);
    }

    [Fact]
    public async Task GetAsync_ItemSizeSmallSaved_RoundTripOnNewStore()
    {
        await NewStore().SaveAsync(new UiSettings { ItemSize = ExplorerItemSize.Small }, None);

        var loaded = await NewStore().GetAsync(None);

        Assert.Equal(ExplorerItemSize.Small, loaded.ItemSize);
    }

    [Fact]
    public async Task GetAsync_ItemSizeGarbage_UsesDefault()
    {
        var value = await LoadRawAsync("{\"ItemSize\":\"Huge\",\"NewTagDays\":12}");

        Assert.Equal(ExplorerItemSize.Large, value.ItemSize);
        Assert.Equal(12, value.NewTagDays);
    }

    [Fact]
    public async Task GetAsync_ItemSizeWrongType_UsesDefault()
    {
        var value = await LoadRawAsync("{\"ItemSize\":true}");

        Assert.Equal(ExplorerItemSize.Large, value.ItemSize);
    }

    [Fact]
    public async Task GetAsync_ItemSizeUndefinedNumber_UsesDefault()
    {
        var value = await LoadRawAsync("{\"ItemSize\":42}");

        Assert.Equal(ExplorerItemSize.Large, value.ItemSize);
    }

    [Fact]
    public async Task GetAsync_ItemSizeNameAnyCase_IsRead()
    {
        var value = await LoadRawAsync("{\"ItemSize\":\"small\"}");

        Assert.Equal(ExplorerItemSize.Small, value.ItemSize);
    }

    [Fact]
    public async Task SaveAsync_UndefinedItemSize_ReturnsDefaultOnSameStore()
    {
        var store = NewStore();
        await store.SaveAsync(new UiSettings { ItemSize = (ExplorerItemSize)9 }, None);

        var value = await store.GetAsync(None);

        Assert.Equal(ExplorerItemSize.Large, value.ItemSize);
    }

    // ---- Overlay: thickness and corners ----

    [Fact]
    public async Task GetAsync_NoRow_OverlayNewKeysAreDefaults()
    {
        var overlay = (await NewStore().GetAsync(None)).Overlay;

        Assert.Equal(2, overlay.ProgressLineThickness);
        Assert.Equal(OverlayCorner.TopLeft, overlay.ClockCorner);
        Assert.Equal(OverlayCorner.TopLeft, overlay.BatteryCorner);
    }

    [Fact]
    public async Task GetAsync_OverlayNewKeysSaved_RoundTripOnNewStore()
    {
        var overlay = new PlaybackOverlaySettings
        {
            ProgressLineThickness = 7,
            ClockCorner = OverlayCorner.BottomRight,
            BatteryCorner = OverlayCorner.BottomLeft,
        };
        await NewStore().SaveAsync(new UiSettings { Overlay = overlay }, None);

        var loaded = (await NewStore().GetAsync(None)).Overlay;

        Assert.Equal(7, loaded.ProgressLineThickness);
        Assert.Equal(OverlayCorner.BottomRight, loaded.ClockCorner);
        Assert.Equal(OverlayCorner.BottomLeft, loaded.BatteryCorner);
    }

    [Fact]
    public async Task SaveAsync_OverlayNewKeys_AreStoredUnderTheirNames()
    {
        var store = NewStore();
        var overlay = new PlaybackOverlaySettings
        {
            ProgressLineThickness = 5,
            ClockCorner = OverlayCorner.TopRight,
            BatteryCorner = OverlayCorner.BottomRight,
        };

        await store.SaveAsync(new UiSettings { Overlay = overlay, ItemSize = ExplorerItemSize.Small }, None);

        Assert.Equal(5, (await RowMemberAsync("OverlayProgressLineThickness")).GetInt32());
        Assert.Equal("TopRight", (await RowMemberAsync("OverlayClockCorner")).GetString());
        Assert.Equal("BottomRight", (await RowMemberAsync("OverlayBatteryCorner")).GetString());
        Assert.Equal("Small", (await RowMemberAsync("ItemSize")).GetString());
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-3, 1)]
    [InlineData(1, 1)]
    [InlineData(12, 12)]
    [InlineData(13, 12)]
    [InlineData(1000, 12)]
    public async Task GetAsync_StoredThickness_IsClamped(int stored, int expected)
    {
        var value = await LoadRawAsync("{\"OverlayProgressLineThickness\":" + stored + "}");

        Assert.Equal(expected, value.Overlay.ProgressLineThickness);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(99, 12)]
    public async Task SaveAsync_ThicknessOutOfRange_IsClampedOnSameStoreAndInRow(int given, int expected)
    {
        var store = NewStore();
        await store.SaveAsync(new UiSettings { Overlay = new PlaybackOverlaySettings { ProgressLineThickness = given } }, None);

        var value = await store.GetAsync(None);

        Assert.Equal(expected, value.Overlay.ProgressLineThickness);
        Assert.Equal(expected, (await RowMemberAsync("OverlayProgressLineThickness")).GetInt32());
    }

    [Theory]
    [InlineData("\"thick\"")]
    [InlineData("true")]
    [InlineData("null")]
    [InlineData("3.5")]
    [InlineData("[]")]
    public async Task GetAsync_ThicknessGarbage_UsesDefault(string stored)
    {
        var value = await LoadRawAsync("{\"OverlayProgressLineThickness\":" + stored + ",\"NewTagDays\":12}");

        Assert.Equal(2, value.Overlay.ProgressLineThickness);
        Assert.Equal(12, value.NewTagDays);
    }

    [Theory]
    [InlineData("OverlayClockCorner", "\"Middle\"")]
    [InlineData("OverlayClockCorner", "77")]
    [InlineData("OverlayClockCorner", "-1")]
    [InlineData("OverlayClockCorner", "false")]
    [InlineData("OverlayClockCorner", "null")]
    [InlineData("OverlayBatteryCorner", "\"Middle\"")]
    [InlineData("OverlayBatteryCorner", "77")]
    [InlineData("OverlayBatteryCorner", "[]")]
    public async Task GetAsync_CornerGarbageOrUndefined_UsesDefault(string key, string stored)
    {
        var value = await LoadRawAsync("{\"" + key + "\":" + stored + "}");

        Assert.Equal(OverlayCorner.TopLeft, value.Overlay.ClockCorner);
        Assert.Equal(OverlayCorner.TopLeft, value.Overlay.BatteryCorner);
    }

    [Fact]
    public async Task GetAsync_OneBadCorner_DoesNotDiscardTheOther()
    {
        var value = await LoadRawAsync("{\"OverlayClockCorner\":\"Nope\",\"OverlayBatteryCorner\":\"BottomRight\"}");

        Assert.Equal(OverlayCorner.TopLeft, value.Overlay.ClockCorner);
        Assert.Equal(OverlayCorner.BottomRight, value.Overlay.BatteryCorner);
    }

    [Fact]
    public async Task SaveAsync_UndefinedCorners_ReturnDefaultsOnSameStore()
    {
        var store = NewStore();
        var overlay = new PlaybackOverlaySettings
        {
            ClockCorner = (OverlayCorner)42,
            BatteryCorner = (OverlayCorner)(-1),
        };
        await store.SaveAsync(new UiSettings { Overlay = overlay }, None);

        var value = await store.GetAsync(None);

        Assert.Equal(OverlayCorner.TopLeft, value.Overlay.ClockCorner);
        Assert.Equal(OverlayCorner.TopLeft, value.Overlay.BatteryCorner);
    }
}
