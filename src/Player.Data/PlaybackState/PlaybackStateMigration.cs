using Player.Core.Contracts;
using SQLite;

namespace Player.Data.PlaybackStateStorage;

public sealed class PlaybackStateMigration : IDbMigration
{
    public int Order => 200;

    public async Task ApplyAsync(IDbConnectionProvider db, CancellationToken ct)
    {
        var conn = (SQLiteAsyncConnection)await db.GetConnectionAsync(ct).ConfigureAwait(false);
        await conn.ExecuteAsync(
            "CREATE TABLE IF NOT EXISTS playback_state (" +
            "file_key TEXT NOT NULL PRIMARY KEY, " +
            "media_store_id INTEGER NOT NULL, " +
            "position_ms INTEGER NOT NULL, " +
            "duration_ms INTEGER NOT NULL, " +
            "finished INTEGER NOT NULL, " +
            "last_played_utc INTEGER NOT NULL, " +
            "overrides_json TEXT NULL)").ConfigureAwait(false);
        await conn.ExecuteAsync(
            "CREATE INDEX IF NOT EXISTS ix_playback_state_media_store_id ON playback_state (media_store_id)")
            .ConfigureAwait(false);
    }
}
