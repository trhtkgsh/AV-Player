using Microsoft.Extensions.DependencyInjection;
using Player.Core.Contracts;
using Player.Data.ThemeStorage;

namespace Player.App.Theming;

public static class ThemingServiceCollectionExtensions
{
    /// <summary>Theme storage, the theme service, the theme page and its route <c>settings-theme</c>, and the page texts.</summary>
    public static IServiceCollection AddTheming(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddThemeStorage();
        services.AddSingleton<ThemeService>();
        services.AddSingleton<IThemeService>(static sp => sp.GetRequiredService<ThemeService>());
        services.AddSingleton<IStringSource, ThemeStrings>();
        services.AddTransient<ThemeViewModel>();
        services.AddTransient<ThemePage>();

        // Flat route name on purpose: Shell resolves a route that starts with another route's name wrongly.
        Routing.RegisterRoute("settings-theme", typeof(ThemePage));
        return services;
    }
}
