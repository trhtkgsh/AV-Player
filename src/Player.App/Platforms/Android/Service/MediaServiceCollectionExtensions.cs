using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Hosting;
using Player.Core.Contracts;

namespace Player.App.Background;

public static class MediaServiceCollectionExtensions
{
    /// <summary>
    /// The playback service and its controller. Needs the engine, the coordinator, the media session,
    /// the global defaults, the app lifecycle, the state recorder and the localizer.
    /// </summary>
    public static IServiceCollection AddMediaService(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddSingleton<IStringSource, ServiceStrings>();
        services.AddSingleton<PlaybackServiceController>();
        services.AddSingleton<IPlaybackServiceController>(sp => sp.GetRequiredService<PlaybackServiceController>());
        services.AddSingleton<IMauiInitializeService, MediaServiceStart>();
        return services;
    }

    /// <summary>The controller follows playback from app start; nothing else would ever create it.</summary>
    private sealed class MediaServiceStart : IMauiInitializeService
    {
        public void Initialize(IServiceProvider services) =>
            services.GetRequiredService<PlaybackServiceController>().Start();
    }
}
