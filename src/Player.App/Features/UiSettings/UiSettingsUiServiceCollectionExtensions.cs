using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls;
using Player.Core.Contracts;

namespace Player.App.Features.InterfaceSettings;

public static class UiSettingsUiServiceCollectionExtensions
{
    public static IServiceCollection AddUiSettingsUi(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<IStringSource, InterfaceStrings>();
        services.AddTransient<SettingsHomeViewModel>();
        services.AddTransient<SettingsHomePage>();
        services.AddTransient<UiSettingsViewModel>();
        services.AddTransient<UiSettingsPage>();

        // Flat route names on purpose: Shell resolves a route that starts with another route's name wrongly.
        Routing.RegisterRoute("settings", typeof(SettingsHomePage));
        Routing.RegisterRoute("settings-ui", typeof(UiSettingsPage));
        return services;
    }
}
