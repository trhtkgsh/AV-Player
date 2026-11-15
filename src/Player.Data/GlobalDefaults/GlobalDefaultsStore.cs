using System.Text.Json;
using Player.Core.Contracts;
using Player.Core.Models;
using Player.Data.Database;

namespace Player.Data.GlobalDefaultsStorage;

public sealed class GlobalDefaultsStore : IGlobalDefaultsStore
{
	public const string SettingsKey = "global_defaults";

	private const float MinSpeed = 0.25f;
	private const float MaxSpeed = 4.0f;
	private const long MinDelayMs = -5000;
	private const long MaxDelayMs = 5000;

	private readonly SettingsRepository _settings;
	private readonly SemaphoreSlim _gate = new(1, 1);
	private volatile GlobalDefaults? _cache;

	public GlobalDefaultsStore(SettingsRepository settings)
	{
		ArgumentNullException.ThrowIfNull(settings);
		_settings = settings;
	}

	public event EventHandler<GlobalDefaults>? Changed;

	public async Task<GlobalDefaults> GetAsync(CancellationToken ct)
	{
		ct.ThrowIfCancellationRequested();

		var cached = _cache;
		if (cached is not null)
		{
			return cached;
		}

		await _gate.WaitAsync(ct).ConfigureAwait(false);
		try
		{
			if (_cache is null)
			{
				_cache = await LoadAsync(ct).ConfigureAwait(false);
			}

			return _cache;
		}
		finally
		{
			_gate.Release();
		}
	}

	public async Task SaveAsync(GlobalDefaults value, CancellationToken ct)
	{
		ArgumentNullException.ThrowIfNull(value);
		ct.ThrowIfCancellationRequested();

		GlobalDefaults normalized;
		await _gate.WaitAsync(ct).ConfigureAwait(false);
		try
		{
			normalized = Normalize(value);
			var json = JsonSerializer.Serialize(ToDto(normalized), GlobalDefaultsJsonContext.Default.GlobalDefaultsDto);
			await _settings.SetJsonAsync(SettingsKey, json, ct).ConfigureAwait(false);
			_cache = normalized;
		}
		finally
		{
			_gate.Release();
		}

		RaiseChanged(normalized);
	}

	private async Task<GlobalDefaults> LoadAsync(CancellationToken ct)
	{
		var json = await _settings.GetJsonAsync(SettingsKey, ct).ConfigureAwait(false);

		GlobalDefaultsDto? dto = null;
		if (json is not null)
		{
			try
			{
				dto = JsonSerializer.Deserialize(json, GlobalDefaultsJsonContext.Default.GlobalDefaultsDto);
			}
			catch (JsonException)
			{
				dto = null;
			}
		}

		return Normalize(FromDto(dto));
	}

	private void RaiseChanged(GlobalDefaults value)
	{
		var handlers = Changed;
		if (handlers is null)
		{
			return;
		}

		foreach (var handler in handlers.GetInvocationList())
		{
			try
			{
				((EventHandler<GlobalDefaults>)handler)(this, value);
			}
			catch (Exception)
			{
				// A failing subscriber must not break the save.
			}
		}
	}

	private static GlobalDefaults Normalize(GlobalDefaults value)
	{
		float speed = float.IsNaN(value.Speed) ? 1.0f : Math.Clamp(value.Speed, MinSpeed, MaxSpeed);
		long audioDelayMs = Math.Clamp(value.AudioDelayMs, MinDelayMs, MaxDelayMs);
		long subtitleDelayMs = Math.Clamp(value.SubtitleDelayMs, MinDelayMs, MaxDelayMs);

		SubtitleStyle? rawStyle = value.SubtitleStyle;
		SubtitleStyle style = NormalizeStyle(rawStyle ?? SubtitleStyle.Default);

		string? audioLanguage = NormalizeLanguage(value.PreferredAudioLanguage);
		string? subtitleLanguage = NormalizeLanguage(value.PreferredSubtitleLanguage);

		var result = value with
		{
			Speed = speed,
			AudioDelayMs = audioDelayMs,
			SubtitleDelayMs = subtitleDelayMs,
			SubtitleStyle = style,
			PreferredAudioLanguage = audioLanguage,
			PreferredSubtitleLanguage = subtitleLanguage
		};

		return result == value ? value : result;
	}

	private static GlobalDefaultsDto ToDto(GlobalDefaults value)
	{
		return new GlobalDefaultsDto
		{
			Speed = value.Speed,
			Decoder = value.Decoder.ToString(),
			Aspect = value.Aspect.ToString(),
			Repeat = value.Repeat.ToString(),
			ResumeFromLastPosition = value.ResumeFromLastPosition,
			SubtitlesEnabled = value.SubtitlesEnabled,
			PreferredAudioLanguage = value.PreferredAudioLanguage,
			PreferredSubtitleLanguage = value.PreferredSubtitleLanguage,
			AudioDelayMs = value.AudioDelayMs,
			SubtitleDelayMs = value.SubtitleDelayMs,
			SubtitleStyle = ToStyleDto(value.SubtitleStyle),
			BackgroundAudio = value.BackgroundAudio,
			PictureInPictureOnLeave = value.PictureInPictureOnLeave
		};
	}

	private static SubtitleStyleDto ToStyleDto(SubtitleStyle style)
	{
		return new SubtitleStyleDto
		{
			FontScale = style.FontScale,
			TextColorArgb = style.TextColorArgb,
			OutlineColorArgb = style.OutlineColorArgb,
			OutlineThickness = style.OutlineThickness,
			BackgroundOpacity = style.BackgroundOpacity,
			Bold = style.Bold,
			BottomMarginFraction = style.BottomMarginFraction
		};
	}

	// Missing or unreadable fields keep the record default; one bad field does not discard the others.
	private static GlobalDefaults FromDto(GlobalDefaultsDto? dto)
	{
		var defaults = new GlobalDefaults();
		if (dto is null)
		{
			return defaults;
		}

		return defaults with
		{
			Speed = dto.Speed ?? defaults.Speed,
			Decoder = ParseEnum<DecoderMode>(dto.Decoder) ?? defaults.Decoder,
			Aspect = ParseEnum<AspectMode>(dto.Aspect) ?? defaults.Aspect,
			Repeat = ParseEnum<RepeatMode>(dto.Repeat) ?? defaults.Repeat,
			ResumeFromLastPosition = dto.ResumeFromLastPosition ?? defaults.ResumeFromLastPosition,
			SubtitlesEnabled = dto.SubtitlesEnabled ?? defaults.SubtitlesEnabled,
			PreferredAudioLanguage = dto.PreferredAudioLanguage ?? defaults.PreferredAudioLanguage,
			PreferredSubtitleLanguage = dto.PreferredSubtitleLanguage ?? defaults.PreferredSubtitleLanguage,
			AudioDelayMs = dto.AudioDelayMs ?? defaults.AudioDelayMs,
			SubtitleDelayMs = dto.SubtitleDelayMs ?? defaults.SubtitleDelayMs,
			SubtitleStyle = FromStyleDto(dto.SubtitleStyle),
			BackgroundAudio = dto.BackgroundAudio ?? defaults.BackgroundAudio,
			PictureInPictureOnLeave = dto.PictureInPictureOnLeave ?? defaults.PictureInPictureOnLeave
		};
	}

	private static SubtitleStyle FromStyleDto(SubtitleStyleDto? dto)
	{
		var defaults = SubtitleStyle.Default;
		if (dto is null)
		{
			return defaults;
		}

		return defaults with
		{
			FontScale = dto.FontScale ?? defaults.FontScale,
			TextColorArgb = dto.TextColorArgb ?? defaults.TextColorArgb,
			OutlineColorArgb = dto.OutlineColorArgb ?? defaults.OutlineColorArgb,
			OutlineThickness = dto.OutlineThickness ?? defaults.OutlineThickness,
			BackgroundOpacity = dto.BackgroundOpacity ?? defaults.BackgroundOpacity,
			Bold = dto.Bold ?? defaults.Bold,
			BottomMarginFraction = dto.BottomMarginFraction ?? defaults.BottomMarginFraction
		};
	}

	private static T? ParseEnum<T>(string? text) where T : struct, Enum
	{
		if (text is not null && Enum.TryParse<T>(text, ignoreCase: true, out var parsed) && Enum.IsDefined(parsed))
		{
			return parsed;
		}

		return null;
	}

	private static SubtitleStyle NormalizeStyle(SubtitleStyle style)
	{
		double fontScale = double.IsFinite(style.FontScale) ? Math.Clamp(style.FontScale, 0.5, 3.0) : 1.0;
		int outlineThickness = Math.Clamp(style.OutlineThickness, 0, 4);
		double backgroundOpacity = double.IsFinite(style.BackgroundOpacity) ? Math.Clamp(style.BackgroundOpacity, 0.0, 1.0) : 0.0;
		double bottomMarginFraction = double.IsFinite(style.BottomMarginFraction) ? Math.Clamp(style.BottomMarginFraction, 0.0, 0.3) : 0.05;

		var result = style with
		{
			FontScale = fontScale,
			OutlineThickness = outlineThickness,
			BackgroundOpacity = backgroundOpacity,
			BottomMarginFraction = bottomMarginFraction
		};

		return result == style ? style : result;
	}

	private static string? NormalizeLanguage(string? language)
	{
		var trimmed = language?.Trim();
		return string.IsNullOrEmpty(trimmed) ? null : trimmed;
	}
}
