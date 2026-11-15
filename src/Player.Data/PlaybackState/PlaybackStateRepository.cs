using System.Globalization;
using Player.Core.Contracts;
using Player.Core.Models;
using SQLite;

namespace Player.Data.PlaybackStateStorage;

/// <summary>Per-file playback state in table playback_state. Never touches the settings table.</summary>
public sealed class PlaybackStateRepository : IPlaybackStateStore
{
    // Stays under SQLite's bound-parameter limit (999 on older builds).
    private const int ChunkSize = 500;

    private readonly IDbConnectionProvider _db;

    public PlaybackStateRepository(IDbConnectionProvider db)
    {
        ArgumentNullException.ThrowIfNull(db);
        _db = db;
    }

    public async Task<PlaybackState?> GetAsync(MediaFileId id, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var conn = await GetConnectionAsync(ct).ConfigureAwait(false);
        var rows = await conn.QueryAsync<PlaybackStateRow>(
            "SELECT * FROM playback_state WHERE file_key = ?", id.ToKey()).ConfigureAwait(false);
        foreach (var row in rows)
        {
            var state = ToState(row);
            if (state is not null)
                return state;
        }
        return null;
    }

    public async Task<IReadOnlyDictionary<MediaFileId, PlaybackState>> GetManyAsync(
        IReadOnlyCollection<MediaFileId> ids, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(ids);
        ct.ThrowIfCancellationRequested();

        var result = new Dictionary<MediaFileId, PlaybackState>();
        if (ids.Count == 0)
            return result;

        var conn = await GetConnectionAsync(ct).ConfigureAwait(false);
        var keys = ids.Select(i => i.ToKey()).Distinct().ToArray();
        foreach (var chunk in keys.Chunk(ChunkSize))
        {
            ct.ThrowIfCancellationRequested();
            var rows = await conn.QueryAsync<PlaybackStateRow>(
                $"SELECT * FROM playback_state WHERE file_key IN ({Placeholders(chunk.Length)})",
                chunk.Select(k => (object)k).ToArray()).ConfigureAwait(false);
            foreach (var row in rows)
            {
                var state = ToState(row);
                if (state is not null)
                    result[state.Id] = state;
            }
        }
        return result;
    }

    public async Task SaveAsync(PlaybackState state, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(state);
        ct.ThrowIfCancellationRequested();
        var conn = await GetConnectionAsync(ct).ConfigureAwait(false);
        var row = new PlaybackStateRow
        {
            FileKey = state.Id.ToKey(),
            MediaStoreId = state.Id.MediaStoreId,
            PositionMs = state.PositionMs,
            DurationMs = state.DurationMs,
            Finished = state.Finished,
            LastPlayedUtc = state.LastPlayedUtc,
            OverridesJson = PlaybackStateOverrides.From(state).ToJson(),
        };
        await conn.InsertOrReplaceAsync(row).ConfigureAwait(false);
    }

    public async Task DeleteAsync(MediaFileId id, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var conn = await GetConnectionAsync(ct).ConfigureAwait(false);
        await conn.ExecuteAsync("DELETE FROM playback_state WHERE file_key = ?", id.ToKey()).ConfigureAwait(false);
    }

    /// <summary>Returns null when the key cannot be parsed.</summary>
    private static PlaybackState? ToState(PlaybackStateRow row)
    {
        if (!TryParseKey(row.FileKey, out var id))
            return null;

        var overrides = PlaybackStateOverrides.Parse(row.OverridesJson);
        return new PlaybackState(id)
        {
            PositionMs = row.PositionMs,
            DurationMs = row.DurationMs,
            Finished = row.Finished,
            LastPlayedUtc = row.LastPlayedUtc,
            Speed = overrides.Speed,
            Decoder = overrides.GetDecoder(),
            Aspect = overrides.GetAspect(),
            AudioTrackId = overrides.AudioTrackId,
            AudioDelayMs = overrides.AudioDelayMs,
            SubtitleTrackId = overrides.SubtitleTrackId,
            SubtitleDelayMs = overrides.SubtitleDelayMs,
            ExternalSubtitleUri = overrides.ExternalSubtitleUri,
        };
    }

    private static bool TryParseKey(string? key, out MediaFileId id)
    {
        id = default;
        if (string.IsNullOrEmpty(key))
            return false;

        var parts = key.Split(':');
        if (parts.Length != 3 ||
            !long.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var storeId) ||
            !long.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var size) ||
            !long.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out var modified))
            return false;

        id = new MediaFileId(storeId, size, modified);
        return true;
    }

    private async Task<SQLiteAsyncConnection> GetConnectionAsync(CancellationToken ct) =>
        (SQLiteAsyncConnection)await _db.GetConnectionAsync(ct).ConfigureAwait(false);

    private static string Placeholders(int count) => string.Join(",", Enumerable.Repeat("?", count));
}
