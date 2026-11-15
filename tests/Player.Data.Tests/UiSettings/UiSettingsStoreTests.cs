using Player.Core.Models;
using Player.Data.Database;
using Player.Data.Tests.Support;
using Player.Data.UiSettingsStorage;

namespace Player.Data.Tests.UiSettingsStorage;

public sealed class UiSettingsStoreTests : TempDatabaseTestBase
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

    private static UiSettings NonDefault() => new()
    {
        HudAutoHideSeconds = 8,
        SkipSeconds = 30,
        ShowClock = false,
        ShowBattery = false,
        ExplorerFields = ExplorerInfoFields.Date | ExplorerInfoFields.Path,
        ViewMode = ExplorerViewMode.Flat,
        SortField = SortField.Size,
        SortDirection = SortDirection.Descending,
        NewTagDays = 21,
    };

    private async Task<int> CountRowsAsync(string? key)
    {
        var connection = await OpenAsync(_provider!);
        return key is null
            ? await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM settings")
            : await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM settings WHERE key = ?", key);
    }

    [Fact]
    public async Task GetAsync_NoRow_ReturnsDefaults()
    {
        var store = NewStore();

        var value = await store.GetAsync(None);

        Assert.Equal(new UiSettings(), value);
    }

    [Fact]
    public async Task GetAsync_AfterSaveOnNewStore_RoundTripsEveryMember()
    {
        var expected = NonDefault();
        await NewStore().SaveAsync(expected, None);

        var loaded = await NewStore().GetAsync(None);

        Assert.Equal(expected, loaded);
    }

    [Fact]
    public async Task GetAsync_AfterSaveOnNewStore_KeepsViewModeSortFieldAndDirection()
    {
        await NewStore().SaveAsync(NonDefault(), None);

        var loaded = await NewStore().GetAsync(None);

        Assert.Equal(ExplorerViewMode.Flat, loaded.ViewMode);
        Assert.Equal(SortField.Size, loaded.SortField);
        Assert.Equal(SortDirection.Descending, loaded.SortDirection);
    }

    [Fact]
    public async Task GetAsync_StoredEmptyObject_ReturnsDefaults()
    {
        var value = await LoadRawAsync("{}");

        Assert.Equal(new UiSettings(), value);
    }

    [Fact]
    public async Task GetAsync_OnlySkipSecondsStored_UsesStoredValue()
    {
        var value = await LoadRawAsync("{\"SkipSeconds\":15}");

        Assert.Equal(15, value.SkipSeconds);
    }

    [Fact]
    public async Task GetAsync_OnlySkipSecondsStored_KeepsDefaultsForOtherMembers()
    {
        var value = await LoadRawAsync("{\"SkipSeconds\":15}");

        Assert.Equal(3, value.HudAutoHideSeconds);
        Assert.True(value.ShowClock);
        Assert.True(value.ShowBattery);
        Assert.Equal(
            ExplorerInfoFields.Duration | ExplorerInfoFields.Resolution | ExplorerInfoFields.Size,
            value.ExplorerFields);
        Assert.Equal(ExplorerViewMode.Folders, value.ViewMode);
        Assert.Equal(SortField.Name, value.SortField);
        Assert.Equal(SortDirection.Ascending, value.SortDirection);
        Assert.Equal(7, value.NewTagDays);
    }

    [Fact]
    public async Task GetAsync_BogusSortFieldName_UsesDefaultSortField()
    {
        var value = await LoadRawAsync("{\"SortField\":\"Bogus\",\"NewTagDays\":12}");

        Assert.Equal(SortField.Name, value.SortField);
    }

    [Fact]
    public async Task GetAsync_BogusSortFieldName_KeepsOtherStoredMembers()
    {
        var value = await LoadRawAsync("{\"SortField\":\"Bogus\",\"NewTagDays\":12}");

        Assert.Equal(12, value.NewTagDays);
    }

    public static TheoryData<ExplorerInfoFields> ExplorerFieldCases => new()
    {
        ExplorerInfoFields.None,
        ExplorerInfoFields.Date,
        ExplorerInfoFields.Duration | ExplorerInfoFields.Path,
        ExplorerInfoFields.Resolution | ExplorerInfoFields.Size | ExplorerInfoFields.Date,
        ExplorerInfoFields.Duration | ExplorerInfoFields.Resolution | ExplorerInfoFields.Size |
            ExplorerInfoFields.Date | ExplorerInfoFields.Path,
    };

    [Theory]
    [MemberData(nameof(ExplorerFieldCases))]
    public async Task GetAsync_ExplorerFieldsSaved_RoundTripOnNewStore(ExplorerInfoFields fields)
    {
        await NewStore().SaveAsync(new UiSettings { ExplorerFields = fields }, None);

        var loaded = await NewStore().GetAsync(None);

        Assert.Equal(fields, loaded.ExplorerFields);
    }

    [Fact]
    public async Task GetAsync_StoredExplorerFieldsWithUndefinedBit_KeepsOnlyDefinedFlags()
    {
        var value = await LoadRawAsync("{\"ExplorerFields\":\"33\"}");

        Assert.Equal(ExplorerInfoFields.Duration, value.ExplorerFields);
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("null")]
    public async Task GetAsync_InvalidOrNullJson_ReturnsDefaults(string json)
    {
        var value = await LoadRawAsync(json);

        Assert.Equal(new UiSettings(), value);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-5, 1)]
    [InlineData(11, 10)]
    [InlineData(100, 10)]
    public async Task GetAsync_StoredHudAutoHideOutOfRange_IsClamped(int stored, int expected)
    {
        var value = await LoadRawAsync($"{{\"HudAutoHideSeconds\":{stored}}}");

        Assert.Equal(expected, value.HudAutoHideSeconds);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(31, 30)]
    public async Task GetAsync_StoredNewTagDaysOutOfRange_IsClamped(int stored, int expected)
    {
        var value = await LoadRawAsync($"{{\"NewTagDays\":{stored}}}");

        Assert.Equal(expected, value.NewTagDays);
    }

    public static TheoryData<int, int> SkipCases => new()
    {
        { 0, 5 },
        { -3, 5 },
        { 7, 5 },
        { 8, 10 },
        { 12, 10 },
        { 13, 15 },
        { 22, 15 },
        { 23, 30 },
        { 45, 30 },
        { 46, 60 },
        { 1000, 60 },
        { int.MaxValue, 60 },
    };

    [Theory]
    [MemberData(nameof(SkipCases))]
    public async Task GetAsync_StoredSkipSeconds_SnapsToNearestChoice(int stored, int expected)
    {
        var value = await LoadRawAsync($"{{\"SkipSeconds\":{stored}}}");

        Assert.Equal(expected, value.SkipSeconds);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-5, 1)]
    [InlineData(11, 10)]
    [InlineData(100, 10)]
    public async Task SaveAsync_HudAutoHideOutOfRange_RaisesChangedWithClampedValue(int given, int expected)
    {
        var store = NewStore();
        UiSettings? raised = null;
        store.Changed += (_, v) => raised = v;

        await store.SaveAsync(new UiSettings { HudAutoHideSeconds = given }, None);

        Assert.Equal(expected, raised!.HudAutoHideSeconds);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-5, 1)]
    [InlineData(11, 10)]
    [InlineData(100, 10)]
    public async Task SaveAsync_HudAutoHideOutOfRange_StoresClampedValue(int given, int expected)
    {
        await NewStore().SaveAsync(new UiSettings { HudAutoHideSeconds = given }, None);

        var loaded = await NewStore().GetAsync(None);

        Assert.Equal(expected, loaded.HudAutoHideSeconds);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(31, 30)]
    public async Task SaveAsync_NewTagDaysOutOfRange_RaisesChangedWithClampedValue(int given, int expected)
    {
        var store = NewStore();
        UiSettings? raised = null;
        store.Changed += (_, v) => raised = v;

        await store.SaveAsync(new UiSettings { NewTagDays = given }, None);

        Assert.Equal(expected, raised!.NewTagDays);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(31, 30)]
    public async Task SaveAsync_NewTagDaysOutOfRange_StoresClampedValue(int given, int expected)
    {
        await NewStore().SaveAsync(new UiSettings { NewTagDays = given }, None);

        var loaded = await NewStore().GetAsync(None);

        Assert.Equal(expected, loaded.NewTagDays);
    }

    [Theory]
    [MemberData(nameof(SkipCases))]
    public async Task SaveAsync_SkipSeconds_RaisesChangedWithNearestChoice(int given, int expected)
    {
        var store = NewStore();
        UiSettings? raised = null;
        store.Changed += (_, v) => raised = v;

        await store.SaveAsync(new UiSettings { SkipSeconds = given }, None);

        Assert.Equal(expected, raised!.SkipSeconds);
    }

    [Theory]
    [MemberData(nameof(SkipCases))]
    public async Task SaveAsync_SkipSeconds_StoresNearestChoice(int given, int expected)
    {
        await NewStore().SaveAsync(new UiSettings { SkipSeconds = given }, None);

        var loaded = await NewStore().GetAsync(None);

        Assert.Equal(expected, loaded.SkipSeconds);
    }

    [Fact]
    public async Task SaveAsync_UndefinedViewMode_StoresDefault()
    {
        await NewStore().SaveAsync(new UiSettings { ViewMode = (ExplorerViewMode)99 }, None);

        var loaded = await NewStore().GetAsync(None);

        Assert.Equal(new UiSettings().ViewMode, loaded.ViewMode);
    }

    [Fact]
    public async Task SaveAsync_UndefinedSortField_StoresDefault()
    {
        await NewStore().SaveAsync(new UiSettings { SortField = (SortField)99 }, None);

        var loaded = await NewStore().GetAsync(None);

        Assert.Equal(new UiSettings().SortField, loaded.SortField);
    }

    [Fact]
    public async Task SaveAsync_UndefinedSortDirection_StoresDefault()
    {
        await NewStore().SaveAsync(new UiSettings { SortDirection = (SortDirection)99 }, None);

        var loaded = await NewStore().GetAsync(None);

        Assert.Equal(new UiSettings().SortDirection, loaded.SortDirection);
    }

    [Fact]
    public async Task SaveAsync_UndefinedEnums_RaisesChangedWithDefaults()
    {
        var store = NewStore();
        UiSettings? raised = null;
        store.Changed += (_, v) => raised = v;

        await store.SaveAsync(
            new UiSettings
            {
                ViewMode = (ExplorerViewMode)99,
                SortField = (SortField)99,
                SortDirection = (SortDirection)99,
            },
            None);

        Assert.Equal(new UiSettings(), raised);
    }

    [Fact]
    public async Task SaveAsync_OneCall_RaisesChangedExactlyOnce()
    {
        var store = NewStore();
        var calls = 0;
        store.Changed += (_, _) => calls++;

        await store.SaveAsync(NonDefault(), None);

        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task SaveAsync_TwoCalls_RaisesChangedTwice()
    {
        var store = NewStore();
        var calls = 0;
        store.Changed += (_, _) => calls++;

        await store.SaveAsync(NonDefault(), None);
        await store.SaveAsync(new UiSettings(), None);

        Assert.Equal(2, calls);
    }

    [Fact]
    public async Task SaveAsync_Changed_SendsSenderAndNormalizedValue()
    {
        var store = NewStore();
        object? sender = null;
        UiSettings? raised = null;
        store.Changed += (s, v) =>
        {
            sender = s;
            raised = v;
        };

        await store.SaveAsync(NonDefault() with { NewTagDays = 99 }, None);

        Assert.Same(store, sender);
        Assert.Equal(NonDefault() with { NewTagDays = 30 }, raised);
    }

    [Fact]
    public async Task SaveAsync_Changed_HandlerSeesNewValueFromGetAsync()
    {
        var store = NewStore();
        UiSettings? seenInHandler = null;
        store.Changed += (_, _) => seenInHandler = store.GetAsync(None).GetAwaiter().GetResult();
        var expected = NonDefault();

        await store.SaveAsync(expected, None);

        Assert.Equal(expected, seenInHandler);
    }

    [Fact]
    public async Task SaveAsync_Changed_HandlerSeesNewValueInStorage()
    {
        var store = NewStore();
        string? storedInHandler = null;
        store.Changed += (_, _) =>
            storedInHandler = _repository.GetJsonAsync(UiSettingsStore.SettingsKey, None).GetAwaiter().GetResult();

        await store.SaveAsync(NonDefault(), None);

        Assert.NotNull(storedInHandler);
    }

    [Fact]
    public async Task GetAsync_Called_DoesNotRaiseChanged()
    {
        var store = NewStore();
        var calls = 0;
        store.Changed += (_, _) => calls++;

        await store.GetAsync(None);
        await store.GetAsync(None);

        Assert.Equal(0, calls);
    }

    [Fact]
    public async Task SaveAsync_SubscriberThrows_DoesNotThrow()
    {
        var store = NewStore();
        store.Changed += (_, _) => throw new InvalidOperationException("boom");

        var exception = await Record.ExceptionAsync(() => store.SaveAsync(NonDefault(), None));

        Assert.Null(exception);
    }

    [Fact]
    public async Task SaveAsync_FirstSubscriberThrows_SecondSubscriberStillCalled()
    {
        var store = NewStore();
        var secondCalls = 0;
        store.Changed += (_, _) => throw new InvalidOperationException("boom");
        store.Changed += (_, _) => secondCalls++;

        await store.SaveAsync(NonDefault(), None);

        Assert.Equal(1, secondCalls);
    }

    [Fact]
    public async Task SaveAsync_SubscriberThrows_ValueIsStillStored()
    {
        var store = NewStore();
        store.Changed += (_, _) => throw new InvalidOperationException("boom");
        var expected = NonDefault();
        await store.SaveAsync(expected, None);

        var loaded = await NewStore().GetAsync(None);

        Assert.Equal(expected, loaded);
    }

    [Fact]
    public async Task GetAsync_RowChangedBehindTheStore_ReturnsCachedValue()
    {
        var store = NewStore();
        await store.GetAsync(None);
        await _repository.SetJsonAsync(UiSettingsStore.SettingsKey, "{\"SkipSeconds\":60}", None);

        var value = await store.GetAsync(None);

        Assert.Equal(new UiSettings(), value);
    }

    [Fact]
    public async Task GetAsync_AfterSave_ReturnsSavedValueOnSameStore()
    {
        var store = NewStore();
        await store.GetAsync(None);
        var expected = NonDefault();
        await store.SaveAsync(expected, None);

        var value = await store.GetAsync(None);

        Assert.Equal(expected, value);
    }

    [Fact]
    public async Task SaveAsync_SeveralSaves_LeavesOneRowForTheKey()
    {
        var store = NewStore();
        await store.SaveAsync(new UiSettings { SkipSeconds = 5 }, None);
        await store.SaveAsync(new UiSettings { SkipSeconds = 15 }, None);
        await store.SaveAsync(NonDefault(), None);

        Assert.Equal(1, await CountRowsAsync("ui_settings"));
    }

    [Fact]
    public async Task SaveAsync_SeveralSaves_LeavesNoOtherRows()
    {
        var store = NewStore();
        await store.SaveAsync(new UiSettings { SkipSeconds = 5 }, None);
        await store.SaveAsync(NonDefault(), None);

        Assert.Equal(1, await CountRowsAsync(null));
    }

    [Fact]
    public async Task GetAsync_TwentyConcurrentCalls_AllReturnEqualValues()
    {
        await StoreRawAsync("{\"SkipSeconds\":15}");
        var store = NewStore();

        var results = await Task.WhenAll(Enumerable.Range(0, 20).Select(_ => Task.Run(() => store.GetAsync(None))));

        Assert.All(results, r => Assert.Equal(new UiSettings { SkipSeconds = 15 }, r));
    }

    [Fact]
    public async Task SaveAsync_CancelledToken_Throws()
    {
        var store = NewStore();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => store.SaveAsync(NonDefault(), Cancelled));
    }

    [Fact]
    public async Task SaveAsync_CancelledToken_StoresNothing()
    {
        var store = NewStore();

        await Record.ExceptionAsync(() => store.SaveAsync(NonDefault(), Cancelled));

        Assert.Null(await _repository.GetJsonAsync(UiSettingsStore.SettingsKey, None));
    }

    [Fact]
    public async Task SaveAsync_CancelledToken_DoesNotRaiseChanged()
    {
        var store = NewStore();
        var calls = 0;
        store.Changed += (_, _) => calls++;

        await Record.ExceptionAsync(() => store.SaveAsync(NonDefault(), Cancelled));

        Assert.Equal(0, calls);
    }

    [Fact]
    public async Task GetAsync_CancelledTokenAndEmptyCache_Throws()
    {
        var store = NewStore();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => store.GetAsync(Cancelled));
    }

    [Fact]
    public async Task GetAsync_CancelledTokenAndFilledCache_ReturnsCachedValue()
    {
        var store = NewStore();
        var expected = NonDefault();
        await store.SaveAsync(expected, None);

        var value = await store.GetAsync(Cancelled);

        Assert.Equal(expected, value);
    }

    [Fact]
    public async Task SaveAsync_NullValue_ThrowsArgumentNullException()
    {
        var store = NewStore();

        await Assert.ThrowsAsync<ArgumentNullException>(() => store.SaveAsync(null!, None));
    }

    // ---- Language ----

    public static TheoryData<AppLanguage> LanguageCases => new()
    {
        AppLanguage.System,
        AppLanguage.English,
        AppLanguage.Hebrew,
    };

    [Fact]
    public void UiSettings_Default_LanguageIsSystem()
    {
        Assert.Equal(AppLanguage.System, new UiSettings().Language);
    }

    [Fact]
    public async Task GetAsync_NoRow_LanguageIsSystem()
    {
        var value = await NewStore().GetAsync(None);

        Assert.Equal(AppLanguage.System, value.Language);
    }

    [Theory]
    [MemberData(nameof(LanguageCases))]
    public async Task GetAsync_LanguageSavedOnOtherStore_RoundTrips(AppLanguage language)
    {
        await NewStore().SaveAsync(new UiSettings { Language = language }, None);

        var loaded = await NewStore().GetAsync(None);

        Assert.Equal(language, loaded.Language);
    }

    [Theory]
    [MemberData(nameof(LanguageCases))]
    public async Task GetAsync_LanguageSavedOnOtherStore_KeepsOtherMembers(AppLanguage language)
    {
        var expected = NonDefault() with { Language = language };
        await NewStore().SaveAsync(expected, None);

        var loaded = await NewStore().GetAsync(None);

        Assert.Equal(expected, loaded);
    }

    [Theory]
    [MemberData(nameof(LanguageCases))]
    public async Task GetAsync_LanguageSavedOnSameStore_ReturnsSavedLanguage(AppLanguage language)
    {
        var store = NewStore();
        await store.SaveAsync(new UiSettings { Language = language }, None);

        var value = await store.GetAsync(None);

        Assert.Equal(language, value.Language);
    }

    [Fact]
    public async Task SaveAsync_HebrewLanguage_StoresNameInRow()
    {
        await NewStore().SaveAsync(new UiSettings { Language = AppLanguage.Hebrew }, None);

        var json = await _repository.GetJsonAsync(UiSettingsStore.SettingsKey, None);

        Assert.Contains("\"Hebrew\"", json);
    }

    [Fact]
    public async Task SaveAsync_HebrewLanguage_RowHasLanguageAsString()
    {
        await NewStore().SaveAsync(new UiSettings { Language = AppLanguage.Hebrew }, None);

        var json = await _repository.GetJsonAsync(UiSettingsStore.SettingsKey, None);

        using var document = System.Text.Json.JsonDocument.Parse(json!);
        var element = document.RootElement.GetProperty("Language");
        Assert.Equal(System.Text.Json.JsonValueKind.String, element.ValueKind);
        Assert.Equal("Hebrew", element.GetString());
    }

    [Fact]
    public async Task GetAsync_OldRowWithoutLanguage_LanguageIsSystem()
    {
        var value = await LoadRawAsync("{\"SkipSeconds\":15,\"NewTagDays\":12}");

        Assert.Equal(AppLanguage.System, value.Language);
    }

    [Fact]
    public async Task GetAsync_OldRowWithoutLanguage_KeepsOtherStoredMembers()
    {
        var value = await LoadRawAsync("{\"SkipSeconds\":15,\"NewTagDays\":12}");

        Assert.Equal(15, value.SkipSeconds);
        Assert.Equal(12, value.NewTagDays);
    }

    [Theory]
    [InlineData("\"Klingon\"")]
    [InlineData("\"\"")]
    [InlineData("null")]
    public async Task GetAsync_UnknownEmptyOrNullLanguage_LanguageIsSystem(string literal)
    {
        var value = await LoadRawAsync($"{{\"Language\":{literal}}}");

        Assert.Equal(AppLanguage.System, value.Language);
    }

    [Theory]
    [InlineData("\"Klingon\"")]
    [InlineData("\"\"")]
    [InlineData("null")]
    public async Task GetAsync_UnknownEmptyOrNullLanguage_KeepsOtherStoredMembers(string literal)
    {
        var value = await LoadRawAsync($"{{\"Language\":{literal},\"SkipSeconds\":15,\"NewTagDays\":12}}");

        Assert.Equal(15, value.SkipSeconds);
        Assert.Equal(12, value.NewTagDays);
    }

    [Theory]
    [InlineData("hebrew", AppLanguage.Hebrew)]
    [InlineData("HEBREW", AppLanguage.Hebrew)]
    [InlineData("english", AppLanguage.English)]
    [InlineData("sYsTeM", AppLanguage.System)]
    public async Task GetAsync_LanguageNameInOtherCase_IsAccepted(string name, AppLanguage expected)
    {
        var value = await LoadRawAsync($"{{\"Language\":\"{name}\"}}");

        Assert.Equal(expected, value.Language);
    }

    [Theory]
    [InlineData("2", AppLanguage.Hebrew)]
    [InlineData("1", AppLanguage.English)]
    [InlineData("0", AppLanguage.System)]
    public async Task GetAsync_DefinedNumberInString_IsAccepted(string number, AppLanguage expected)
    {
        var value = await LoadRawAsync($"{{\"Language\":\"{number}\"}}");

        Assert.Equal(expected, value.Language);
    }

    [Theory]
    [InlineData("7")]
    [InlineData("-1")]
    public async Task GetAsync_UndefinedNumberInString_LanguageIsSystem(string number)
    {
        var value = await LoadRawAsync($"{{\"Language\":\"{number}\"}}");

        Assert.Equal(AppLanguage.System, value.Language);
    }

    [Theory]
    [InlineData("7")]
    [InlineData("-1")]
    public async Task GetAsync_UndefinedJsonNumber_LanguageIsSystem(string number)
    {
        var value = await LoadRawAsync($"{{\"Language\":{number}}}");

        Assert.Equal(AppLanguage.System, value.Language);
    }

    [Fact]
    public async Task GetAsync_DefinedJsonNumber_LanguageIsSystemOrHebrew()
    {
        var value = await LoadRawAsync("{\"Language\":2}");

        Assert.True(
            value.Language is AppLanguage.System or AppLanguage.Hebrew,
            $"Unexpected language {value.Language}");
    }

    [Fact]
    public async Task GetAsync_JsonNumberLanguage_KeepsOtherStoredMembers()
    {
        var value = await LoadRawAsync("{\"Language\":7,\"SkipSeconds\":15,\"NewTagDays\":12}");

        Assert.Equal(15, value.SkipSeconds);
        Assert.Equal(12, value.NewTagDays);
    }

    [Theory]
    [MemberData(nameof(LanguageCases))]
    public async Task SaveAsync_Language_RaisesChangedWithSavedLanguage(AppLanguage language)
    {
        var store = NewStore();
        UiSettings? raised = null;
        store.Changed += (_, v) => raised = v;

        await store.SaveAsync(new UiSettings { Language = language }, None);

        Assert.Equal(language, raised!.Language);
    }

    [Fact]
    public async Task SaveAsync_UndefinedLanguage_StoresSystem()
    {
        await NewStore().SaveAsync(new UiSettings { Language = (AppLanguage)42 }, None);

        var loaded = await NewStore().GetAsync(None);

        Assert.Equal(AppLanguage.System, loaded.Language);
    }

    [Fact]
    public async Task SaveAsync_UndefinedLanguage_RowHoldsSystemName()
    {
        await NewStore().SaveAsync(new UiSettings { Language = (AppLanguage)42 }, None);

        var json = await _repository.GetJsonAsync(UiSettingsStore.SettingsKey, None);

        Assert.Contains("\"System\"", json);
        Assert.DoesNotContain("42", json);
    }

    [Fact]
    public async Task SaveAsync_UndefinedLanguage_RaisesChangedWithSystem()
    {
        var store = NewStore();
        UiSettings? raised = null;
        store.Changed += (_, v) => raised = v;

        await store.SaveAsync(new UiSettings { Language = (AppLanguage)42 }, None);

        Assert.Equal(AppLanguage.System, raised!.Language);
    }

    [Fact]
    public async Task SaveAsync_UndefinedLanguage_ReturnsSystemOnSameStore()
    {
        var store = NewStore();
        await store.SaveAsync(new UiSettings { Language = (AppLanguage)42 }, None);

        var value = await store.GetAsync(None);

        Assert.Equal(AppLanguage.System, value.Language);
    }

    // ---- Round 3: Layout, ExplorerFontScale, IncludeAudioOnlyFolders, LongPressSpeed, Tree and Favorites ----

    private static UiSettings NonDefaultRound3() => NonDefault() with
    {
        Layout = ExplorerLayout.Grid,
        ExplorerFontScale = 1.3,
        IncludeAudioOnlyFolders = false,
        LongPressSpeed = 3.5f,
        ViewMode = ExplorerViewMode.Tree,
    };

    private async Task<System.Text.Json.JsonElement> RowMemberAsync(string name)
    {
        var json = await _repository.GetJsonAsync(UiSettingsStore.SettingsKey, None);
        using var document = System.Text.Json.JsonDocument.Parse(json!);
        return document.RootElement.GetProperty(name).Clone();
    }

    [Fact]
    public void UiSettings_Default_NewFieldsHaveTheDocumentedDefaults()
    {
        var value = new UiSettings();

        Assert.Equal(ExplorerLayout.List, value.Layout);
        Assert.Equal(1.0, value.ExplorerFontScale);
        Assert.True(value.IncludeAudioOnlyFolders);
        Assert.Equal(2.0f, value.LongPressSpeed);
    }

    [Fact]
    public async Task GetAsync_NoRow_NewFieldsAreDefaults()
    {
        var value = await NewStore().GetAsync(None);

        Assert.Equal(ExplorerLayout.List, value.Layout);
        Assert.Equal(1.0, value.ExplorerFontScale);
        Assert.True(value.IncludeAudioOnlyFolders);
        Assert.Equal(2.0f, value.LongPressSpeed);
    }

    [Fact]
    public async Task GetAsync_AfterSaveOnNewStore_RoundTripsEveryMemberIncludingNewFields()
    {
        var expected = NonDefaultRound3();
        await NewStore().SaveAsync(expected, None);

        var loaded = await NewStore().GetAsync(None);

        Assert.Equal(expected, loaded);
    }

    [Theory]
    [InlineData(ExplorerLayout.List)]
    [InlineData(ExplorerLayout.Grid)]
    public async Task GetAsync_LayoutSavedOnOtherStore_RoundTrips(ExplorerLayout layout)
    {
        await NewStore().SaveAsync(new UiSettings { Layout = layout }, None);

        Assert.Equal(layout, (await NewStore().GetAsync(None)).Layout);
    }

    [Theory]
    [InlineData(ExplorerViewMode.Folders)]
    [InlineData(ExplorerViewMode.Flat)]
    [InlineData(ExplorerViewMode.Tree)]
    [InlineData(ExplorerViewMode.Favorites)]
    public async Task GetAsync_ViewModeSavedOnOtherStore_RoundTrips(ExplorerViewMode mode)
    {
        await NewStore().SaveAsync(new UiSettings { ViewMode = mode }, None);

        Assert.Equal(mode, (await NewStore().GetAsync(None)).ViewMode);
    }

    [Theory]
    [InlineData(0.8)]
    [InlineData(1.0)]
    [InlineData(1.3)]
    [InlineData(1.6)]
    public async Task GetAsync_ExplorerFontScaleSavedOnOtherStore_RoundTrips(double scale)
    {
        await NewStore().SaveAsync(new UiSettings { ExplorerFontScale = scale }, None);

        Assert.Equal(scale, (await NewStore().GetAsync(None)).ExplorerFontScale);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task GetAsync_IncludeAudioOnlyFoldersSavedOnOtherStore_RoundTrips(bool include)
    {
        await NewStore().SaveAsync(new UiSettings { IncludeAudioOnlyFolders = include }, None);

        Assert.Equal(include, (await NewStore().GetAsync(None)).IncludeAudioOnlyFolders);
    }

    [Theory]
    [InlineData(0.25f)]
    [InlineData(1.0f)]
    [InlineData(2.0f)]
    [InlineData(3.5f)]
    [InlineData(4.0f)]
    public async Task GetAsync_LongPressSpeedSavedOnOtherStore_RoundTrips(float speed)
    {
        await NewStore().SaveAsync(new UiSettings { LongPressSpeed = speed }, None);

        Assert.Equal(speed, (await NewStore().GetAsync(None)).LongPressSpeed);
    }

    [Fact]
    public async Task GetAsync_NewFieldsSavedOnSameStore_ReturnsThem()
    {
        var store = NewStore();
        var expected = NonDefaultRound3();
        await store.SaveAsync(expected, None);

        Assert.Equal(expected, await store.GetAsync(None));
    }

    [Fact]
    public async Task GetAsync_OldRowWithoutNewFields_LoadsTheDefaultsAndKeepsOtherValues()
    {
        var value = await LoadRawAsync(
            "{\"SkipSeconds\":15,\"NewTagDays\":12,\"Language\":\"Hebrew\",\"ViewMode\":\"Flat\",\"ShowClock\":false}");

        Assert.Equal(ExplorerLayout.List, value.Layout);
        Assert.Equal(1.0, value.ExplorerFontScale);
        Assert.True(value.IncludeAudioOnlyFolders);
        Assert.Equal(2.0f, value.LongPressSpeed);
        Assert.Equal(15, value.SkipSeconds);
        Assert.Equal(12, value.NewTagDays);
        Assert.Equal(AppLanguage.Hebrew, value.Language);
        Assert.Equal(ExplorerViewMode.Flat, value.ViewMode);
        Assert.False(value.ShowClock);
    }

    [Fact]
    public async Task GetAsync_OnlyOneNewFieldStored_TheOthersTakeTheDefaults()
    {
        var value = await LoadRawAsync("{\"LongPressSpeed\":3}");

        Assert.Equal(3.0f, value.LongPressSpeed);
        Assert.Equal(ExplorerLayout.List, value.Layout);
        Assert.Equal(1.0, value.ExplorerFontScale);
        Assert.True(value.IncludeAudioOnlyFolders);
    }

    // ---- Round 3: clamping and rounding ----

    [Theory]
    [InlineData(0.5, 0.8)]
    [InlineData(0.8, 0.8)]
    [InlineData(0.84, 0.8)]
    [InlineData(1.04, 1.0)]
    [InlineData(1.26, 1.3)]
    [InlineData(1.6, 1.6)]
    [InlineData(1.64, 1.6)]
    [InlineData(3.0, 1.6)]
    [InlineData(-1.0, 0.8)]
    [InlineData(0.0, 0.8)]
    [InlineData(double.PositiveInfinity, 1.6)]
    [InlineData(double.NegativeInfinity, 0.8)]
    [InlineData(double.NaN, 1.0)]
    public async Task SaveAsync_ExplorerFontScale_IsClampedAndRoundedToOneDecimal(double given, double expected)
    {
        var store = NewStore();
        UiSettings? raised = null;
        store.Changed += (_, v) => raised = v;

        await store.SaveAsync(new UiSettings { ExplorerFontScale = given }, None);

        Assert.Equal(expected, raised!.ExplorerFontScale);
        Assert.Equal(expected, (await store.GetAsync(None)).ExplorerFontScale);
        Assert.Equal(expected, (await NewStore().GetAsync(None)).ExplorerFontScale);
    }

    [Theory]
    [InlineData(0.1f, 0.25f)]
    [InlineData(0.25f, 0.25f)]
    [InlineData(0.3f, 0.25f)]
    [InlineData(0.4f, 0.5f)]
    [InlineData(1.1f, 1.0f)]
    [InlineData(1.13f, 1.25f)]
    [InlineData(2.0f, 2.0f)]
    [InlineData(3.9f, 4.0f)]
    [InlineData(4.0f, 4.0f)]
    [InlineData(9.0f, 4.0f)]
    [InlineData(0.0f, 0.25f)]
    [InlineData(-1.0f, 0.25f)]
    [InlineData(float.PositiveInfinity, 4.0f)]
    [InlineData(float.NegativeInfinity, 0.25f)]
    [InlineData(float.NaN, 2.0f)]
    public async Task SaveAsync_LongPressSpeed_IsClampedAndRoundedToQuarterSteps(float given, float expected)
    {
        var store = NewStore();
        UiSettings? raised = null;
        store.Changed += (_, v) => raised = v;

        await store.SaveAsync(new UiSettings { LongPressSpeed = given }, None);

        Assert.Equal(expected, raised!.LongPressSpeed);
        Assert.Equal(expected, (await store.GetAsync(None)).LongPressSpeed);
        Assert.Equal(expected, (await NewStore().GetAsync(None)).LongPressSpeed);
    }

    [Theory]
    [InlineData("0.5", 0.8)]
    [InlineData("2", 1.6)]
    [InlineData("1.26", 1.3)]
    [InlineData("1.04", 1.0)]
    [InlineData("-3", 0.8)]
    public async Task GetAsync_StoredExplorerFontScaleOutOfRange_IsClampedAndRounded(string stored, double expected)
    {
        var value = await LoadRawAsync($"{{\"ExplorerFontScale\":{stored}}}");

        Assert.Equal(expected, value.ExplorerFontScale);
    }

    [Theory]
    [InlineData("0.1", 0.25f)]
    [InlineData("9", 4.0f)]
    [InlineData("3.9", 4.0f)]
    [InlineData("0.4", 0.5f)]
    [InlineData("-2", 0.25f)]
    public async Task GetAsync_StoredLongPressSpeedOutOfRange_IsClampedAndRounded(string stored, float expected)
    {
        var value = await LoadRawAsync($"{{\"LongPressSpeed\":{stored}}}");

        Assert.Equal(expected, value.LongPressSpeed);
    }

    [Fact]
    public async Task SaveAsync_FontScaleAndLongPressSpeedOutOfRange_KeepsTheOtherMembers()
    {
        var given = NonDefaultRound3() with { ExplorerFontScale = 9, LongPressSpeed = 9f };

        await NewStore().SaveAsync(given, None);

        var loaded = await NewStore().GetAsync(None);
        Assert.Equal(given with { ExplorerFontScale = 1.6, LongPressSpeed = 4.0f }, loaded);
    }

    // ---- Round 3: enums by name ----

    [Theory]
    [InlineData(ExplorerLayout.List, "List")]
    [InlineData(ExplorerLayout.Grid, "Grid")]
    public async Task SaveAsync_Layout_IsStoredByName(ExplorerLayout layout, string name)
    {
        await NewStore().SaveAsync(new UiSettings { Layout = layout }, None);

        var element = await RowMemberAsync("Layout");

        Assert.Equal(System.Text.Json.JsonValueKind.String, element.ValueKind);
        Assert.Equal(name, element.GetString());
    }

    [Theory]
    [InlineData(ExplorerViewMode.Tree, "Tree")]
    [InlineData(ExplorerViewMode.Favorites, "Favorites")]
    public async Task SaveAsync_NewViewModes_AreStoredByName(ExplorerViewMode mode, string name)
    {
        await NewStore().SaveAsync(new UiSettings { ViewMode = mode }, None);

        var element = await RowMemberAsync("ViewMode");

        Assert.Equal(System.Text.Json.JsonValueKind.String, element.ValueKind);
        Assert.Equal(name, element.GetString());
    }

    [Fact]
    public async Task SaveAsync_FontScaleSpeedAndInclude_AreStoredAsNumbersAndBoolean()
    {
        await NewStore().SaveAsync(
            new UiSettings { ExplorerFontScale = 1.3, LongPressSpeed = 3.5f, IncludeAudioOnlyFolders = false }, None);

        Assert.Equal(System.Text.Json.JsonValueKind.Number, (await RowMemberAsync("ExplorerFontScale")).ValueKind);
        Assert.Equal(System.Text.Json.JsonValueKind.Number, (await RowMemberAsync("LongPressSpeed")).ValueKind);
        Assert.Equal(System.Text.Json.JsonValueKind.False, (await RowMemberAsync("IncludeAudioOnlyFolders")).ValueKind);
    }

    [Theory]
    [InlineData("\"Grid\"", ExplorerLayout.Grid)]
    [InlineData("\"grid\"", ExplorerLayout.Grid)]
    [InlineData("\"LIST\"", ExplorerLayout.List)]
    public async Task GetAsync_LayoutNameInAnyCase_IsAccepted(string literal, ExplorerLayout expected)
    {
        var value = await LoadRawAsync($"{{\"Layout\":{literal}}}");

        Assert.Equal(expected, value.Layout);
    }

    [Theory]
    [InlineData("\"Tree\"", ExplorerViewMode.Tree)]
    [InlineData("\"favorites\"", ExplorerViewMode.Favorites)]
    public async Task GetAsync_NewViewModeNames_AreAccepted(string literal, ExplorerViewMode expected)
    {
        var value = await LoadRawAsync($"{{\"ViewMode\":{literal}}}");

        Assert.Equal(expected, value.ViewMode);
    }

    [Theory]
    [InlineData("\"Klingon\"")]
    [InlineData("\"\"")]
    [InlineData("7")]
    [InlineData("-1")]
    public async Task GetAsync_UnknownLayout_FallsBackAloneAndKeepsOtherMembers(string literal)
    {
        var value = await LoadRawAsync($"{{\"Layout\":{literal},\"SkipSeconds\":15,\"LongPressSpeed\":3}}");

        Assert.Equal(ExplorerLayout.List, value.Layout);
        Assert.Equal(15, value.SkipSeconds);
        Assert.Equal(3.0f, value.LongPressSpeed);
    }

    [Fact]
    public async Task SaveAsync_UndefinedLayout_StoresList()
    {
        await NewStore().SaveAsync(new UiSettings { Layout = (ExplorerLayout)42 }, None);

        Assert.Equal(ExplorerLayout.List, (await NewStore().GetAsync(None)).Layout);
        Assert.Equal("List", (await RowMemberAsync("Layout")).GetString());
    }

    [Fact]
    public async Task SaveAsync_UndefinedViewMode_StoresFolders()
    {
        await NewStore().SaveAsync(new UiSettings { ViewMode = (ExplorerViewMode)42 }, None);

        Assert.Equal(ExplorerViewMode.Folders, (await NewStore().GetAsync(None)).ViewMode);
    }

    // ---- Round 3: a field of the wrong JSON kind falls back alone ----

    [Theory]
    [InlineData("true")]
    [InlineData("null")]
    [InlineData("[1]")]
    [InlineData("{}")]
    public async Task GetAsync_LayoutOfTheWrongJsonKind_FallsBackAlone(string literal)
    {
        var value = await LoadRawAsync(
            $"{{\"Layout\":{literal},\"ExplorerFontScale\":1.3,\"IncludeAudioOnlyFolders\":false,\"LongPressSpeed\":3,\"SkipSeconds\":15}}");

        Assert.Equal(ExplorerLayout.List, value.Layout);
        Assert.Equal(1.3, value.ExplorerFontScale);
        Assert.False(value.IncludeAudioOnlyFolders);
        Assert.Equal(3.0f, value.LongPressSpeed);
        Assert.Equal(15, value.SkipSeconds);
    }

    [Theory]
    [InlineData("\"big\"")]
    [InlineData("true")]
    [InlineData("null")]
    [InlineData("[1]")]
    [InlineData("{}")]
    public async Task GetAsync_ExplorerFontScaleOfTheWrongJsonKind_FallsBackAlone(string literal)
    {
        var value = await LoadRawAsync(
            $"{{\"Layout\":\"Grid\",\"ExplorerFontScale\":{literal},\"IncludeAudioOnlyFolders\":false,\"LongPressSpeed\":3,\"SkipSeconds\":15}}");

        Assert.Equal(1.0, value.ExplorerFontScale);
        Assert.Equal(ExplorerLayout.Grid, value.Layout);
        Assert.False(value.IncludeAudioOnlyFolders);
        Assert.Equal(3.0f, value.LongPressSpeed);
        Assert.Equal(15, value.SkipSeconds);
    }

    [Theory]
    [InlineData("\"yes\"")]
    [InlineData("1")]
    [InlineData("null")]
    [InlineData("[true]")]
    [InlineData("{}")]
    public async Task GetAsync_IncludeAudioOnlyFoldersOfTheWrongJsonKind_FallsBackAlone(string literal)
    {
        var value = await LoadRawAsync(
            $"{{\"Layout\":\"Grid\",\"ExplorerFontScale\":1.3,\"IncludeAudioOnlyFolders\":{literal},\"LongPressSpeed\":3,\"SkipSeconds\":15}}");

        Assert.True(value.IncludeAudioOnlyFolders);
        Assert.Equal(ExplorerLayout.Grid, value.Layout);
        Assert.Equal(1.3, value.ExplorerFontScale);
        Assert.Equal(3.0f, value.LongPressSpeed);
        Assert.Equal(15, value.SkipSeconds);
    }

    [Theory]
    [InlineData("\"fast\"")]
    [InlineData("true")]
    [InlineData("null")]
    [InlineData("[3]")]
    [InlineData("{}")]
    public async Task GetAsync_LongPressSpeedOfTheWrongJsonKind_FallsBackAlone(string literal)
    {
        var value = await LoadRawAsync(
            $"{{\"Layout\":\"Grid\",\"ExplorerFontScale\":1.3,\"IncludeAudioOnlyFolders\":false,\"LongPressSpeed\":{literal},\"SkipSeconds\":15}}");

        Assert.Equal(2.0f, value.LongPressSpeed);
        Assert.Equal(ExplorerLayout.Grid, value.Layout);
        Assert.Equal(1.3, value.ExplorerFontScale);
        Assert.False(value.IncludeAudioOnlyFolders);
        Assert.Equal(15, value.SkipSeconds);
    }

    [Fact]
    public async Task GetAsync_AllNewFieldsOfTheWrongJsonKind_TakeTheDefaultsAndKeepOldMembers()
    {
        var value = await LoadRawAsync(
            "{\"Layout\":false,\"ExplorerFontScale\":\"x\",\"IncludeAudioOnlyFolders\":\"x\",\"LongPressSpeed\":\"x\",\"SkipSeconds\":15,\"NewTagDays\":12}");

        Assert.Equal(new UiSettings { SkipSeconds = 15, NewTagDays = 12 }, value);
    }

    [Fact]
    public async Task SaveAsync_NewFields_RaiseChangedWithTheNewFields()
    {
        var store = NewStore();
        UiSettings? raised = null;
        store.Changed += (_, v) => raised = v;
        var expected = NonDefaultRound3();

        await store.SaveAsync(expected, None);

        Assert.Equal(expected, raised);
    }
}
