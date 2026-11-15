using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls;

namespace Player.App.Features.DefaultsSettings;

public static class GlobalDefaultsUiServiceCollectionExtensions
{
    public static IServiceCollection AddGlobalDefaultsUi(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddTransient<GlobalDefaultsViewModel>();
        services.AddTransient<GlobalDefaultsPage>();

        // The route name is flat on purpose: Shell resolves a route that starts with another route's name wrongly.
        Routing.RegisterRoute("settings-defaults", typeof(GlobalDefaultsPage));
        return services;
    }
}
