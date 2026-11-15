using Android.App;
using Android.Content.PM;
using Android.Content.Res;
using Android.OS;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui;
using Player.App.Pip;
using Player.App.Platforms.Android.Lifecycle;

namespace Player.App;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density, SupportsPictureInPicture = true)]
public class MainActivity : MauiAppCompatActivity
{
	protected override void OnCreate(Bundle? savedInstanceState)
	{
		base.OnCreate(savedInstanceState);
		ApplyThemeSystemBars();
	}

	// Foreground means started: a picture-in-picture window is started but not resumed.
	protected override void OnStart()
	{
		base.OnStart();
		ResolveLifecycle()?.NotifyForegrounded();
	}

	protected override void OnResume()
	{
		base.OnResume();
		ApplyThemeSystemBars();
	}

	protected override void OnStop()
	{
		base.OnStop();
		ResolvePip()?.OnActivityStopped(this);
		ResolveLifecycle()?.NotifyBackgrounded();
	}

	protected override void OnDestroy()
	{
		ResolvePip()?.OnActivityDestroyed(this);
		base.OnDestroy();
		if (IsFinishing)
		{
			ResolveLifecycle()?.NotifyStopping();
		}
	}

	public override void OnPictureInPictureModeChanged(bool isInPictureInPictureMode, Configuration? newConfig)
	{
		base.OnPictureInPictureModeChanged(isInPictureInPictureMode, newConfig);
		ResolvePip()?.OnPictureInPictureModeChanged(this, isInPictureInPictureMode);
	}

	protected override void OnUserLeaveHint()
	{
		base.OnUserLeaveHint();
		ResolvePip()?.OnUserLeaveHint();
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

	private static PictureInPictureController? ResolvePip() =>
		IPlatformApplication.Current?.Services.GetService<PictureInPictureController>();
}
