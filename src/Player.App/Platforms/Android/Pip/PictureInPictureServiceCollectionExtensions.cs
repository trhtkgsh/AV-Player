using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Hosting;
using Player.Core.Contracts;

namespace Player.App.Pip;

public static class PictureInPictureServiceCollectionExtensions
{
    /// <summary>
    /// Registers the PiP controller (one instance behind both the class and its contract), its texts, and a
    /// start hook that creates it at app start so it follows the engine from the first file.
    /// </summary>
    public static IServiceCollection AddPictureInPicture(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddSingleton<IStringSource, PipStrings>();
        services.AddSingleton<PictureInPictureController>();
        services.AddSingleton<IPictureInPictureController>(sp => sp.GetRequiredService<PictureInPictureController>());
        services.AddSingleton<IMauiInitializeService, PictureInPictureStarter>();
        return services;
    }

    // Runs on the UI thread, like the other starters.
    private sealed class PictureInPictureStarter : IMauiInitializeService
    {
        public void Initialize(IServiceProvider services)
        {
            ArgumentNullException.ThrowIfNull(services);
            _ = services.GetRequiredService<PictureInPictureController>();
        }
    }
}
