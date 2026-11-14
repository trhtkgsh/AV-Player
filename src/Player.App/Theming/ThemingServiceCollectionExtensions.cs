using Microsoft.Extensions.DependencyInjection;
using Player.Core.Contracts;

namespace Player.App.Theming;

public static class ThemingServiceCollectionExtensions
{
    public static IServiceCollection AddTheming(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddSingleton<IThemeService, ThemeService>();
        return services;
    }
}
