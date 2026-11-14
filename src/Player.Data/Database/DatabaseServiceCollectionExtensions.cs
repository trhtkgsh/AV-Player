using Microsoft.Extensions.DependencyInjection;
using Player.Core.Contracts;

namespace Player.Data.Database;

public static class DatabaseServiceCollectionExtensions
{
	public static IServiceCollection AddPlayerDatabase(this IServiceCollection services, string databasePath)
	{
		ArgumentNullException.ThrowIfNull(services);
		ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);

		services.AddSingleton<IDbConnectionProvider>(sp =>
			new DbConnectionProvider(databasePath, sp.GetServices<IDbMigration>()));
		services.AddSingleton<SettingsRepository>(sp =>
			new SettingsRepository(sp.GetRequiredService<IDbConnectionProvider>(), sp.GetService<TimeProvider>()));

		return services;
	}
}
