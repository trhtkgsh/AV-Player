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
        services.AddSingleton<AbRepeatDraft>();
        services.AddTransient<RepeatSheetViewModel>();
        services.AddTransient<RepeatSheet>();
        services.AddTransient<AudioSheetViewModel>();
        services.AddTransient<AudioSheet>();
        // Flat route names on purpose: a route that starts with another registered route's name is resolved wrongly by Shell.
        Routing.RegisterRoute("player-speed", typeof(SpeedSheet));
        Routing.RegisterRoute("player-repeat", typeof(RepeatSheet));
        Routing.RegisterRoute("player-audio", typeof(AudioSheet));
        return services;
    }
}
