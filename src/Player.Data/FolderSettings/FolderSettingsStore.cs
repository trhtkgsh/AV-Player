using System.Globalization;
using System.Text.Json;
using Player.Core.Contracts;
using Player.Core.Models;
using Player.Data.Database;

namespace Player.Data.FolderSettingsStorage;

/// <summary>
/// Per-folder speed and favorite mark, kept as one JSON map in the <c>folder_settings</c> settings row.
/// The map is loaded once and kept in memory; each save writes the whole map and then publishes it.
/// </summary>
public sealed class FolderSettingsStore : IFolderSettingsStore
{
	public const string SettingsKey = "folder_settings";

	private const float MinSpeed = 0.25f;
	private const float MaxSpeed = 4.0f;

	private readonly SettingsRepository _settings;
	private readonly SemaphoreSlim _gate = new(1, 1);

	// Published maps are never mutated: a save builds a new map and swaps the reference, so readers
	// never see a half-applied change and a failed write leaves the previous map in place.
	private volatile Dictionary<long, FolderSettings>? _map;

	public FolderSettingsStore(SettingsRepository settings)
	{
		ArgumentNullException.ThrowIfNull(settings);
		_settings = settings;
	}

	public event EventHandler? Changed;

	public async Task<FolderSettings?> GetAsync(long bucketId, CancellationToken ct)
	{
		var map = await GetMapAsync(ct).ConfigureAwait(false);
		return map.TryGetValue(bucketId, out var settings) ? settings : null;
	}

	public async Task<IReadOnlyDictionary<long, FolderSettings>> GetAllAsync(CancellationToken ct)
	{
		var map = await GetMapAsync(ct).ConfigureAwait(false);
		return new Dictionary<long, FolderSettings>(map);
	}

	public async Task SaveAsync(FolderSettings value, CancellationToken ct)
	{
		ArgumentNullException.ThrowIfNull(value);
		ct.ThrowIfCancellationRequested();

		var entry = Normalize(value.BucketId, value.Speed, value.Favorite);
		var changed = false;

		await _gate.WaitAsync(ct).ConfigureAwait(false);
		try
		{
			var current = _map;
			if (current is null)
			{
				current = await LoadAsync(ct).ConfigureAwait(false);
				_map = current;
			}

			if (WithEntry(current, entry) is { } updated)
			{
				var json = JsonSerializer.Serialize(ToDto(updated), FolderSettingsJsonContext.Default.DictionaryStringFolderSettingsDto);
				await _settings.SetJsonAsync(SettingsKey, json, ct).ConfigureAwait(false);
				_map = updated;
				changed = true;
			}
		}
		finally
		{
			_gate.Release();
		}

		if (changed)
		{
			RaiseChanged();
		}
	}

	private async Task<Dictionary<long, FolderSettings>> GetMapAsync(CancellationToken ct)
	{
		if (_map is { } cached)
		{
			return cached;
		}

		await _gate.WaitAsync(ct).ConfigureAwait(false);
		try
		{
			var loaded = _map ?? await LoadAsync(ct).ConfigureAwait(false);
			_map = loaded;
			return loaded;
		}
		finally
		{
			_gate.Release();
		}
	}

	// A missing, empty or broken row loads as an empty map. Each entry is read on its own, so one bad
	// entry is skipped and the others are kept.
	private async Task<Dictionary<long, FolderSettings>> LoadAsync(CancellationToken ct)
	{
		var map = new Dictionary<long, FolderSettings>();
		var json = await _settings.GetJsonAsync(SettingsKey, ct).ConfigureAwait(false);
		if (string.IsNullOrWhiteSpace(json))
		{
			return map;
		}

		Dictionary<string, JsonElement>? raw;
		try
		{
			raw = JsonSerializer.Deserialize(json, FolderSettingsJsonContext.Default.DictionaryStringJsonElement);
		}
		catch (JsonException)
		{
			return map;
		}

		if (raw is null)
		{
			return map;
		}

		foreach (var (key, element) in raw)
		{
			if (!long.TryParse(key, NumberStyles.Integer, CultureInfo.InvariantCulture, out var bucketId) ||
				element.ValueKind != JsonValueKind.Object)
			{
				continue;
			}

			var entry = ReadEntry(bucketId, element);
			if (HasMark(entry))
			{
				map[bucketId] = entry;
			}
		}

		return map;
	}

	// Each member falls back on its own: a speed that is not a number and a favorite that is not a boolean
	// are read as absent, and the other member is kept.
	private static FolderSettings ReadEntry(long bucketId, JsonElement element)
	{
		float? speed = null;
		if (element.TryGetProperty("Speed", out var speedElement) &&
			speedElement.ValueKind == JsonValueKind.Number &&
			speedElement.TryGetSingle(out var parsedSpeed))
		{
			speed = parsedSpeed;
		}

		var favorite = element.TryGetProperty("Favorite", out var favoriteElement) &&
			favoriteElement.ValueKind == JsonValueKind.True;

		return Normalize(bucketId, speed, favorite);
	}

	// Returns the map with the entry applied, or null when the map would not change.
	private static Dictionary<long, FolderSettings>? WithEntry(Dictionary<long, FolderSettings> current, FolderSettings entry)
	{
		if (HasMark(entry))
		{
			if (current.TryGetValue(entry.BucketId, out var existing) && existing == entry)
			{
				return null;
			}
		}
		else if (!current.ContainsKey(entry.BucketId))
		{
			return null;
		}

		var updated = new Dictionary<long, FolderSettings>(current);
		if (HasMark(entry))
		{
			updated[entry.BucketId] = entry;
		}
		else
		{
			updated.Remove(entry.BucketId);
		}

		return updated;
	}

	private static FolderSettings Normalize(long bucketId, float? speed, bool favorite) => new(bucketId)
	{
		Speed = speed is { } value && float.IsFinite(value) ? Math.Clamp(value, MinSpeed, MaxSpeed) : null,
		Favorite = favorite,
	};

	private static bool HasMark(FolderSettings settings) => settings.Speed is not null || settings.Favorite;

	private static Dictionary<string, FolderSettingsDto> ToDto(Dictionary<long, FolderSettings> map)
	{
		var dto = new Dictionary<string, FolderSettingsDto>(map.Count);
		foreach (var (bucketId, settings) in map)
		{
			dto[bucketId.ToString(CultureInfo.InvariantCulture)] = new FolderSettingsDto
			{
				Speed = settings.Speed,
				Favorite = settings.Favorite ? true : null,
			};
		}

		return dto;
	}

	private void RaiseChanged()
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
				((EventHandler)subscriber).Invoke(this, EventArgs.Empty);
			}
			catch (Exception)
			{
				// A failing subscriber must not break the save.
			}
		}
	}
}
