using System.Text.Json;
using Player.Core.Contracts;
using Player.Core.Models;
using Player.Data.Database;

namespace Player.Data.UiSettingsStorage;

public sealed class UiSettingsStore : IUiSettingsStore
{
	public const string SettingsKey = "ui_settings";

	private static readonly int[] SkipChoices = [5, 10, 15, 30, 60];

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
		HudAutoHideSeconds = value.HudAutoHideSeconds,
		SkipSeconds = value.SkipSeconds,
		ShowClock = value.ShowClock,
		ShowBattery = value.ShowBattery,
		ExplorerFields = Name(value.ExplorerFields),
		ViewMode = Name(value.ViewMode),
		SortField = Name(value.SortField),
		SortDirection = Name(value.SortDirection),
		NewTagDays = value.NewTagDays,
		Language = Name(value.Language),
	};

	private static JsonElement Name<T>(T value) where T : struct, Enum =>
		JsonSerializer.SerializeToElement(value.ToString(), UiSettingsJsonContext.Default.String);

	// Each field falls back on its own: a missing or unreadable field takes the record default
	// and never discards the others.
	private static UiSettings FromDto(UiSettingsDto? dto)
	{
		var defaults = new UiSettings();
		if (dto is null)
		{
			return defaults;
		}

		return new UiSettings
		{
			HudAutoHideSeconds = dto.HudAutoHideSeconds ?? defaults.HudAutoHideSeconds,
			SkipSeconds = dto.SkipSeconds ?? defaults.SkipSeconds,
			ShowClock = dto.ShowClock ?? defaults.ShowClock,
			ShowBattery = dto.ShowBattery ?? defaults.ShowBattery,
			ExplorerFields = ParseEnum(dto.ExplorerFields, defaults.ExplorerFields),
			ViewMode = ParseEnum(dto.ViewMode, defaults.ViewMode),
			SortField = ParseEnum(dto.SortField, defaults.SortField),
			SortDirection = ParseEnum(dto.SortDirection, defaults.SortDirection),
			NewTagDays = dto.NewTagDays ?? defaults.NewTagDays,
			Language = ParseEnum(dto.Language, defaults.Language),
		};
	}

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
		};

		return normalized == value ? value : normalized;
	}

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
