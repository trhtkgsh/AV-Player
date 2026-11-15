using Microsoft.Extensions.DependencyInjection;

namespace Player.Data.ThemeStorage;

public static class ThemeStorageServiceCollectionExtensions
{
	public static IServiceCollection AddThemeStorage(this IServiceCollection services)
	{
		ArgumentNullException.ThrowIfNull(services);
		services.AddSingleton<ThemeSettingsRepository>();
		return services;
	}
}
