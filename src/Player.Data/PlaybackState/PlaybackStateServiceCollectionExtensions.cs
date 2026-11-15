using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Player.Core.Contracts;

namespace Player.Data.PlaybackStateStore;

public static class PlaybackStateServiceCollectionExtensions
{
    /// <summary>Registers the playback_state migration and repository. The recorder is registered elsewhere.</summary>
    public static IServiceCollection AddPlaybackStateStore(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IDbMigration, PlaybackStateMigration>());
        services.AddSingleton<IPlaybackStateStore, PlaybackStateRepository>();
        return services;
    }
}
