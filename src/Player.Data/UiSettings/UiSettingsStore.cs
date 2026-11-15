using System.Globalization;
using System.Text.Json;
using Player.Core.Contracts;
using Player.Core.Models;
using Player.Data.Database;

namespace Player.Data.UiSettingsStorage;

public sealed class UiSettingsStore : IUiSettingsStore
{
	public const string SettingsKey = "ui_settings";

	// Version 2 of the row: ExplorerFields is read as stored. Older rows have the former default
	// (Duration, Resolution, Size), so they are read as None until the next save writes version 2.
	private const int CurrentSchemaVersion = 2;

	private static readonly int[] SkipChoices = [5, 10, 15, 30, 60];

	private const double MinFontScale = 0.8;
	private const double MaxFontScale = 1.6;
	private const float MinSpeed = 0.25f;
	private const float MaxSpeed = 4.0f;

	private const ExplorerInfoFields AllExplorerFields =
		ExplorerInfoFields.Duration | ExplorerInfoFields.Resolution | ExplorerInfoFields.Size |
		ExplorerInfoFields.Date | ExplorerInfoFields.Path;

	private readonly SettingsRepository _settings;
	private readonly SemaphoreSlim _gate = new(1, 1);
	private volatile UiSettings? _cache;

	public UiSettingsStore(SettingsRepository settings)
	{
		ArgumentNullException.ThrowIfNull(settings);
		_settings = settings;
	}

	public event EventHandler<UiSettings>? Changed;

	public async Task<UiSettings> GetAsync(CancellationToken ct)
	{
		var cached = _cache;
		if (cached is not null)
		{
			return cached;
		}

		ct.ThrowIfCancellationRequested();
		await _gate.WaitAsync(ct).ConfigureAwait(false);
		try
		{
			if (_cache is { } filled)
			{
				return filled;
			}

			var loaded = await LoadAsync(ct).ConfigureAwait(false);
			var normalized = Normalize(loaded);
			_cache = normalized;
			return normalized;
		}
		finally
		{
			_gate.Release();
		}
	}

	public async Task SaveAsync(UiSettings value, CancellationToken ct)
	{
		ArgumentNullException.ThrowIfNull(value);
		ct.ThrowIfCancellationRequested();

		UiSettings normalized;
		await _gate.WaitAsync(ct).ConfigureAwait(false);
		try
		{
			normalized = Normalize(value);
			var json = JsonSerializer.Serialize(ToDto(normalized), UiSettingsJsonContext.Default.UiSettingsDto);
			await _settings.SetJsonAsync(SettingsKey, json, ct).ConfigureAwait(false);
			_cache = normalized;
		}
		finally
		{
			_gate.Release();
		}

		RaiseChanged(normalized);
	}

	private async Task<UiSettings> LoadAsync(CancellationToken ct)
	{
		var json = await _settings.GetJsonAsync(SettingsKey, ct).ConfigureAwait(false);
		if (json is null)
		{
			return new UiSettings();
		}

		try
		{
			var dto = JsonSerializer.Deserialize(json, UiSettingsJsonContext.Default.UiSettingsDto);
			return FromDto(dto);
		}
		catch (JsonException)
		{
			return new UiSettings();
		}
	}

	private static UiSettingsDto ToDto(UiSettings value) => new()
	{
		SchemaVersion = ToElement(CurrentSchemaVersion),
		HudAutoHideSeconds = ToElement(value.HudAutoHideSeconds),
		SkipSeconds = ToElement(value.SkipSeconds),
		ShowClock = ToElement(value.ShowClock),
		ShowBattery = ToElement(value.ShowBattery),
		ExplorerFields = Name(value.ExplorerFields),
		ViewMode = Name(value.ViewMode),
		SortField = Name(value.SortField),
		SortDirection = Name(value.SortDirection),
		NewTagDays = ToElement(value.NewTagDays),
		Language = Name(value.Language),
		Layout = Name(value.Layout),
		ItemSize = Name(value.ItemSize),
		ExplorerFontScale = ToElement(value.ExplorerFontScale),
		IncludeAudioOnlyFolders = ToElement(value.IncludeAudioOnlyFolders),
		LongPressSpeed = ToElement(value.LongPressSpeed),
		OverlayProgressLine = ToElement(value.Overlay.ProgressLine),
		OverlayProgressLineEdge = Name(value.Overlay.ProgressLineEdge),
		OverlayProgressLineThickness = ToElement(value.Overlay.ProgressLineThickness),
		OverlayClock = ToElement(value.Overlay.Clock),
		OverlayBattery = ToElement(value.Overlay.Battery),
		OverlayTime = Name(value.Overlay.Time),
		OverlayCorner = Name(value.Overlay.Corner),
		OverlayClockCorner = Name(value.Overlay.ClockCorner),
		OverlayBatteryCorner = Name(value.Overlay.BatteryCorner),
		OverlayMarginDp = ToElement(value.Overlay.MarginDp),
		OverlayFontSize = ToElement(value.Overlay.FontSize),
		OverlayColorArgb = ColorHex(value.Overlay.ColorArgb),
	};

	private static JsonElement Name<T>(T value) where T : struct, Enum =>
		JsonSerializer.SerializeToElement(value.ToString(), UiSettingsJsonContext.Default.String);

	private static JsonElement ToElement(int value) =>
		JsonSerializer.SerializeToElement(value, UiSettingsJsonContext.Default.Int32);

	// The color is stored as eight upper-case hex digits, e.g. "FFFFFFFF".
	private static JsonElement ColorHex(uint value) =>
		JsonSerializer.SerializeToElement(value.ToString("X8", CultureInfo.InvariantCulture), UiSettingsJsonContext.Default.String);

	private static JsonElement ToElement(double value) =>
		JsonSerializer.SerializeToElement(value, UiSettingsJsonContext.Default.Double);

	private static JsonElement ToElement(float value) =>
		JsonSerializer.SerializeToElement(value, UiSettingsJsonContext.Default.Single);

	private static JsonElement ToElement(bool value) =>
		JsonSerializer.SerializeToElement(value, UiSettingsJsonContext.Default.Boolean);

	// Each field falls back on its own: a missing or unreadable field takes the record default
	// and never discards the others.
	private static UiSettings FromDto(UiSettingsDto? dto)
	{
		var defaults = new UiSettings();
		if (dto is null)
		{
			return defaults;
		}

		// One-time switch: a row older than version 2 holds the former default of ExplorerFields.
		// It is read as None; the next save writes version 2 with the fields the user kept.
		var isCurrent = ParseInt(dto.SchemaVersion, 0) >= CurrentSchemaVersion;

		return new UiSettings
		{
			HudAutoHideSeconds = ParseInt(dto.HudAutoHideSeconds, defaults.HudAutoHideSeconds),
			SkipSeconds = ParseInt(dto.SkipSeconds, defaults.SkipSeconds),
			ShowClock = ParseBool(dto.ShowClock, defaults.ShowClock),
			ShowBattery = ParseBool(dto.ShowBattery, defaults.ShowBattery),
			ExplorerFields = isCurrent
				? ParseEnum(dto.ExplorerFields, defaults.ExplorerFields)
				: ExplorerInfoFields.None,
			ViewMode = ParseEnum(dto.ViewMode, defaults.ViewMode),
			SortField = ParseEnum(dto.SortField, defaults.SortField),
			SortDirection = ParseEnum(dto.SortDirection, defaults.SortDirection),
			NewTagDays = ParseInt(dto.NewTagDays, defaults.NewTagDays),
			Language = ParseEnum(dto.Language, defaults.Language),
			Layout = ParseEnum(dto.Layout, defaults.Layout),
			ItemSize = ParseEnum(dto.ItemSize, defaults.ItemSize),
			ExplorerFontScale = ParseNumber(dto.ExplorerFontScale, defaults.ExplorerFontScale),
			IncludeAudioOnlyFolders = ParseBool(dto.IncludeAudioOnlyFolders, defaults.IncludeAudioOnlyFolders),
			LongPressSpeed = (float)ParseNumber(dto.LongPressSpeed, defaults.LongPressSpeed),
			Overlay = new PlaybackOverlaySettings
			{
				ProgressLine = ParseBool(dto.OverlayProgressLine, defaults.Overlay.ProgressLine),
				ProgressLineEdge = ParseEnum(dto.OverlayProgressLineEdge, defaults.Overlay.ProgressLineEdge),
				ProgressLineThickness = ParseInt(dto.OverlayProgressLineThickness, defaults.Overlay.ProgressLineThickness),
				Clock = ParseBool(dto.OverlayClock, defaults.Overlay.Clock),
				Battery = ParseBool(dto.OverlayBattery, defaults.Overlay.Battery),
				Time = ParseEnum(dto.OverlayTime, defaults.Overlay.Time),
				Corner = ParseEnum(dto.OverlayCorner, defaults.Overlay.Corner),
				ClockCorner = ParseEnum(dto.OverlayClockCorner, defaults.Overlay.ClockCorner),
				BatteryCorner = ParseEnum(dto.OverlayBatteryCorner, defaults.Overlay.BatteryCorner),
				MarginDp = ParseInt(dto.OverlayMarginDp, defaults.Overlay.MarginDp),
				FontSize = ParseInt(dto.OverlayFontSize, defaults.Overlay.FontSize),
				ColorArgb = ParseColorHex(dto.OverlayColorArgb, defaults.Overlay.ColorArgb),
			},
		};
	}

	private static int ParseInt(JsonElement? element, int fallback) =>
		element is { ValueKind: JsonValueKind.Number } value && value.TryGetInt32(out var number)
			? number
			: fallback;

	// A missing, wrong-length or non-hex value takes the fallback.
	private static uint ParseColorHex(JsonElement? element, uint fallback) =>
		element is { ValueKind: JsonValueKind.String } value
		&& value.GetString() is { Length: 8 } text
		&& uint.TryParse(text, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out var number)
			? number
			: fallback;

	private static double ParseNumber(JsonElement? element, double fallback) =>
		element is { ValueKind: JsonValueKind.Number } value && value.TryGetDouble(out var number)
			? number
			: fallback;

	private static bool ParseBool(JsonElement? element, bool fallback) =>
		element is { ValueKind: JsonValueKind.True or JsonValueKind.False } value
			? value.GetBoolean()
			: fallback;

	// Each enum field is read on its own: a name (any case), a number, or anything else. A name or number
	// that is not defined is rejected by Normalize; anything else takes the default.
	private static T ParseEnum<T>(JsonElement? element, T fallback) where T : struct, Enum
	{
		if (element is not { } value)
		{
			return fallback;
		}

		switch (value.ValueKind)
		{
			case JsonValueKind.String:
				return value.GetString() is { } text && Enum.TryParse(text, ignoreCase: true, out T parsed)
					? parsed
					: fallback;
			case JsonValueKind.Number:
				return value.TryGetInt32(out var number) ? (T)Enum.ToObject(typeof(T), number) : fallback;
			default:
				return fallback;
		}
	}

	private void RaiseChanged(UiSettings value)
	{
		var handler = Changed;
		if (handler is null)
		{
			return;
		}

		foreach (var subscriber in handler.GetInvocationList())
		{
			try
			{
				((EventHandler<UiSettings>)subscriber).Invoke(this, value);
			}
			catch (Exception)
			{
				// A failing subscriber must not break the save.
			}
		}
	}

	private static UiSettings Normalize(UiSettings value)
	{
		var defaults = new UiSettings();

		var normalized = value with
		{
			HudAutoHideSeconds = Math.Clamp(value.HudAutoHideSeconds, 1, 10),
			SkipSeconds = NearestSkip(value.SkipSeconds),
			NewTagDays = Math.Clamp(value.NewTagDays, 1, 30),
			ExplorerFields = value.ExplorerFields & AllExplorerFields,
			ViewMode = Enum.IsDefined(value.ViewMode) ? value.ViewMode : defaults.ViewMode,
			SortField = Enum.IsDefined(value.SortField) ? value.SortField : defaults.SortField,
			SortDirection = Enum.IsDefined(value.SortDirection) ? value.SortDirection : defaults.SortDirection,
			Language = Enum.IsDefined(value.Language) ? value.Language : defaults.Language,
			Layout = Enum.IsDefined(value.Layout) ? value.Layout : defaults.Layout,
			ItemSize = Enum.IsDefined(value.ItemSize) ? value.ItemSize : defaults.ItemSize,
			ExplorerFontScale = NormalizeFontScale(value.ExplorerFontScale),
			LongPressSpeed = NormalizeLongPressSpeed(value.LongPressSpeed),
			Overlay = NormalizeOverlay(value.Overlay),
		};

		return normalized == value ? value : normalized;
	}

	// Thickness, margin and size are clamped to their range; unknown enum values take the default.
	private static PlaybackOverlaySettings NormalizeOverlay(PlaybackOverlaySettings value)
	{
		var defaults = new PlaybackOverlaySettings();
		return value with
		{
			ProgressLineEdge = Enum.IsDefined(value.ProgressLineEdge) ? value.ProgressLineEdge : defaults.ProgressLineEdge,
			ProgressLineThickness = Math.Clamp(
				value.ProgressLineThickness,
				PlaybackOverlaySettings.MinProgressLineThickness,
				PlaybackOverlaySettings.MaxProgressLineThickness),
			Time = Enum.IsDefined(value.Time) ? value.Time : defaults.Time,
			Corner = Enum.IsDefined(value.Corner) ? value.Corner : defaults.Corner,
			ClockCorner = Enum.IsDefined(value.ClockCorner) ? value.ClockCorner : defaults.ClockCorner,
			BatteryCorner = Enum.IsDefined(value.BatteryCorner) ? value.BatteryCorner : defaults.BatteryCorner,
			MarginDp = Math.Clamp(value.MarginDp, PlaybackOverlaySettings.MinMarginDp, PlaybackOverlaySettings.MaxMarginDp),
			FontSize = Math.Clamp(value.FontSize, PlaybackOverlaySettings.MinFontSize, PlaybackOverlaySettings.MaxFontSize),
		};
	}

	// 0.8 .. 1.6 in steps of 0.1; NaN takes the record default.
	private static double NormalizeFontScale(double value) =>
		double.IsNaN(value)
			? new UiSettings().ExplorerFontScale
			: Math.Round(Math.Clamp(value, MinFontScale, MaxFontScale), 1, MidpointRounding.AwayFromZero);

	// 0.25 .. 4.0 in steps of 0.25; NaN takes the record default.
	private static float NormalizeLongPressSpeed(float value) =>
		float.IsNaN(value)
			? new UiSettings().LongPressSpeed
			: (float)(Math.Clamp(Math.Round(Math.Clamp(value, MinSpeed, MaxSpeed) * 4.0, MidpointRounding.AwayFromZero) / 4.0, MinSpeed, MaxSpeed));

	private static int NearestSkip(int seconds)
	{
		var best = SkipChoices[0];
		var bestDistance = Math.Abs((long)seconds - best);
		for (var i = 1; i < SkipChoices.Length; i++)
		{
			var distance = Math.Abs((long)seconds - SkipChoices[i]);
			if (distance < bestDistance)
			{
				best = SkipChoices[i];
				bestDistance = distance;
			}
		}

		return best;
	}
}
