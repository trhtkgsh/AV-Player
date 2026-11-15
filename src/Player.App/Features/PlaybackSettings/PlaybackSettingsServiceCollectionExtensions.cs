using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls;

namespace Player.App.Features.PlaybackSettings;

public static class PlaybackSettingsServiceCollectionExtensions
{
    public static IServiceCollection AddPlaybackSettings(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddTransient<SpeedSheetViewModel>();
        services.AddTransient<SpeedSheet>();
        // Flat route name on purpose: a route that starts with another registered route's name is resolved wrongly by Shell.
        Routing.RegisterRoute("player-speed", typeof(SpeedSheet));
        return services;
    }
}
