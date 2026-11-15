using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls;
using Player.Core.Contracts;

namespace Player.App.Features.Explorer;

public static class ExplorerServiceCollectionExtensions
{
    public static IServiceCollection AddExplorer(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<IStringSource, ExplorerStrings>();
        services.AddTransient<FoldersViewModel>();
        services.AddTransient<FoldersPage>();
        services.AddTransient<ItemsViewModel>();
        services.AddTransient<ItemsPage>();

        Routing.RegisterRoute("explorer/items", typeof(ItemsPage));
        return services;
    }
}
