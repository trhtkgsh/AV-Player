using Player.Core.Contracts;
using Player.Data.Metadata;

namespace Player.App.Platforms.Android.Thumbnails;

public static class ThumbnailServiceCollectionExtensions
{
    public static IServiceCollection AddThumbnailCache(this IServiceCollection services)
    {
        services.AddMediaMetadataStore();
        services.AddSingleton<AndroidMetadataExtractor>();
        services.AddSingleton<IMediaMetadataService, MediaMetadataService>();
        return services;
    }
}
