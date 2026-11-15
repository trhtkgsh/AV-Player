#if ANDROID
using AndroidX.Core.View;
using Microsoft.Maui.ApplicationModel;
using AndroidColor = global::Android.Graphics.Color;

namespace Player.App.Theming;

public sealed partial class ThemeService
{
    // Up to API 34 the window colors the bars. From API 35 these calls do nothing and the area behind the status
    // bar is the Shell app bar, which follows ColorSurface through Shell.BackgroundColor in Styles.xaml.
    // Icon brightness goes through WindowInsetsControllerCompat on every level. Bars are never hidden or shown here.
    partial void ApplyBarsCore(uint surface, bool dark)
    {
        var activity = Platform.CurrentActivity;
        var window = activity?.Window;
        if (window is null)
        {
            return;
        }

        // The strip behind the status bar from API 35 on, and the app bars Shell creates later.
        AppBarTinter.Apply(activity, surface, CurrentBarSurface);

        if (!OperatingSystem.IsAndroidVersionAtLeast(35))
        {
#pragma warning disable CA1422 // Obsolete from API 35 only; guarded by the version check above.
            var color = new AndroidColor(unchecked((int)surface));
            window.SetStatusBarColor(color);
            window.SetNavigationBarColor(color);
#pragma warning restore CA1422
        }

        var controller = WindowCompat.GetInsetsController(window, window.DecorView);
        if (controller is not null)
        {
            controller.AppearanceLightStatusBars = !dark;
            controller.AppearanceLightNavigationBars = !dark;
        }
    }

    private uint? CurrentBarSurface()
    {
        lock (_stateLock)
        {
            return _hasTheme ? _barSurface : null;
        }
    }
}
#endif
