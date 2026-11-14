using Microsoft.Extensions.DependencyInjection;
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

		// Phase 1 placeholders. Replaced, not extended, in Phase 2.
		services.AddSingleton<IUiSettingsStore, InMemoryUiSettingsStore>();
		services.AddSingleton<IGlobalDefaultsStore, InMemoryGlobalDefaultsStore>();
		services.AddSingleton<IPlaybackStateStore, InMemoryPlaybackStateStore>();
		services.AddSingleton<IPlaybackStateRecorder, NoOpPlaybackStateRecorder>();

		return services;
	}
}
