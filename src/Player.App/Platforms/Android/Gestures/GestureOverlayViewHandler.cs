using Microsoft.Maui.Handlers;
using Player.Core.Contracts;

namespace Player.App.Gestures;

internal sealed class GestureOverlayViewHandler() : ViewHandler<GestureOverlayView, GestureOverlayNativeView>(ViewMapper)
{
    protected override GestureOverlayNativeView CreatePlatformView()
    {
        var services = MauiContext?.Services ?? throw new InvalidOperationException("No MAUI context.");
        return new GestureOverlayNativeView(Context, services.GetRequiredService<GestureSource>());
    }
}

public static class GestureBuilderExtensions
{
    /// <summary>Registers the native handler of <see cref="GestureOverlayView"/>. Pair it with <c>AddGestures</c>.</summary>
    public static MauiAppBuilder UseGestureOverlay(this MauiAppBuilder builder)
    {
        builder.ConfigureMauiHandlers(handlers => handlers.AddHandler<GestureOverlayView, GestureOverlayViewHandler>());
        return builder;
    }
}

public static class GestureServiceCollectionExtensions
{
    /// <summary>One gesture source for the whole process, exposed as <see cref="IGestureSource"/>.</summary>
    public static IServiceCollection AddGestures(this IServiceCollection services)
    {
        services.AddSingleton<GestureSource>();
        services.AddSingleton<IGestureSource>(provider => provider.GetRequiredService<GestureSource>());
        return services;
    }
}
