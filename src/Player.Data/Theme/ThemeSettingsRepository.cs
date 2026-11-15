using System.Text.Json;
using Player.Core.Contracts;
using Player.Core.Logic.Theming;
using Player.Core.Models;
using Player.Data.Database;

namespace Player.Data.ThemeStorage;

/// <summary>Reads and writes the <c>theme</c> row of the <c>settings</c> table. Loading never throws on bad data.</summary>
public sealed class ThemeSettingsRepository
{
	public const string SettingsKey = "theme";

	private readonly SettingsRepository _settings;

	public ThemeSettingsRepository(SettingsRepository settings)
	{
		ArgumentNullException.ThrowIfNull(settings);
		_settings = settings;
	}

	public async Task<ThemeSettings> LoadAsync(CancellationToken ct)
	{
		var json = await _settings.GetJsonAsync(SettingsKey, ct).ConfigureAwait(false);
		if (string.IsNullOrWhiteSpace(json))
		{
			return new ThemeSettings();
		}

		try
		{
			var dto = JsonSerializer.Deserialize(json, ThemeJsonContext.Default.ThemeSettingsReadDto);
			return FromDto(dto);
		}
		catch (JsonException)
		{
			return new ThemeSettings();
		}
	}

	public async Task SaveAsync(ThemeSettings settings, CancellationToken ct)
	{
		ArgumentNullException.ThrowIfNull(settings);
		ct.ThrowIfCancellationRequested();

		var normalized = Normalize(settings);
		var dto = new ThemeSettingsWriteDto
		{
			Theme = normalized.Theme.ToString(),
			AccentColorArgb = normalized.AccentColorArgb,
			HudOpacity = normalized.HudOpacity,
		};
		var json = JsonSerializer.Serialize(dto, ThemeJsonContext.Default.ThemeSettingsWriteDto);
		await _settings.SetJsonAsync(SettingsKey, json, ct).ConfigureAwait(false);
	}

	// Each field falls back on its own and never discards the others. The DTO is merged onto the
	// record defaults; the record is never deserialized directly.
	private static ThemeSettings FromDto(ThemeSettingsReadDto? dto)
	{
		var defaults = new ThemeSettings();
		if (dto is null)
		{
			return defaults;
		}

		return Normalize(new ThemeSettings
		{
			Theme = ReadTheme(dto.Theme, defaults.Theme),
			AccentColorArgb = ReadAccent(dto.AccentColorArgb, defaults.AccentColorArgb),
			HudOpacity = ReadOpacity(dto.HudOpacity, defaults.HudOpacity),
		});
	}

	private static ThemeSettings Normalize(ThemeSettings value) => ThemeSettingsNormalizer.Normalize(value);

	private static ThemeId ReadTheme(JsonElement? element, ThemeId fallback)
	{
		if (element is not { } value)
		{
			return fallback;
		}

		switch (value.ValueKind)
		{
			case JsonValueKind.String:
				var text = value.GetString();
				return text is not null && Enum.TryParse(text, ignoreCase: true, out ThemeId byName) && Enum.IsDefined(byName)
					? byName
					: fallback;
			case JsonValueKind.Number:
				return value.TryGetInt32(out var number) && Enum.IsDefined(typeof(ThemeId), number)
					? (ThemeId)number
					: fallback;
			default:
				return fallback;
		}
	}

	private static uint ReadAccent(JsonElement? element, uint fallback)
	{
		if (element is { ValueKind: JsonValueKind.Number } value)
		{
			if (value.TryGetUInt32(out var unsigned))
			{
				return unsigned;
			}

			if (value.TryGetInt32(out var signed))
			{
				return unchecked((uint)signed);
			}
		}

		return fallback;
	}

	private static double ReadOpacity(JsonElement? element, double fallback) =>
		element is { ValueKind: JsonValueKind.Number } value && value.TryGetDouble(out var number) && double.IsFinite(number)
			? number
			: fallback;
}
