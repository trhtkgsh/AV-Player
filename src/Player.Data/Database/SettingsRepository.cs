using Player.Core.Contracts;
using SQLite;

namespace Player.Data.Database;

public sealed class SettingsRepository
{
	private readonly IDbConnectionProvider _db;
	private readonly TimeProvider _timeProvider;

	public SettingsRepository(IDbConnectionProvider db, TimeProvider? timeProvider = null)
	{
		ArgumentNullException.ThrowIfNull(db);
		_db = db;
		_timeProvider = timeProvider ?? TimeProvider.System;
	}

	public async Task<string?> GetJsonAsync(string key, CancellationToken ct)
	{
		ArgumentNullException.ThrowIfNull(key);
		var connection = await OpenAsync(ct);
		ct.ThrowIfCancellationRequested();
		var row = await connection.Table<SettingsRow>().Where(r => r.Key == key).FirstOrDefaultAsync();
		return row?.Json;
	}

	public async Task SetJsonAsync(string key, string json, CancellationToken ct)
	{
		ArgumentNullException.ThrowIfNull(key);
		ArgumentNullException.ThrowIfNull(json);
		var connection = await OpenAsync(ct);
		ct.ThrowIfCancellationRequested();
		var row = new SettingsRow
		{
			Key = key,
			Json = json,
			UpdatedUtc = _timeProvider.GetUtcNow().ToUnixTimeSeconds()
		};
		await connection.InsertOrReplaceAsync(row);
	}

	private async Task<SQLiteAsyncConnection> OpenAsync(CancellationToken ct) =>
		(SQLiteAsyncConnection)await _db.GetConnectionAsync(ct);
}
