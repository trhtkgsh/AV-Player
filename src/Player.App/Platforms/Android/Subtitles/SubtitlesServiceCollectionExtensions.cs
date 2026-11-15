using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Hosting;
using Player.Core.Contracts;

namespace Player.App.Subtitles;

public static class SubtitlesServiceCollectionExtensions
{
    /// <summary>Registers the subtitle service and creates it at app start, so it sees the first file that opens.</summary>
    public static IServiceCollection AddSubtitlesAudio(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddSingleton<ISubtitleService, SubtitleService>();
        services.AddSingleton<IMauiInitializeService, SubtitleServiceStarter>();
        return services;
    }

    // Runs on the UI thread, which the engine's constructor requires.
    private sealed class SubtitleServiceStarter : IMauiInitializeService
    {
        public void Initialize(IServiceProvider services)
        {
            ArgumentNullException.ThrowIfNull(services);
            _ = services.GetRequiredService<ISubtitleService>();
        }
    }
}
