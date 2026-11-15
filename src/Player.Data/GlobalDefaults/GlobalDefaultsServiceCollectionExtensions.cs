using Microsoft.Extensions.DependencyInjection;
using Player.Core.Contracts;

namespace Player.Data.GlobalDefaultsStore;

public static class GlobalDefaultsServiceCollectionExtensions
{
	public static IServiceCollection AddGlobalDefaults(this IServiceCollection services)
	{
		services.AddSingleton<IGlobalDefaultsStore, GlobalDefaultsStore>();
		return services;
	}
}
