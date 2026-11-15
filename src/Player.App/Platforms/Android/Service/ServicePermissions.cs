using Android.App;

// The playback service runs in the foreground with the type "media playback" (required from API 34),
// and shows a notification (asked for on API 33 and later; playback works without it).
[assembly: UsesPermission(Android.Manifest.Permission.ForegroundService)]
[assembly: UsesPermission(Android.Manifest.Permission.ForegroundServiceMediaPlayback)]
[assembly: UsesPermission(Android.Manifest.Permission.PostNotifications)]
