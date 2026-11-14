using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;

namespace EngineSpike;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
	const string CommandAction = "com.arielplayer.enginespike.CMD";

	protected override void OnCreate(Bundle? savedInstanceState)
	{
		base.OnCreate(savedInstanceState);

		// Test hook: adb shell am broadcast -a com.arielplayer.enginespike.CMD --es cmd <name>
		var filter = new IntentFilter(CommandAction);
		if (OperatingSystem.IsAndroidVersionAtLeast(33))
			RegisterReceiver(new CommandReceiver(), filter, ReceiverFlags.Exported);
		else
			RegisterReceiver(new CommandReceiver(), filter);
	}

	sealed class CommandReceiver : BroadcastReceiver
	{
		public override void OnReceive(Context? context, Intent? intent) =>
			SpikeCommands.Dispatch(intent?.GetStringExtra("cmd"));
	}
}
