using System.Runtime.ExceptionServices;
using Player.Core.Contracts;
using SQLite;

namespace Player.Data.Database;

// Opens the single SQLite connection lazily, enables WAL, creates the settings table and runs
// every migration once per process. Only the first caller runs the initialization; the others
// wait on the gate and then receive the same connection.
public sealed class DbConnectionProvider : IDbConnectionProvider
{
	private readonly string _databasePath;
	private readonly IEnumerable<IDbMigration> _migrations;
	private readonly SemaphoreSlim _gate = new(1, 1);
	private SQLiteAsyncConnection? _connection;
	private volatile bool _initialized;
	private ExceptionDispatchInfo? _failure;

	public DbConnectionProvider(string databasePath, IEnumerable<IDbMigration> migrations)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);
		ArgumentNullException.ThrowIfNull(migrations);
		_databasePath = databasePath;
		_migrations = migrations;
	}

	public async Task<object> GetConnectionAsync(CancellationToken ct)
	{
		if (_initialized)
		{
			return _connection!;
		}

		// The caller's token applies while waiting for the gate only. Once started, the
		// initialization runs to completion so one cancelled caller cannot leave the file half set up.
		await _gate.WaitAsync(ct);
		try
		{
			if (_failure is not null)
			{
				_failure.Throw();
			}

			if (!_initialized)
			{
				try
				{
					await InitializeAsync();
				}
				catch (Exception ex)
				{
					// A failed initialization is not retried: migrations must not run twice in one process.
					_failure = ExceptionDispatchInfo.Capture(ex);
					throw;
				}

				_initialized = true;
			}

			return _connection!;
		}
		finally
		{
			_gate.Release();
		}
	}

	private async Task InitializeAsync()
	{
		_connection = new SQLiteAsyncConnection(_databasePath);
		await _connection.EnableWriteAheadLoggingAsync();
		await _connection.CreateTableAsync<SettingsRow>();

		// Migrations call GetConnectionAsync on the provider they receive. Passing this provider
		// would wait on the gate held by this call and deadlock, so they get the open connection.
		var migrationProvider = new OpenConnectionProvider(_connection);
		foreach (var migration in _migrations.OrderBy(m => m.Order))
		{
			await migration.ApplyAsync(migrationProvider, CancellationToken.None);
		}
	}

	private sealed class OpenConnectionProvider(SQLiteAsyncConnection connection) : IDbConnectionProvider
	{
		public Task<object> GetConnectionAsync(CancellationToken ct) => Task.FromResult<object>(connection);
	}
}
