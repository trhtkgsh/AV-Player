using Player.Core.Contracts;

namespace Player.App.FileOps;

public static class FileOpsServiceCollectionExtensions
{
	public static IServiceCollection AddFileOps(this IServiceCollection services)
	{
		ArgumentNullException.ThrowIfNull(services);
		services.AddSingleton<ConsentLauncher>();
		services.AddSingleton<IFileOperations, MediaStoreFileOperations>();
		return services;
	}
}
