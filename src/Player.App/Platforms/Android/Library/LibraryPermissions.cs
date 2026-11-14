using Android;
using Android.App;

[assembly: UsesPermission(Manifest.Permission.ReadMediaVideo)]
[assembly: UsesPermission(Manifest.Permission.ReadMediaAudio)]
[assembly: UsesPermission(Manifest.Permission.ReadExternalStorage, MaxSdkVersion = 32)]
[assembly: UsesPermission("android.permission.READ_MEDIA_VISUAL_USER_SELECTED")]

namespace Player.App.Platforms.Android.Library;

/// <summary>
/// Video and audio read access only (no images), chosen by API level. Used to REQUEST permissions;
/// the granted state is evaluated by MediaStoreLibrary because MAUI reports Granted only when every entry is granted.
/// </summary>
internal sealed class MediaReadPermission : Permissions.BasePlatformPermission
{
	internal const string VisualUserSelected = "android.permission.READ_MEDIA_VISUAL_USER_SELECTED";

	public override (string androidPermission, bool isRuntime)[] RequiredPermissions
	{
		get
		{
			if (OperatingSystem.IsAndroidVersionAtLeast(34))
			{
				// VISUAL_USER_SELECTED together with READ_MEDIA_VIDEO is how Android 14 offers "Select photos and videos".
				return
				[
					(Manifest.Permission.ReadMediaVideo, true),
					(VisualUserSelected, true),
					(Manifest.Permission.ReadMediaAudio, true),
				];
			}

			return OperatingSystem.IsAndroidVersionAtLeast(33)
				? [(Manifest.Permission.ReadMediaVideo, true), (Manifest.Permission.ReadMediaAudio, true)]
				: [(Manifest.Permission.ReadExternalStorage, true)];
		}
	}
}
