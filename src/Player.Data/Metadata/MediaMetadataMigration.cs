using Player.Core.Contracts;
using SQLite;

namespace Player.Data.Metadata;

public sealed class MediaMetadataMigration : IDbMigration
{
    public int Order => 100;

    public async Task ApplyAsync(IDbConnectionProvider db, CancellationToken ct)
    {
        var conn = (SQLiteAsyncConnection)await db.GetConnectionAsync(ct).ConfigureAwait(false);
        await conn.ExecuteAsync(
            "CREATE TABLE IF NOT EXISTS media_metadata (" +
            "file_key TEXT NOT NULL PRIMARY KEY, " +
            "media_store_id INTEGER NOT NULL, " +
            "duration_ms INTEGER NOT NULL, " +
            "width INTEGER NOT NULL, " +
            "height INTEGER NOT NULL, " +
            "thumbnail_path TEXT NULL, " +
            "failed INTEGER NOT NULL, " +
            "created_utc INTEGER NOT NULL)").ConfigureAwait(false);
        await conn.ExecuteAsync(
            "CREATE INDEX IF NOT EXISTS ix_media_metadata_media_store_id ON media_metadata (media_store_id)")
            .ConfigureAwait(false);
    }
}
