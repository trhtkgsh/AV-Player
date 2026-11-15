using Android.App;
using Android.Content;
using Android.Graphics.Drawables;
using Player.Core.Contracts;
using Player.Core.Models;

namespace Player.App.Background;

/// <summary>
/// Builds the media notification of the playback service: title, previous, play or pause, next, close.
/// From API 33 the system draws the buttons of a media notification from the media session (which
/// offers its own close action); the actions here serve older versions. Swiping the notification away
/// while paused closes playback.
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

        builder.SetDeleteIntent(Pending(context, ActionClose, 5));

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

    private Notification.Action Action(Context context, int icon, string textKey, string action, int requestCode) =>
        new Notification.Action.Builder(
            Icon.CreateWithResource(context, icon), localizer[textKey], Pending(context, action, requestCode)).Build()!;

    private static PendingIntent? Pending(Context context, string action, int requestCode) =>
        PendingIntent.GetService(
            context, requestCode, new Intent(context, typeof(PlaybackService)).SetAction(action)!,
            PendingIntentFlags.Immutable | PendingIntentFlags.UpdateCurrent);

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
