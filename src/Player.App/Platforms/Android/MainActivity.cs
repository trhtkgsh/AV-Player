using Android.App;
using Android.Content.PM;
using Android.OS;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui;
using Player.App.Platforms.Android.Lifecycle;

namespace Player.App;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
	protected override void OnResume()
	{
		base.OnResume();
		ResolveLifecycle()?.NotifyForegrounded();
	}

	protected override void OnStop()
	{
		base.OnStop();
		ResolveLifecycle()?.NotifyBackgrounded();
	}

	protected override void OnDestroy()
	{
		base.OnDestroy();
		if (IsFinishing)
		{
			ResolveLifecycle()?.NotifyStopping();
		}
	}

	private static AppLifecycle? ResolveLifecycle() =>
		IPlatformApplication.Current?.Services.GetService<AppLifecycle>();
}
