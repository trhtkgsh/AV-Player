using Microsoft.Extensions.DependencyInjection;
using Player.Core.Contracts;

namespace Player.App.Localization;

/// <summary>Dependency injection registration of the localization module.</summary>
public static class LocalizationServiceCollectionExtensions
{
    /// <summary>Registers the <see cref="Localizer"/> as a singleton (also as <see cref="ILocalizer"/>) and the common texts.</summary>
    public static IServiceCollection AddLocalization(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddSingleton<Localizer>();
        services.AddSingleton<ILocalizer>(static sp => sp.GetRequiredService<Localizer>());
        services.AddSingleton<IStringSource, CommonStrings>();
        return services;
    }
}
