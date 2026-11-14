using Player.Core.Contracts;

namespace Player.App.Platforms.Android.Library;

public static class MediaLibraryServiceCollectionExtensions
{
	public static IServiceCollection AddMediaLibrary(this IServiceCollection services)
	{
		ArgumentNullException.ThrowIfNull(services);
		services.AddSingleton<IMediaLibrary, MediaStoreLibrary>();
		return services;
	}
}
