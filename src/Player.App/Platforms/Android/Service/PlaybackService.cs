using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Microsoft.Extensions.DependencyInjection;

namespace Player.App.Background;

/// <summary>
/// The foreground service that keeps the process alive while a file plays. It owns nothing: the engine,
/// the coordinator and the media session are the app's singletons, and every decision is made by
/// <see cref="PlaybackServiceController"/>. Never restarted by the system: without the app's state
/// there is nothing to play.
/// </summary>
[Service(Exported = false, ForegroundServiceType = ForegroundService.TypeMediaPlayback)]
public sealed class PlaybackService : Service
{
    public override IBinder? OnBind(Intent? intent) => null;

    public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
    {
        if (Controller() is { } controller)
            controller.OnServiceCommand(this, intent?.Action);
        else
            StopSelf();   // no app container: nothing can be played

        return StartCommandResult.NotSticky;
    }

    /// <summary>The app was swiped away from the recent apps list.</summary>
    public override void OnTaskRemoved(Intent? rootIntent)
    {
        Controller()?.OnTaskRemoved();
        base.OnTaskRemoved(rootIntent);
    }

    public override void OnDestroy()
    {
        Controller()?.OnServiceDestroyed(this);
        base.OnDestroy();
    }

    private static PlaybackServiceController? Controller() =>
        IPlatformApplication.Current?.Services.GetService<PlaybackServiceController>();
}
