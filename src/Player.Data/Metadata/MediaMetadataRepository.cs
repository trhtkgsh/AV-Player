using Player.Core.Contracts;
using SQLite;

namespace Player.Data.Metadata;

public sealed class MediaMetadataRepository
{
    // Stays under SQLite's bound-parameter limit (999 on older builds).
    private const int ChunkSize = 500;

    private readonly IDbConnectionProvider _db;

    public MediaMetadataRepository(IDbConnectionProvider db) => _db = db;

    public async Task<IReadOnlyList<MediaMetadataRow>> GetManyAsync(IReadOnlyCollection<string> fileKeys, CancellationToken ct)
    {
        var result = new List<MediaMetadataRow>();
        if (fileKeys.Count == 0)
            return result;

        var conn = await GetConnectionAsync(ct).ConfigureAwait(false);
        foreach (var chunk in fileKeys.Distinct().Chunk(ChunkSize))
        {
            ct.ThrowIfCancellationRequested();
            var rows = await conn.QueryAsync<MediaMetadataRow>(
                $"SELECT * FROM media_metadata WHERE file_key IN ({Placeholders(chunk.Length)})",
                chunk.Select(k => (object)k).ToArray()).ConfigureAwait(false);
            result.AddRange(rows);
        }
        return result;
    }

    public async Task UpsertAsync(MediaMetadataRow row, CancellationToken ct)
    {
        var conn = await GetConnectionAsync(ct).ConfigureAwait(false);
        await conn.InsertOrReplaceAsync(row).ConfigureAwait(false);
    }

    /// <summary>Deletes every row whose key is not in <paramref name="keepKeys"/>. Returns the deleted rows.</summary>
    public async Task<IReadOnlyList<MediaMetadataRow>> DeleteNotInAsync(IReadOnlyCollection<string> keepKeys, CancellationToken ct)
    {
        var conn = await GetConnectionAsync(ct).ConfigureAwait(false);
        var keep = new HashSet<string>(keepKeys);
        var all = await conn.QueryAsync<MediaMetadataRow>("SELECT * FROM media_metadata").ConfigureAwait(false);
        var stale = all.Where(r => !keep.Contains(r.FileKey)).ToList();
        if (stale.Count == 0)
            return stale;

        await conn.RunInTransactionAsync(tx =>
        {
            foreach (var chunk in stale.Select(r => r.FileKey).Chunk(ChunkSize))
                tx.Execute(
                    $"DELETE FROM media_metadata WHERE file_key IN ({Placeholders(chunk.Length)})",
                    chunk.Select(k => (object)k).ToArray());
        }).ConfigureAwait(false);
        return stale;
    }

    public async Task<IReadOnlyList<MediaMetadataRow>> ListFailedAsync(CancellationToken ct)
    {
        var conn = await GetConnectionAsync(ct).ConfigureAwait(false);
        return await conn.QueryAsync<MediaMetadataRow>("SELECT * FROM media_metadata WHERE failed = 1")
            .ConfigureAwait(false);
    }

    private async Task<SQLiteAsyncConnection> GetConnectionAsync(CancellationToken ct) =>
        (SQLiteAsyncConnection)await _db.GetConnectionAsync(ct).ConfigureAwait(false);

    private static string Placeholders(int count) => string.Join(",", Enumerable.Repeat("?", count));
}
