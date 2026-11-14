using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls;

namespace Player.App.Features.PlayerHud;

public static class PlayerHudServiceCollectionExtensions
{
    public static IServiceCollection AddPlayerHud(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddTransient<PlayerViewModel>();
        services.AddTransient<PlayerPage>();
        Routing.RegisterRoute("player", typeof(PlayerPage));
        return services;
    }
}
