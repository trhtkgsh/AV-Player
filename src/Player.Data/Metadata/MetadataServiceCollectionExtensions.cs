using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Player.Core.Contracts;

namespace Player.Data.Metadata;

public static class MetadataServiceCollectionExtensions
{
    public static IServiceCollection AddMediaMetadataStore(this IServiceCollection services)
    {
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IDbMigration, MediaMetadataMigration>());
        services.TryAddSingleton<MediaMetadataRepository>();
        return services;
    }
}
