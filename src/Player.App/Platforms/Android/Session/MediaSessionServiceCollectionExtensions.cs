using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Hosting;
using Player.Core.Contracts;

namespace Player.App.MediaControls;

public static class MediaSessionServiceCollectionExtensions
{
    /// <summary>
    /// The process-wide media session bridge, exposed as <see cref="IMediaSessionBridge"/> too. A start hook makes it
    /// follow the engine and the coordinator from app start. Needs the engine, the coordinator, the metadata service,
    /// the UI settings store, the localizer and a <see cref="TimeProvider"/>.
    /// </summary>
    public static IServiceCollection AddMediaSession(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddSingleton<IStringSource, SessionStrings>();
        services.AddSingleton<MediaSessionBridge>();
        services.AddSingleton<IMediaSessionBridge>(provider => provider.GetRequiredService<MediaSessionBridge>());
        services.AddSingleton<IMauiInitializeService, MediaSessionStarter>();
        return services;
    }

    private sealed class MediaSessionStarter : IMauiInitializeService
    {
        public void Initialize(IServiceProvider services) =>
            services.GetRequiredService<MediaSessionBridge>().Start();
    }
}
