using Microsoft.Extensions.DependencyInjection;
using Player.Core.Contracts;

namespace Player.Data.UiSettingsStore;

public static class UiSettingsServiceCollectionExtensions
{
	public static IServiceCollection AddUiSettings(this IServiceCollection services)
	{
		ArgumentNullException.ThrowIfNull(services);
		services.AddSingleton<IUiSettingsStore, UiSettingsStore>();
		return services;
	}
}
