using Android;
using Android.App;

[assembly: UsesPermission(Manifest.Permission.ReadMediaVideo)]
[assembly: UsesPermission(Manifest.Permission.ReadMediaAudio)]
[assembly: UsesPermission(Manifest.Permission.ReadExternalStorage, MaxSdkVersion = 32)]
[assembly: UsesPermission("android.permission.READ_MEDIA_VISUAL_USER_SELECTED")]

namespace Player.App.Platforms.Android.Library;

/// <summary>Video and audio read access only (no images), chosen by API level.</summary>
internal sealed class MediaReadPermission : Permissions.BasePlatformPermission
{
	public override (string androidPermission, bool isRuntime)[] RequiredPermissions =>
		OperatingSystem.IsAndroidVersionAtLeast(33)
			? [(Manifest.Permission.ReadMediaVideo, true), (Manifest.Permission.ReadMediaAudio, true)]
			: [(Manifest.Permission.ReadExternalStorage, true)];
}
