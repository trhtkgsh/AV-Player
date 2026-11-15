using System.Text.Json;
using Player.Core.Models;
using Player.Data.Database;
using Player.Data.Tests.Support;
using Player.Data.UiSettingsStorage;

namespace Player.Data.Tests.UiSettingsStorage;

public sealed class UiSettingsStoreOverlayTests : TempDatabaseTestBase
{
    private static CancellationToken None => CancellationToken.None;

    private SettingsRepository _repository = null!;
    private DbConnectionProvider? _provider;

    private UiSettingsStore NewStore()
    {
        _provider ??= CreateProvider();
        _repository = new SettingsRepository(_provider);
        return new UiSettingsStore(_repository);
    }

    private async Task<PlaybackOverlaySettings> LoadOverlayAsync(string json)
    {
        NewStore();
        await _repository.SetJsonAsync(UiSettingsStore.SettingsKey, json, None);
        return (await NewStore().GetAsync(None)).Overlay;
    }

    private async Task<JsonElement> RowMemberAsync(string name)
    {
        var json = await _repository.GetJsonAsync(UiSettingsStore.SettingsKey, None);
        using var document = JsonDocument.Parse(json!);
        return document.RootElement.GetProperty(name).Clone();
    }

    private static PlaybackOverlaySettings NonDefaultOverlay() => new()
    {
        ProgressLine = false,
        ProgressLineEdge = OverlayEdge.Top,
        Clock = false,
        Battery = false,
        Time = OverlayTimeMode.ElapsedRemaining,
        Corner = OverlayCorner.BottomLeft,
        MarginDp = 20,
        FontSize = 18,
        ColorArgb = 0x80FF00AA,
    };

    [Fact]
    public async Task GetAsync_AfterSaveOnNewStore_RoundTripsEveryOverlayField()
    {
        var expected = new UiSettings { Overlay = NonDefaultOverlay() };
        await NewStore().SaveAsync(expected, None);

        var loaded = await NewStore().GetAsync(None);

        Assert.Equal(NonDefaultOverlay(), loaded.Overlay);
    }

    [Fact]
    public async Task GetAsync_RowWithoutAnyOverlayKey_LoadsOverlayDefaults()
    {
        var overlay = await LoadOverlayAsync("{\"SkipSeconds\":15}");

        Assert.Equal(new PlaybackOverlaySettings(), overlay);
    }

    [Fact]
    public async Task GetAsync_OnlyOneOverlayKeyStored_KeepsDefaultsForTheOthers()
    {
        var overlay = await LoadOverlayAsync("{\"OverlayCorner\":\"BottomRight\"}");

        Assert.Equal(new PlaybackOverlaySettings { Corner = OverlayCorner.BottomRight }, overlay);
    }

    [Theory]
    [InlineData("\"yes\"")]
    [InlineData("1")]
    [InlineData("null")]
    [InlineData("[]")]
    [InlineData("{}")]
    public async Task GetAsync_BoolOverlayFieldsOfWrongKind_LoadDefaults(string value)
    {
        var overlay = await LoadOverlayAsync(
            $"{{\"OverlayProgressLine\":{value},\"OverlayClock\":{value},\"OverlayBattery\":{value}}}");

        Assert.Equal(new PlaybackOverlaySettings(), overlay);
    }

    [Theory]
    [InlineData("\"Sideways\"")]
    [InlineData("\"\"")]
    [InlineData("99")]
    [InlineData("-1")]
    [InlineData("true")]
    [InlineData("1.5")]
    [InlineData("[]")]
    [InlineData("null")]
    public async Task GetAsync_EnumOverlayFieldsUndefinedOrWrongKind_LoadDefaults(string value)
    {
        var overlay = await LoadOverlayAsync(
            $"{{\"OverlayProgressLineEdge\":{value},\"OverlayTime\":{value},\"OverlayCorner\":{value}}}");

        Assert.Equal(new PlaybackOverlaySettings(), overlay);
    }

    [Fact]
    public async Task GetAsync_EnumNameInOtherCase_IsAccepted()
    {
        var overlay = await LoadOverlayAsync("{\"OverlayCorner\":\"topleft\",\"OverlayProgressLineEdge\":\"TOP\"}");

        Assert.Equal(OverlayCorner.TopLeft, overlay.Corner);
        Assert.Equal(OverlayEdge.Top, overlay.ProgressLineEdge);
    }

    [Fact]
    public async Task GetAsync_EnumStoredAsDefinedNumber_IsAccepted()
    {
        var overlay = await LoadOverlayAsync("{\"OverlayCorner\":2}");

        Assert.Equal(OverlayCorner.BottomLeft, overlay.Corner);
    }

    [Theory]
    [InlineData(-5, 0)]
    [InlineData(0, 0)]
    [InlineData(48, 48)]
    [InlineData(49, 48)]
    [InlineData(100000, 48)]
    public async Task GetAsync_MarginOutOfRange_IsClamped(int stored, int expected)
    {
        var overlay = await LoadOverlayAsync($"{{\"OverlayMarginDp\":{stored}}}");

        Assert.Equal(expected, overlay.MarginDp);
    }

    [Theory]
    [InlineData(-3, 10)]
    [InlineData(9, 10)]
    [InlineData(10, 10)]
    [InlineData(24, 24)]
    [InlineData(25, 24)]
    [InlineData(100000, 24)]
    public async Task GetAsync_FontSizeOutOfRange_IsClamped(int stored, int expected)
    {
        var overlay = await LoadOverlayAsync($"{{\"OverlayFontSize\":{stored}}}");

        Assert.Equal(expected, overlay.FontSize);
    }

    [Theory]
    [InlineData("\"20\"")]
    [InlineData("12.5")]
    [InlineData("99999999999")]
    [InlineData("true")]
    [InlineData("null")]
    public async Task GetAsync_MarginAndSizeOfWrongKindOrNotInt_LoadDefaults(string value)
    {
        var overlay = await LoadOverlayAsync($"{{\"OverlayMarginDp\":{value},\"OverlayFontSize\":{value}}}");

        Assert.Equal(0, overlay.MarginDp);
        Assert.Equal(12, overlay.FontSize);
    }

    [Theory]
    [InlineData("\"FFFFFF\"")]
    [InlineData("\"FFFFFFFFF\"")]
    [InlineData("\"GGGGGGGG\"")]
    [InlineData("\"\"")]
    [InlineData("\"#FFFFFFF\"")]
    [InlineData("\" FFFFFFF\"")]
    [InlineData("4294967295")]
    [InlineData("true")]
    [InlineData("null")]
    public async Task GetAsync_ColorMalformed_LoadsDefaultColor(string value)
    {
        var overlay = await LoadOverlayAsync($"{{\"OverlayColorArgb\":{value}}}");

        Assert.Equal(0xFFFFFFFFu, overlay.ColorArgb);
    }

    [Fact]
    public async Task GetAsync_ColorMalformed_DoesNotDiscardOtherOverlayFields()
    {
        var overlay = await LoadOverlayAsync("{\"OverlayColorArgb\":\"zz\",\"OverlayMarginDp\":7}");

        Assert.Equal(7, overlay.MarginDp);
    }

    [Fact]
    public async Task GetAsync_ColorInLowerCase_IsAcceptedByTheStore()
    {
        var overlay = await LoadOverlayAsync("{\"OverlayColorArgb\":\"80ff00aa\"}");

        Assert.Equal(0x80FF00AAu, overlay.ColorArgb);
    }

    [Fact]
    public async Task SaveAsync_Color_IsWrittenAsEightUpperCaseHexDigits()
    {
        await NewStore().SaveAsync(new UiSettings { Overlay = new PlaybackOverlaySettings { ColorArgb = 0x0000AB0C } }, None);

        var member = await RowMemberAsync("OverlayColorArgb");

        Assert.Equal(JsonValueKind.String, member.ValueKind);
        Assert.Equal("0000AB0C", member.GetString());
    }

    [Fact]
    public async Task SaveAsync_ColorOutsideThePageList_IsKeptOnReload()
    {
        await NewStore().SaveAsync(new UiSettings { Overlay = new PlaybackOverlaySettings { ColorArgb = 0x12345678 } }, None);

        var loaded = await NewStore().GetAsync(None);

        Assert.Equal(0x12345678u, loaded.Overlay.ColorArgb);
    }

    [Fact]
    public async Task SaveAsync_OutOfRangeMarginAndSize_AreStoredClamped()
    {
        var store = NewStore();
        await store.SaveAsync(new UiSettings { Overlay = new PlaybackOverlaySettings { MarginDp = 500, FontSize = 1 } }, None);

        Assert.Equal(48, (await RowMemberAsync("OverlayMarginDp")).GetInt32());
        Assert.Equal(10, (await RowMemberAsync("OverlayFontSize")).GetInt32());
    }

    [Fact]
    public async Task SaveAsync_UndefinedOverlayEnums_AreSavedAsDefaults()
    {
        var store = NewStore();
        var overlay = new PlaybackOverlaySettings
        {
            ProgressLineEdge = (OverlayEdge)42,
            Time = (OverlayTimeMode)42,
            Corner = (OverlayCorner)42,
        };

        await store.SaveAsync(new UiSettings { Overlay = overlay }, None);
        var loaded = await NewStore().GetAsync(None);

        Assert.Equal(new PlaybackOverlaySettings(), loaded.Overlay);
    }

    [Fact]
    public async Task SaveAsync_OverlayChange_RaisesChangedWithTheNewOverlay()
    {
        var store = NewStore();
        UiSettings? raised = null;
        var calls = 0;
        store.Changed += (_, v) =>
        {
            raised = v;
            calls++;
        };

        await store.SaveAsync(new UiSettings { Overlay = NonDefaultOverlay() }, None);

        Assert.Equal(1, calls);
        Assert.Equal(NonDefaultOverlay(), raised!.Overlay);
    }

    [Fact]
    public async Task SaveAsync_OverlayChange_DoesNotDisturbOtherFields()
    {
        var store = NewStore();
        var before = new UiSettings
        {
            HudAutoHideSeconds = 8,
            SkipSeconds = 30,
            ShowClock = false,
            SortField = SortField.Size,
            NewTagDays = 21,
            Layout = ExplorerLayout.Grid,
            ExplorerFontScale = 1.3,
            LongPressSpeed = 3.5f,
        };
        await store.SaveAsync(before, None);

        await store.SaveAsync(before with { Overlay = NonDefaultOverlay() }, None);
        var loaded = await NewStore().GetAsync(None);

        Assert.Equal(before with { Overlay = NonDefaultOverlay() }, loaded);
    }

    [Fact]
    public async Task SaveAsync_OtherFieldChange_DoesNotDisturbOverlay()
    {
        var store = NewStore();
        var first = new UiSettings { Overlay = NonDefaultOverlay() };
        await store.SaveAsync(first, None);

        await store.SaveAsync(first with { SkipSeconds = 60 }, None);
        var loaded = await NewStore().GetAsync(None);

        Assert.Equal(NonDefaultOverlay(), loaded.Overlay);
        Assert.Equal(60, loaded.SkipSeconds);
    }
}
