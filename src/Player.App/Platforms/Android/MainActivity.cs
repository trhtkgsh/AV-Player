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
	protected override void OnCreate(Bundle? savedInstanceState)
	{
		base.OnCreate(savedInstanceState);
		ApplyThemeSystemBars();
	}

	protected override void OnResume()
	{
		base.OnResume();
		ApplyThemeSystemBars();
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

	// The theme can be applied before this activity exists, so the bars are set again whenever the activity comes up.
	private void ApplyThemeSystemBars()
	{
		try
		{
			(IPlatformApplication.Current?.Services.GetService<Player.Core.Contracts.IThemeService>() as Player.App.Theming.ThemeService)?.ApplySystemBars();
		}
		catch (Exception ex)
		{
			Android.Util.Log.Warn("ArielApp", $"Applying the system bars failed: {ex}");
		}
	}

	private static AppLifecycle? ResolveLifecycle() =>
		IPlatformApplication.Current?.Services.GetService<AppLifecycle>();
}
