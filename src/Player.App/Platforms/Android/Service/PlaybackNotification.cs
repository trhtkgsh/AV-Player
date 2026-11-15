using Android.App;
using Android.Content;
using Android.Graphics.Drawables;
using Player.Core.Contracts;
using Player.Core.Models;

namespace Player.App.Background;

/// <summary>
/// Builds the media notification of the playback service: title, previous, play or pause, next, close.
/// From API 33 the system draws the buttons from the media session; the actions here serve older versions
/// and the close button.
/// </summary>
internal sealed class PlaybackNotification(ILocalizer localizer)
{
    internal const string ActionToggle = "com.arielplayer.app.service.TOGGLE";
    internal const string ActionNext = "com.arielplayer.app.service.NEXT";
    internal const string ActionPrevious = "com.arielplayer.app.service.PREVIOUS";
    internal const string ActionClose = "com.arielplayer.app.service.CLOSE";
    private const string ChannelId = "playback";

    public Notification Build(Context context, MediaItem? item, bool playing, object? sessionToken)
    {
        EnsureChannel(context);

        var builder = new Notification.Builder(context, ChannelId)
            .SetSmallIcon(Android.Resource.Drawable.IcMediaPlay)!
            .SetContentTitle(item?.DisplayName ?? "")!
            .SetContentText(item?.FolderName)!
            .SetOngoing(playing)!
            .SetOnlyAlertOnce(true)!
            .SetShowWhen(false)!
            .SetCategory(Notification.CategoryTransport)!
            .SetVisibility(NotificationVisibility.Public)!;

        if (context.PackageManager?.GetLaunchIntentForPackage(context.PackageName!) is { } launch)
        {
            builder.SetContentIntent(PendingIntent.GetActivity(
                context, 0, launch, PendingIntentFlags.Immutable | PendingIntentFlags.UpdateCurrent));
        }

        builder.AddAction(Action(context, Android.Resource.Drawable.IcMediaPrevious, "Service.Previous", ActionPrevious, 1));
        builder.AddAction(playing
            ? Action(context, Android.Resource.Drawable.IcMediaPause, "Service.Pause", ActionToggle, 2)
            : Action(context, Android.Resource.Drawable.IcMediaPlay, "Service.Play", ActionToggle, 2));
        builder.AddAction(Action(context, Android.Resource.Drawable.IcMediaNext, "Service.Next", ActionNext, 3));
        builder.AddAction(Action(context, Android.Resource.Drawable.IcMenuCloseClearCancel, "Service.Close", ActionClose, 4));

        var style = new Notification.MediaStyle().SetShowActionsInCompactView(0, 1, 2)!;
        if (sessionToken is Android.Media.Session.MediaSession.Token token)
            style.SetMediaSession(token);
        builder.SetStyle(style);

        return builder.Build()!;
    }

    private Notification.Action Action(Context context, int icon, string textKey, string action, int requestCode)
    {
        var intent = new Intent(context, typeof(PlaybackService)).SetAction(action)!;
        var pending = PendingIntent.GetService(
            context, requestCode, intent, PendingIntentFlags.Immutable | PendingIntentFlags.UpdateCurrent);
        return new Notification.Action.Builder(Icon.CreateWithResource(context, icon), localizer[textKey], pending).Build()!;
    }

    /// <summary>Creating a channel that exists only updates its name (the language may have changed).</summary>
    private void EnsureChannel(Context context)
    {
        if (context.GetSystemService(Context.NotificationService) is not NotificationManager manager)
            return;

        var channel = new NotificationChannel(ChannelId, localizer["Service.Channel"], NotificationImportance.Low);
        channel.SetShowBadge(false);
        manager.CreateNotificationChannel(channel);
    }
}
