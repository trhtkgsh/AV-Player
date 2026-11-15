using System.Text.Json;
using Player.Core.Contracts;
using Player.Core.Models;
using Player.Data.Database;

namespace Player.Data.GlobalDefaultsStore;

public sealed class GlobalDefaultsStore : IGlobalDefaultsStore
{
	public const string SettingsKey = "global_defaults";

	private const float MinSpeed = 0.25f;
	private const float MaxSpeed = 4.0f;
	private const long MinDelayMs = -5000;
	private const long MaxDelayMs = 5000;

	private readonly SettingsRepository _settings;
	private readonly SemaphoreSlim _gate = new(1, 1);
	private GlobalDefaults? _cache;

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
			var json = JsonSerializer.Serialize(normalized, GlobalDefaultsJsonContext.Default.GlobalDefaults);
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

		GlobalDefaults? loaded = null;
		if (json is not null)
		{
			try
			{
				loaded = JsonSerializer.Deserialize(json, GlobalDefaultsJsonContext.Default.GlobalDefaults);
			}
			catch (JsonException)
			{
				loaded = null;
			}
		}

		return Normalize(loaded ?? new GlobalDefaults());
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
		SubtitleStyle style = rawStyle ?? SubtitleStyle.Default;

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

	private static string? NormalizeLanguage(string? language)
	{
		var trimmed = language?.Trim();
		return string.IsNullOrEmpty(trimmed) ? null : trimmed;
	}
}
