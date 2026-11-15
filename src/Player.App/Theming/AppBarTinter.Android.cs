#if ANDROID
using System.Runtime.CompilerServices;
using Android.OS;
using Android.Views;
using AndroidX.AppCompat.App;
using AndroidX.Fragment.App;
using Google.Android.Material.AppBar;
using AndroidColor = global::Android.Graphics.Color;
using AndroidLog = global::Android.Util.Log;
using View = global::Android.Views.View;

namespace Player.App.Theming;

/// <summary>
/// Colors every <see cref="AppBarLayout"/> of an activity with the surface color of the theme. From API 35 on (edge to
/// edge) the strip behind the status bar belongs to the AppBarLayout, whose own background comes from the static
/// <c>colorPrimary</c>; Shell only colors the toolbar inside it. Done on every API level, where it is harmless.
/// </summary>
internal static class AppBarTinter
{
    private const string LogTag = "ArielTheme";

    // One registration per activity instance; a recreated activity is a new key. Nothing here keeps a dead activity alive.
    private static readonly ConditionalWeakTable<AppCompatActivity, object> Registered = new();

    /// <summary>Colors the app bars now and registers once per activity to color the ones created later. UI thread only.</summary>
    public static void Apply(global::Android.App.Activity? activity, uint surface, Func<uint?> currentSurface)
    {
        try
        {
            if (activity?.Window?.DecorView is { } root)
            {
                Tint(root, surface);
            }

            if (activity is AppCompatActivity compat && !Registered.TryGetValue(compat, out _))
            {
                Registered.Add(compat, new object());
                compat.SupportFragmentManager.RegisterFragmentLifecycleCallbacks(new Callbacks(currentSurface), true);
            }
        }
        catch (Exception ex)
        {
            AndroidLog.Warn(LogTag, $"Coloring the app bars failed: {ex.Message}");
        }
    }

    private static void Tint(View? view, uint surface)
    {
        if (view is null)
        {
            return;
        }

        if (view is AppBarLayout bar)
        {
            bar.SetBackgroundColor(new AndroidColor(unchecked((int)surface)));
            return;
        }

        if (view is ViewGroup group)
        {
            for (var i = 0; i < group.ChildCount; i++)
            {
                Tint(group.GetChildAt(i), surface);
            }
        }
    }

    private sealed class Callbacks(Func<uint?> currentSurface) : FragmentManager.FragmentLifecycleCallbacks
    {
        public override void OnFragmentViewCreated(FragmentManager fm, Fragment f, View v, Bundle? savedInstanceState) =>
            TintSafely(v);

        public override void OnFragmentResumed(FragmentManager fm, Fragment f) => TintSafely(f.View);

        private void TintSafely(View? view)
        {
            try
            {
                if (currentSurface() is { } surface)
                {
                    Tint(view, surface);
                }
            }
            catch (Exception ex)
            {
                AndroidLog.Warn(LogTag, $"Coloring the app bars failed: {ex.Message}");
            }
        }
    }
}
#endif
