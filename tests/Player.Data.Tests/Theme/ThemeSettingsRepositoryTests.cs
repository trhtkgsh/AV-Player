using System.Text.Json;
using Player.Core.Models;
using Player.Data.Database;
using Player.Data.Tests.Support;
using Player.Data.ThemeStorage;

namespace Player.Data.Tests.ThemeStorageTests;

public sealed class ThemeSettingsRepositoryTests : TempDatabaseTestBase
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

    private SettingsRepository _settings = null!;
    private DbConnectionProvider? _provider;

    private ThemeSettingsRepository NewRepository()
    {
        _provider ??= CreateProvider();
        _settings = new SettingsRepository(_provider);
        return new ThemeSettingsRepository(_settings);
    }

    private async Task StoreRawAsync(string json)
    {
        NewRepository();
        await _settings.SetJsonAsync(ThemeSettingsRepository.SettingsKey, json, None);
    }

    private async Task<ThemeSettings> LoadRawAsync(string json)
    {
        await StoreRawAsync(json);
        return await NewRepository().LoadAsync(None);
    }

    private async Task<JsonElement> ReadStoredAsync()
    {
        var json = await _settings.GetJsonAsync(ThemeSettingsRepository.SettingsKey, None);
        Assert.NotNull(json);
        return JsonDocument.Parse(json).RootElement.Clone();
    }

    public static TheoryData<ThemeId> AllThemes()
    {
        var data = new TheoryData<ThemeId>();
        foreach (var id in Enum.GetValues<ThemeId>())
        {
            data.Add(id);
        }

        return data;
    }

    [Fact]
    public async Task LoadAsync_NoRow_ReturnsDefaults()
    {
        var value = await NewRepository().LoadAsync(None);

        Assert.Equal(new ThemeSettings(), value);
    }

    [Theory]
    [MemberData(nameof(AllThemes))]
    public async Task LoadAsync_AfterSaveOnNewRepository_ReturnsSameSettings(ThemeId theme)
    {
        var expected = new ThemeSettings { Theme = theme, AccentColorArgb = 0xFFB07CFF, HudOpacity = 0.35 };
        await NewRepository().SaveAsync(expected, None);

        var loaded = await NewRepository().LoadAsync(None);

        Assert.Equal(expected, loaded);
    }

    [Fact]
    public async Task SaveAsync_Theme_IsStoredByName()
    {
        await NewRepository().SaveAsync(new ThemeSettings { Theme = ThemeId.HighContrast }, None);

        var stored = await ReadStoredAsync();

        Assert.Equal(JsonValueKind.String, stored.GetProperty("Theme").ValueKind);
        Assert.Equal("HighContrast", stored.GetProperty("Theme").GetString());
    }

    [Fact]
    public async Task SaveAsync_OutOfRangeSettings_StoresNormalizedValues()
    {
        var input = new ThemeSettings { Theme = (ThemeId)99, AccentColorArgb = 0x004C9AFF, HudOpacity = 5 };

        await NewRepository().SaveAsync(input, None);

        var stored = await ReadStoredAsync();
        Assert.Equal("DarkOled", stored.GetProperty("Theme").GetString());
        Assert.Equal(0xFF4C9AFFu, stored.GetProperty("AccentColorArgb").GetUInt32());
        Assert.Equal(1.0, stored.GetProperty("HudOpacity").GetDouble());
    }

    [Theory]
    [InlineData(-3.0, 0.0)]
    [InlineData(double.NaN, 0.6)]
    [InlineData(double.PositiveInfinity, 1.0)]
    [InlineData(double.NegativeInfinity, 0.0)]
    public async Task SaveAsync_BadOpacity_StoresNormalizedOpacity(double opacity, double expected)
    {
        await NewRepository().SaveAsync(new ThemeSettings { HudOpacity = opacity }, None);

        var stored = await ReadStoredAsync();

        Assert.Equal(expected, stored.GetProperty("HudOpacity").GetDouble());
    }

    [Fact]
    public async Task SaveAsync_Twice_KeepsLastValueOnly()
    {
        var repository = NewRepository();
        await repository.SaveAsync(new ThemeSettings { Theme = ThemeId.Light }, None);
        await repository.SaveAsync(new ThemeSettings { Theme = ThemeId.DarkSlate }, None);

        var loaded = await NewRepository().LoadAsync(None);

        Assert.Equal(ThemeId.DarkSlate, loaded.Theme);
    }

    [Fact]
    public async Task SaveAsync_NullSettings_ThrowsArgumentNullException()
    {
        var repository = NewRepository();

        await Assert.ThrowsAsync<ArgumentNullException>(() => repository.SaveAsync(null!, None));
    }

    [Fact]
    public void Constructor_NullSettingsRepository_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new ThemeSettingsRepository(null!));
    }

    [Theory]
    [InlineData("{not json")]
    [InlineData("garbage")]
    [InlineData("{\"Theme\":")]
    public async Task LoadAsync_BrokenJson_ReturnsDefaults(string json)
    {
        var value = await LoadRawAsync(json);

        Assert.Equal(new ThemeSettings(), value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("{}")]
    [InlineData("null")]
    public async Task LoadAsync_EmptyRow_ReturnsDefaults(string json)
    {
        var value = await LoadRawAsync(json);

        Assert.Equal(new ThemeSettings(), value);
    }

    [Theory]
    [InlineData("[]")]
    [InlineData("[1,2,3]")]
    [InlineData("[{\"Theme\":\"Light\"}]")]
    public async Task LoadAsync_JsonArrayRow_ReturnsDefaults(string json)
    {
        var value = await LoadRawAsync(json);

        Assert.Equal(new ThemeSettings(), value);
    }

    [Fact]
    public async Task LoadAsync_OnlyThemeStored_KeepsDefaultAccentAndOpacity()
    {
        var value = await LoadRawAsync("{\"Theme\":\"Light\"}");

        Assert.Equal(new ThemeSettings { Theme = ThemeId.Light }, value);
    }

    [Fact]
    public async Task LoadAsync_OnlyAccentStored_KeepsDefaultThemeAndOpacity()
    {
        var value = await LoadRawAsync("{\"AccentColorArgb\":4294198070}");

        Assert.Equal(new ThemeSettings { AccentColorArgb = 4294198070 }, value);
    }

    [Fact]
    public async Task LoadAsync_OnlyOpacityStored_KeepsDefaultThemeAndAccent()
    {
        var value = await LoadRawAsync("{\"HudOpacity\":0.25}");

        Assert.Equal(new ThemeSettings { HudOpacity = 0.25 }, value);
    }

    [Fact]
    public async Task LoadAsync_OneFieldWronglyTyped_KeepsTheOtherFields()
    {
        var value = await LoadRawAsync("{\"Theme\":\"Light\",\"AccentColorArgb\":\"red\",\"HudOpacity\":0.25}");

        Assert.Equal(new ThemeSettings { Theme = ThemeId.Light, HudOpacity = 0.25 }, value);
    }

    [Theory]
    [InlineData("Nonexistent")]
    [InlineData("")]
    [InlineData("4")]
    [InlineData("99")]
    public async Task LoadAsync_UnknownThemeName_GivesDarkOled(string name)
    {
        var value = await LoadRawAsync($"{{\"Theme\":\"{name}\"}}");

        Assert.Equal(ThemeId.DarkOled, value.Theme);
    }

    [Theory]
    [InlineData("light", ThemeId.Light)]
    [InlineData("LIGHT", ThemeId.Light)]
    [InlineData("darkslate", ThemeId.DarkSlate)]
    [InlineData("highContrast", ThemeId.HighContrast)]
    public async Task LoadAsync_ThemeNameInOtherCase_IsAccepted(string name, ThemeId expected)
    {
        var value = await LoadRawAsync($"{{\"Theme\":\"{name}\"}}");

        Assert.Equal(expected, value.Theme);
    }

    [Theory]
    [InlineData("0", ThemeId.DarkOled)]
    [InlineData("1", ThemeId.DarkSlate)]
    [InlineData("2", ThemeId.Light)]
    [InlineData("3", ThemeId.HighContrast)]
    public async Task LoadAsync_NumericDefinedTheme_IsAccepted(string number, ThemeId expected)
    {
        var value = await LoadRawAsync($"{{\"Theme\":{number}}}");

        Assert.Equal(expected, value.Theme);
    }

    [Theory]
    [InlineData("4")]
    [InlineData("99")]
    [InlineData("-1")]
    [InlineData("2.5")]
    [InlineData("true")]
    [InlineData("null")]
    [InlineData("[]")]
    public async Task LoadAsync_UndefinedOrNonIntegerTheme_GivesDarkOled(string rawValue)
    {
        var value = await LoadRawAsync($"{{\"Theme\":{rawValue}}}");

        Assert.Equal(ThemeId.DarkOled, value.Theme);
    }

    [Fact]
    public async Task LoadAsync_AccentWithoutAlpha_IsReadOpaque()
    {
        var value = await LoadRawAsync("{\"AccentColorArgb\":5020415}");   // 0x004C9AFF

        Assert.Equal(0xFF4C9AFFu, value.AccentColorArgb);
    }

    [Fact]
    public async Task LoadAsync_AccentWithHalfAlpha_IsReadOpaque()
    {
        var value = await LoadRawAsync("{\"AccentColorArgb\":2152504063}");   // 0x804C9AFF

        Assert.Equal(0xFF4C9AFFu, value.AccentColorArgb);
    }

    [Fact]
    public async Task LoadAsync_AccentAsNegativeInt_IsReadOpaque()
    {
        var negative = unchecked((int)0xFF4C9AFF);

        var value = await LoadRawAsync($"{{\"AccentColorArgb\":{negative}}}");

        Assert.True(negative < 0);
        Assert.Equal(0xFF4C9AFFu, value.AccentColorArgb);
    }

    [Theory]
    [InlineData("\"red\"")]
    [InlineData("1.5")]
    [InlineData("-5000000000")]
    [InlineData("5000000000")]
    [InlineData("null")]
    [InlineData("true")]
    public async Task LoadAsync_AccentNotAnArgbInteger_GivesDefaultAccent(string rawValue)
    {
        var value = await LoadRawAsync($"{{\"AccentColorArgb\":{rawValue}}}");

        Assert.Equal(new ThemeSettings().AccentColorArgb, value.AccentColorArgb);
    }

    [Theory]
    [InlineData("-0.5", 0.0)]
    [InlineData("-100", 0.0)]
    [InlineData("1.5", 1.0)]
    [InlineData("100", 1.0)]
    [InlineData("0", 0.0)]
    [InlineData("1", 1.0)]
    [InlineData("0.4", 0.4)]
    public async Task LoadAsync_OpacityNumber_IsClampedToZeroOne(string rawValue, double expected)
    {
        var value = await LoadRawAsync($"{{\"HudOpacity\":{rawValue}}}");

        Assert.Equal(expected, value.HudOpacity);
    }

    [Theory]
    [InlineData("\"abc\"")]
    [InlineData("\"0.5\"")]
    [InlineData("true")]
    [InlineData("null")]
    [InlineData("{}")]
    [InlineData("[]")]
    public async Task LoadAsync_OpacityNotANumber_GivesDefaultOpacity(string rawValue)
    {
        var value = await LoadRawAsync($"{{\"HudOpacity\":{rawValue}}}");

        Assert.Equal(0.6, value.HudOpacity);
    }

    [Fact]
    public async Task SaveAsync_CancelledToken_ThrowsOperationCanceled()
    {
        var repository = NewRepository();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => repository.SaveAsync(new ThemeSettings(), Cancelled));
    }

    [Fact]
    public async Task SaveAsync_CancelledToken_StoresNothing()
    {
        var repository = NewRepository();

        await Record.ExceptionAsync(() => repository.SaveAsync(new ThemeSettings(), Cancelled));

        Assert.Null(await _settings.GetJsonAsync(ThemeSettingsRepository.SettingsKey, None));
    }

    [Fact]
    public async Task LoadAsync_CancelledToken_ThrowsOperationCanceled()
    {
        var repository = NewRepository();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => repository.LoadAsync(Cancelled));
    }
}
