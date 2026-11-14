using Player.Core.Contracts;

namespace Player.App.Engine;

public static class PlayerEngineServiceCollectionExtensions
{
    /// <summary>One engine instance for the whole process, exposed through both of its contracts.</summary>
    public static IServiceCollection AddPlayerEngine(this IServiceCollection services)
    {
        services.AddSingleton<VlcPlayerEngine>();
        services.AddSingleton<IPlayerEngine>(provider => provider.GetRequiredService<VlcPlayerEngine>());
        services.AddSingleton<IVideoOutputBinding>(provider => provider.GetRequiredService<VlcPlayerEngine>());
        return services;
    }
}
