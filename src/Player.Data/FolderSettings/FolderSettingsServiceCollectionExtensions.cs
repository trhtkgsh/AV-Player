using Microsoft.Extensions.DependencyInjection;
using Player.Core.Contracts;

namespace Player.Data.FolderSettingsStorage;

public static class FolderSettingsServiceCollectionExtensions
{
	public static IServiceCollection AddFolderSettings(this IServiceCollection services)
	{
		ArgumentNullException.ThrowIfNull(services);
		services.AddSingleton<IFolderSettingsStore, FolderSettingsStore>();
		return services;
	}
}
