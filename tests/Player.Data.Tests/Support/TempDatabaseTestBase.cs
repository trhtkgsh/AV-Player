using Player.Core.Contracts;
using Player.Data.Database;
using SQLite;

namespace Player.Data.Tests.Support;

/// <summary>
/// Gives each test class instance its own temporary directory and a database path inside it.
/// Providers created through <see cref="CreateProvider"/> are closed before the directory is deleted.
/// </summary>
public abstract class TempDatabaseTestBase : IAsyncLifetime
{
    private readonly List<DbConnectionProvider> _providers = new();

    protected TempDatabaseTestBase()
    {
        DirectoryPath = Path.Combine(Path.GetTempPath(), "ariel-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(DirectoryPath);
        DatabasePath = Path.Combine(DirectoryPath, "ariel.db");
    }

    protected string DirectoryPath { get; }

    protected string DatabasePath { get; }

    protected DbConnectionProvider CreateProvider(params IDbMigration[] migrations)
    {
        var provider = new DbConnectionProvider(DatabasePath, migrations);
        _providers.Add(provider);
        return provider;
    }

    protected static async Task<SQLiteAsyncConnection> OpenAsync(IDbConnectionProvider provider) =>
        (SQLiteAsyncConnection)await provider.GetConnectionAsync(CancellationToken.None);

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        foreach (var provider in _providers)
        {
            try
            {
                var connection = await OpenAsync(provider);
                await connection.CloseAsync();
            }
            catch (Exception)
            {
                // A provider whose initialization failed has nothing usable to close.
            }
        }

        try
        {
            Directory.Delete(DirectoryPath, recursive: true);
        }
        catch (Exception)
        {
            // Cleanup problems must not fail a test.
        }
    }
}
