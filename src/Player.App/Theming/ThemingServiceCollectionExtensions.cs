using Microsoft.Extensions.DependencyInjection;
using Player.Core.Contracts;
using Player.Data.ThemeStorage;

namespace Player.App.Theming;

public static class ThemingServiceCollectionExtensions
{
    public static IServiceCollection AddTheming(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddThemeStorage();
        services.AddSingleton<ThemeService>();
        services.AddSingleton<IThemeService>(static sp => sp.GetRequiredService<ThemeService>());
        return services;
    }
}
