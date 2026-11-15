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
    private bool _sheetOpen;
    private bool _background;
    private bool _applied;

    public PlayerScreenKeeper(IPlayerEngine engine, IPictureInPictureController pictureInPicture, IAppLifecycle lifecycle)
    {
        _engine = engine;
        _pip = pictureInPicture;
        engine.SnapshotChanged += (_, _) => OnUi(() => Apply(force: false));
        pictureInPicture.Changed += (_, _) => OnUi(() => Apply(force: false));
        lifecycle.Backgrounded += (_, _) => OnUi(() => { _background = true; Apply(force: false); });
        lifecycle.Foregrounded += (_, _) => OnUi(() => { _background = false; Apply(force: true); });
    }

    /// <summary>
    /// Called by the view model: true when the page is activated (it is then the visible page again, so a settings
    /// sheet is closed), false when it is left or covered by a sheet.
    /// </summary>
    public void SetPageActive(bool active) => OnUi(() =>
    {
        _pageActive = active;
        if (active) _sheetOpen = false;
        Apply(force: active);
    });

    /// <summary>
    /// Called before a settings sheet opens over the page. The page still shows the picture under the sheet, so the
    /// flag is kept even though the page is deactivated while the sheet is up.
    /// </summary>
    public void SetSheetOpen(bool open) => OnUi(() => { _sheetOpen = open; Apply(force: false); });

    private void Apply(bool force)
    {
        // The picture is on the screen in Picture-in-Picture, and on the page (or under a settings sheet on it)
        // while the app is in front. Audio-only files show no picture, so they never keep the screen on.
        var snapshot = _engine.Snapshot;
        bool picture = _pip.IsActive || ((_pageActive || _sheetOpen) && !_background);
        bool on = picture && snapshot.Status == PlayerStatus.Playing && snapshot.HasVideo && snapshot.VideoEnabled;
        if (!force && on == _applied) return;

        try
        {
            // Written even when the value looks unchanged: when no window was found the write did nothing, so the
            // cached value is not trusted across foreground and page activation (force).
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
