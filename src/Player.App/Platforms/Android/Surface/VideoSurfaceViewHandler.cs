using Microsoft.Maui.Handlers;
using Player.Core.Contracts;

namespace Player.App.Surface;

internal sealed class VideoSurfaceViewHandler() : ViewHandler<VideoSurfaceView, VideoSurfaceContainer>(ViewMapper)
{
    private VideoSurfaceController? _controller;

    protected override VideoSurfaceContainer CreatePlatformView()
    {
        var services = MauiContext?.Services ?? throw new InvalidOperationException("No MAUI context.");
        _controller = services.GetRequiredService<VideoSurfaceController>();
        return new VideoSurfaceContainer(Context, services.GetRequiredService<IVideoOutputBinding>(), _controller);
    }

    protected override void ConnectHandler(VideoSurfaceContainer platformView)
    {
        base.ConnectHandler(platformView);
        _controller?.Attach(platformView);
    }

    protected override void DisconnectHandler(VideoSurfaceContainer platformView)
    {
        _controller?.Detach(platformView);
        base.DisconnectHandler(platformView);
    }
}

public static class VideoSurfaceBuilderExtensions
{
    /// <summary>Registers the native handler of <see cref="VideoSurfaceView"/> and <see cref="IVideoSurface"/>.</summary>
    public static MauiAppBuilder UseVideoSurface(this MauiAppBuilder builder)
    {
        builder.Services.AddSingleton<VideoSurfaceController>();
        builder.Services.AddSingleton<IVideoSurface>(provider => provider.GetRequiredService<VideoSurfaceController>());
        builder.ConfigureMauiHandlers(handlers => handlers.AddHandler<VideoSurfaceView, VideoSurfaceViewHandler>());
        return builder;
    }
}
