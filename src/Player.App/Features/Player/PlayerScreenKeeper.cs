using Microsoft.Maui.Devices;
using Player.Core.Contracts;
using Player.Core.Models;

namespace Player.App.Features.PlayerHud;

/// <summary>
/// Keeps the screen on while playback runs and the picture is on the screen: the player page is active, or the app is
/// in Picture-in-Picture. The screen goes back to normal when playback is paused, ended, idle or failed, when the page
/// is left, and when the app goes to the background without Picture-in-Picture (sound only).
/// Singleton. Every state change and every write of <see cref="DeviceDisplay"/> runs on the UI thread.
/// </summary>
public sealed class PlayerScreenKeeper
{
    private readonly IPlayerEngine _engine;
    private readonly IPictureInPictureController _pip;
    private bool _pageActive;
    private bool _background;
    private bool _applied;

    public PlayerScreenKeeper(IPlayerEngine engine, IPictureInPictureController pictureInPicture, IAppLifecycle lifecycle)
    {
        _engine = engine;
        _pip = pictureInPicture;
        engine.SnapshotChanged += (_, _) => OnUi(Apply);
        pictureInPicture.Changed += (_, _) => OnUi(Apply);
        lifecycle.Backgrounded += (_, _) => OnUi(() => { _background = true; Apply(); });
        lifecycle.Foregrounded += (_, _) => OnUi(() => { _background = false; Apply(); });
    }

    /// <summary>Called by the view model: true when the page is activated, false when it is left.</summary>
    public void SetPageActive(bool active) => OnUi(() => { _pageActive = active; Apply(); });

    private void Apply()
    {
        // The picture is on the screen in Picture-in-Picture, and on the page while the app is in front.
        bool picture = _pip.IsActive || (_pageActive && !_background);
        bool on = picture && _engine.Snapshot.Status == PlayerStatus.Playing;
        if (on == _applied) return;

        try
        {
            DeviceDisplay.Current.KeepScreenOn = on;
            _applied = on;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Keep screen on failed: {ex}");
        }
    }

    private static void OnUi(Action action)
    {
        if (MainThread.IsMainThread) action();
        else MainThread.BeginInvokeOnMainThread(action);
    }
}
