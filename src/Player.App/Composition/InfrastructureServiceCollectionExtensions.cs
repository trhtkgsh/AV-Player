using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Hosting;
using Player.App.Composition.Placeholders;
using Player.App.Platforms.Android.Lifecycle;
using Player.Core.Contracts;

namespace Player.App.Composition;

public static class InfrastructureServiceCollectionExtensions
{
	public static IServiceCollection AddAppInfrastructure(this IServiceCollection services)
	{
		ArgumentNullException.ThrowIfNull(services);

		services.AddSingleton<TimeProvider>(TimeProvider.System);

		services.AddSingleton<AppLifecycle>();
		services.AddSingleton<IAppLifecycle>(sp => sp.GetRequiredService<AppLifecycle>());

		// Phase 1 placeholder. Replaced by media-service in Phase 4.
		services.AddSingleton<IMauiInitializeService, PauseOnBackgroundPlaceholder>();

		return services;
	}
}
