using System.Globalization;
using Player.Core.Models;
using Player.Data.Database;
using Player.Data.GlobalDefaultsStorage;
using Player.Data.Tests.Support;

namespace Player.Data.Tests.GlobalDefaultsStorage;

public sealed class GlobalDefaultsStoreTests : TempDatabaseTestBase
{
    private static CancellationToken None => CancellationToken.None;

    private static string Num(double value) => value.ToString("R", CultureInfo.InvariantCulture);

    private (GlobalDefaultsStore Store, SettingsRepository Settings, DbConnectionProvider Provider) CreateStore()
    {
        var provider = CreateProvider();
        var settings = new SettingsRepository(provider);
        return (new GlobalDefaultsStore(settings), settings, provider);
    }

    private GlobalDefaultsStore NewStoreOverSameFile() => new(new SettingsRepository(CreateProvider()));

    private async Task<GlobalDefaults> LoadFromJsonAsync(string json)
    {
        var (store, settings, _) = CreateStore();
        await settings.SetJsonAsync(GlobalDefaultsStore.SettingsKey, json, None);
        return await store.GetAsync(None);
    }

    /// <summary>Saves through one store, returns the value seen by Changed and the value a new store reads back.</summary>
    private async Task<(GlobalDefaults Changed, GlobalDefaults Reloaded)> SaveAndObserveAsync(GlobalDefaults value)
    {
        var (store, _, _) = CreateStore();
        GlobalDefaults? changed = null;
        store.Changed += (_, v) => changed = v;
        await store.SaveAsync(value, None);
        var reloaded = await NewStoreOverSameFile().GetAsync(None);
        return (Assert.IsType<GlobalDefaults>(changed), reloaded);
    }

    private static GlobalDefaults WithStyle(Func<SubtitleStyle, SubtitleStyle> change) =>
        new() { SubtitleStyle = change(SubtitleStyle.Default) };

    [Fact]
    public async Task GetAsync_NoRow_ReturnsDefaults()
    {
        var (store, _, _) = CreateStore();

        var value = await store.GetAsync(None);

        Assert.Equal(new GlobalDefaults(), value);
    }

    [Fact]
    public async Task GetAsync_AfterSaveOnOtherInstance_RoundTripsEveryMember()
    {
        var expected = new GlobalDefaults
        {
            Speed = 1.5f,
            Decoder = DecoderMode.Software,
            Aspect = AspectMode.Crop,
            Repeat = RepeatMode.Folder,
            ResumeFromLastPosition = false,
            SubtitlesEnabled = false,
            PreferredAudioLanguage = "he",
            PreferredSubtitleLanguage = "en",
            AudioDelayMs = -250,
            SubtitleDelayMs = 1200,
            SubtitleStyle = new SubtitleStyle(1.5, 0xFF112233, 0x80445566, 3, 0.5, true, 0.1),
            BackgroundAudio = true,
            PictureInPictureOnLeave = true
        };
        var (store, _, _) = CreateStore();
        await store.SaveAsync(expected, None);

        var loaded = await NewStoreOverSameFile().GetAsync(None);

        Assert.Equal(expected, loaded);
    }

    [Fact]
    public async Task GetAsync_StoredEmptyObject_ReturnsDefaults()
    {
        var value = await LoadFromJsonAsync("{}");

        Assert.Equal(new GlobalDefaults(), value);
    }

    [Fact]
    public async Task GetAsync_StoredOnlyRepeat_KeepsRepeatAndDefaultsForTheRest()
    {
        var value = await LoadFromJsonAsync("{\"Repeat\":\"Folder\"}");

        Assert.Equal(new GlobalDefaults { Repeat = RepeatMode.Folder }, value);
        Assert.Equal(RepeatMode.Folder, value.Repeat);
        Assert.Equal(1.0f, value.Speed);
        Assert.True(value.ResumeFromLastPosition);
        Assert.True(value.SubtitlesEnabled);
        Assert.Equal(SubtitleStyle.Default, value.SubtitleStyle);
    }

    [Fact]
    public async Task GetAsync_StoredOnlyFontScale_KeepsOtherStyleMembersAtDefault()
    {
        var value = await LoadFromJsonAsync("{\"SubtitleStyle\":{\"FontScale\":2}}");

        Assert.Equal(2.0, value.SubtitleStyle.FontScale);
        Assert.Equal(0xFFFFFFFFu, value.SubtitleStyle.TextColorArgb);
        Assert.Equal(SubtitleStyle.Default with { FontScale = 2.0 }, value.SubtitleStyle);
    }

    [Fact]
    public async Task GetAsync_UnknownDecoderName_KeepsDefaultDecoderAndOtherFields()
    {
        var value = await LoadFromJsonAsync("{\"Decoder\":\"Gpu\",\"Speed\":1.5}");

        Assert.Equal(DecoderMode.Hardware, value.Decoder);
        Assert.Equal(1.5f, value.Speed);
    }

    [Fact]
    public async Task GetAsync_EnumNameInOtherCase_IsParsed()
    {
        var value = await LoadFromJsonAsync("{\"Repeat\":\"file\"}");

        Assert.Equal(RepeatMode.File, value.Repeat);
    }

    [Fact]
    public async Task GetAsync_PropertyNameInOtherCase_IsIgnored()
    {
        var value = await LoadFromJsonAsync("{\"repeat\":\"Folder\"}");

        Assert.Equal(new GlobalDefaults(), value);
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("null")]
    public async Task GetAsync_InvalidOrNullJson_ReturnsDefaults(string json)
    {
        var value = await LoadFromJsonAsync(json);

        Assert.Equal(new GlobalDefaults(), value);
    }

    [Theory]
    [InlineData(0.0, 0.25f)]
    [InlineData(0.1, 0.25f)]
    [InlineData(9.0, 4.0f)]
    public async Task GetAsync_StoredSpeedOutOfRange_IsClamped(double stored, float expected)
    {
        var value = await LoadFromJsonAsync("{\"Speed\":" + Num(stored) + "}");

        Assert.Equal(expected, value.Speed);
    }

    [Theory]
    [InlineData(-9000, -5000)]
    [InlineData(9000, 5000)]
    public async Task GetAsync_StoredAudioDelayOutOfRange_IsClamped(long stored, long expected)
    {
        var value = await LoadFromJsonAsync("{\"AudioDelayMs\":" + stored + "}");

        Assert.Equal(expected, value.AudioDelayMs);
    }

    [Theory]
    [InlineData(-9000, -5000)]
    [InlineData(9000, 5000)]
    public async Task GetAsync_StoredSubtitleDelayOutOfRange_IsClamped(long stored, long expected)
    {
        var value = await LoadFromJsonAsync("{\"SubtitleDelayMs\":" + stored + "}");

        Assert.Equal(expected, value.SubtitleDelayMs);
    }

    [Theory]
    [InlineData(0.1, 0.5)]
    [InlineData(9.0, 3.0)]
    public async Task GetAsync_StoredFontScaleOutOfRange_IsClamped(double stored, double expected)
    {
        var value = await LoadFromJsonAsync("{\"SubtitleStyle\":{\"FontScale\":" + Num(stored) + "}}");

        Assert.Equal(expected, value.SubtitleStyle.FontScale);
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(9, 4)]
    public async Task GetAsync_StoredOutlineThicknessOutOfRange_IsClamped(int stored, int expected)
    {
        var value = await LoadFromJsonAsync("{\"SubtitleStyle\":{\"OutlineThickness\":" + stored + "}}");

        Assert.Equal(expected, value.SubtitleStyle.OutlineThickness);
    }

    [Theory]
    [InlineData(-1.0, 0.0)]
    [InlineData(2.0, 1.0)]
    public async Task GetAsync_StoredBackgroundOpacityOutOfRange_IsClamped(double stored, double expected)
    {
        var value = await LoadFromJsonAsync("{\"SubtitleStyle\":{\"BackgroundOpacity\":" + Num(stored) + "}}");

        Assert.Equal(expected, value.SubtitleStyle.BackgroundOpacity);
    }

    [Theory]
    [InlineData(-1.0, 0.0)]
    [InlineData(1.0, 0.3)]
    public async Task GetAsync_StoredBottomMarginOutOfRange_IsClamped(double stored, double expected)
    {
        var value = await LoadFromJsonAsync("{\"SubtitleStyle\":{\"BottomMarginFraction\":" + Num(stored) + "}}");

        Assert.Equal(expected, value.SubtitleStyle.BottomMarginFraction);
    }

    [Theory]
    [InlineData(0.0f, 0.25f)]
    [InlineData(0.1f, 0.25f)]
    [InlineData(9.0f, 4.0f)]
    [InlineData(float.NaN, 1.0f)]
    [InlineData(float.PositiveInfinity, 4.0f)]
    public async Task SaveAsync_SpeedOutOfRange_IsClampedInChangedAndStorage(float speed, float expected)
    {
        var (changed, reloaded) = await SaveAndObserveAsync(new GlobalDefaults { Speed = speed });

        Assert.Equal(expected, changed.Speed);
        Assert.Equal(expected, reloaded.Speed);
    }

    [Theory]
    [InlineData(-9000L, -5000L)]
    [InlineData(9000L, 5000L)]
    public async Task SaveAsync_AudioDelayOutOfRange_IsClampedInChangedAndStorage(long delay, long expected)
    {
        var (changed, reloaded) = await SaveAndObserveAsync(new GlobalDefaults { AudioDelayMs = delay });

        Assert.Equal(expected, changed.AudioDelayMs);
        Assert.Equal(expected, reloaded.AudioDelayMs);
    }

    [Theory]
    [InlineData(-9000L, -5000L)]
    [InlineData(9000L, 5000L)]
    public async Task SaveAsync_SubtitleDelayOutOfRange_IsClampedInChangedAndStorage(long delay, long expected)
    {
        var (changed, reloaded) = await SaveAndObserveAsync(new GlobalDefaults { SubtitleDelayMs = delay });

        Assert.Equal(expected, changed.SubtitleDelayMs);
        Assert.Equal(expected, reloaded.SubtitleDelayMs);
    }

    [Theory]
    [InlineData(0.1, 0.5)]
    [InlineData(9.0, 3.0)]
    [InlineData(double.NaN, 1.0)]
    public async Task SaveAsync_FontScaleOutOfRange_IsClampedInChangedAndStorage(double scale, double expected)
    {
        var (changed, reloaded) = await SaveAndObserveAsync(WithStyle(s => s with { FontScale = scale }));

        Assert.Equal(expected, changed.SubtitleStyle.FontScale);
        Assert.Equal(expected, reloaded.SubtitleStyle.FontScale);
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(9, 4)]
    public async Task SaveAsync_OutlineThicknessOutOfRange_IsClampedInChangedAndStorage(int thickness, int expected)
    {
        var (changed, reloaded) = await SaveAndObserveAsync(WithStyle(s => s with { OutlineThickness = thickness }));

        Assert.Equal(expected, changed.SubtitleStyle.OutlineThickness);
        Assert.Equal(expected, reloaded.SubtitleStyle.OutlineThickness);
    }

    [Theory]
    [InlineData(-1.0, 0.0)]
    [InlineData(2.0, 1.0)]
    [InlineData(double.NaN, 0.0)]
    public async Task SaveAsync_BackgroundOpacityOutOfRange_IsClampedInChangedAndStorage(double opacity, double expected)
    {
        var (changed, reloaded) = await SaveAndObserveAsync(WithStyle(s => s with { BackgroundOpacity = opacity }));

        Assert.Equal(expected, changed.SubtitleStyle.BackgroundOpacity);
        Assert.Equal(expected, reloaded.SubtitleStyle.BackgroundOpacity);
    }

    [Theory]
    [InlineData(-1.0, 0.0)]
    [InlineData(1.0, 0.3)]
    [InlineData(double.NaN, 0.05)]
    public async Task SaveAsync_BottomMarginOutOfRange_IsClampedInChangedAndStorage(double margin, double expected)
    {
        var (changed, reloaded) = await SaveAndObserveAsync(WithStyle(s => s with { BottomMarginFraction = margin }));

        Assert.Equal(expected, changed.SubtitleStyle.BottomMarginFraction);
        Assert.Equal(expected, reloaded.SubtitleStyle.BottomMarginFraction);
    }

    [Fact]
    public async Task SaveAsync_LanguageWithSpaces_IsTrimmed()
    {
        var (changed, reloaded) = await SaveAndObserveAsync(
            new GlobalDefaults { PreferredAudioLanguage = "  he  ", PreferredSubtitleLanguage = "  en " });

        Assert.Equal("he", changed.PreferredAudioLanguage);
        Assert.Equal("he", reloaded.PreferredAudioLanguage);
        Assert.Equal("en", reloaded.PreferredSubtitleLanguage);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task SaveAsync_BlankLanguage_IsStoredAsNull(string language)
    {
        var (changed, reloaded) = await SaveAndObserveAsync(
            new GlobalDefaults { PreferredAudioLanguage = language, PreferredSubtitleLanguage = language });

        Assert.Null(changed.PreferredAudioLanguage);
        Assert.Null(changed.PreferredSubtitleLanguage);
        Assert.Null(reloaded.PreferredAudioLanguage);
        Assert.Null(reloaded.PreferredSubtitleLanguage);
    }

    [Fact]
    public async Task SaveAsync_Called_RaisesChangedExactlyOnceWithNormalizedValue()
    {
        var (store, _, _) = CreateStore();
        var raised = new List<GlobalDefaults>();
        store.Changed += (_, v) => raised.Add(v);

        await store.SaveAsync(new GlobalDefaults { Speed = 9f }, None);

        var single = Assert.Single(raised);
        Assert.Equal(new GlobalDefaults { Speed = 4.0f }, single);
    }

    [Fact]
    public async Task SaveAsync_Called_ValueIsAlreadyStoredWhenChangedIsRaised()
    {
        var (store, _, _) = CreateStore();
        Task<GlobalDefaults>? readInsideHandler = null;
        store.Changed += (_, _) => readInsideHandler = store.GetAsync(None);

        await store.SaveAsync(new GlobalDefaults { Repeat = RepeatMode.File }, None);

        Assert.NotNull(readInsideHandler);
        Assert.Equal(RepeatMode.File, (await readInsideHandler).Repeat);
    }

    [Fact]
    public async Task GetAsync_Called_DoesNotRaiseChanged()
    {
        var (store, _, _) = CreateStore();
        var count = 0;
        store.Changed += (_, _) => count++;

        await store.GetAsync(None);

        Assert.Equal(0, count);
    }

    [Fact]
    public async Task SaveAsync_FirstSubscriberThrows_DoesNotThrowAndSecondSubscriberIsCalled()
    {
        var (store, _, _) = CreateStore();
        var secondCalls = 0;
        store.Changed += (_, _) => throw new InvalidOperationException("boom");
        store.Changed += (_, _) => secondCalls++;

        await store.SaveAsync(new GlobalDefaults(), None);

        Assert.Equal(1, secondCalls);
    }

    [Fact]
    public async Task GetAsync_RowChangedDirectlyAfterFirstGet_ReturnsCachedValue()
    {
        var (store, settings, _) = CreateStore();
        await store.GetAsync(None);
        await settings.SetJsonAsync(GlobalDefaultsStore.SettingsKey, "{\"Repeat\":\"Folder\"}", None);

        var value = await store.GetAsync(None);

        Assert.Equal(new GlobalDefaults(), value);
    }

    [Fact]
    public async Task GetAsync_AfterSave_ReturnsSavedValue()
    {
        var (store, _, _) = CreateStore();
        await store.GetAsync(None);
        await store.SaveAsync(new GlobalDefaults { Repeat = RepeatMode.Folder }, None);

        var value = await store.GetAsync(None);

        Assert.Equal(RepeatMode.Folder, value.Repeat);
    }

    [Fact]
    public async Task SaveAsync_SeveralTimes_LeavesExactlyOneSettingsRowWithTheKey()
    {
        var (store, _, provider) = CreateStore();
        await store.SaveAsync(new GlobalDefaults { Speed = 1.25f }, None);
        await store.SaveAsync(new GlobalDefaults { Speed = 1.5f }, None);
        await store.SaveAsync(new GlobalDefaults { Speed = 2f }, None);
        var connection = await OpenAsync(provider);

        var rows = await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM settings");
        var keyed = await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM settings WHERE key = ?", "global_defaults");

        Assert.Equal(1, rows);
        Assert.Equal(1, keyed);
    }

    [Fact]
    public async Task SaveAsync_Called_DoesNotCreatePlaybackStateTable()
    {
        var (store, _, provider) = CreateStore();
        await store.SaveAsync(new GlobalDefaults(), None);
        var connection = await OpenAsync(provider);

        var tables = await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = 'playback_state'");

        Assert.Equal(0, tables);
    }

    [Fact]
    public async Task GetAsync_20ConcurrentCalls_AllReturnEqualValues()
    {
        var (store, settings, _) = CreateStore();
        await settings.SetJsonAsync(GlobalDefaultsStore.SettingsKey, "{\"Repeat\":\"Folder\",\"Speed\":1.5}", None);

        var results = await Task.WhenAll(Enumerable.Range(0, 20).Select(_ => store.GetAsync(None)));

        var expected = new GlobalDefaults { Repeat = RepeatMode.Folder, Speed = 1.5f };
        Assert.Equal(20, results.Length);
        Assert.All(results, r => Assert.Equal(expected, r));
    }

    [Fact]
    public async Task SaveAsync_CancelledToken_ThrowsAndStoresNothingAndDoesNotRaiseChanged()
    {
        var (store, settings, _) = CreateStore();
        var raised = 0;
        store.Changed += (_, _) => raised++;
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => store.SaveAsync(new GlobalDefaults(), cts.Token));

        Assert.Null(await settings.GetJsonAsync(GlobalDefaultsStore.SettingsKey, None));
        Assert.Equal(0, raised);
    }

    [Fact]
    public async Task GetAsync_CancelledTokenWithEmptyCache_Throws()
    {
        var (store, _, _) = CreateStore();
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => store.GetAsync(cts.Token));
    }

    [Fact]
    public async Task SaveAsync_NullValue_ThrowsArgumentNullException()
    {
        var (store, _, _) = CreateStore();

        await Assert.ThrowsAsync<ArgumentNullException>(() => store.SaveAsync(null!, None));
    }
}
