using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls;
using Player.Core.Contracts;

namespace Player.App.Features.PlayerHud;

public static class PlayerHudServiceCollectionExtensions
{
    public static IServiceCollection AddPlayerHud(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddSingleton<IStringSource, PlayerStrings>();
        services.AddSingleton<PlayerScreenKeeper>();
        services.AddTransient<PlayerViewModel>();
        services.AddTransient<PlayerPage>();
        Routing.RegisterRoute("player", typeof(PlayerPage));
        return services;
    }
}
