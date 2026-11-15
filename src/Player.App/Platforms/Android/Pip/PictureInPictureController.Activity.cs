using Android.App;
using Android.Content;
using Android.Util;
using AndroidX.Lifecycle;
using Microsoft.Maui.ApplicationModel;

namespace Player.App.Pip;

// The activity callbacks, the action receiver and the close rule. Everything here runs on the main thread
// except the work the actions start.
public sealed partial class PictureInPictureController
{
    private PipActionReceiver? _receiver;
    private Context? _receiverHost;

    /// <summary>Called by MainActivity (main thread) from OnPictureInPictureModeChanged.</summary>
    internal void OnPictureInPictureModeChanged(bool isInPictureInPictureMode)
    {
        try
        {
            var activity = Platform.CurrentActivity;
            if (isInPictureInPictureMode)
            {
                _isActive = true;
                if (activity is not null)
                {
                    RegisterReceiver(activity);
                }

                RaiseChanged();
                UpdateParams();
                return;
            }

            _isActive = false;
            UnregisterReceiver();
            RaiseChanged();

            // Out of PiP with the activity not started: the user closed the window. (A screen that turns off
            // while PiP is shown raises no callback. Expanding to full screen leaves the activity started.)
            if (activity is not null && !IsAtLeastStarted(activity))
            {
                _ = Task.Run(CloseAsync);
            }
        }
        catch (Exception ex)
        {
            Log.Warn(Tag, $"Picture-in-Picture mode change failed: {ex}");
        }
    }

    /// <summary>Called by MainActivity (main thread) from OnUserLeaveHint. Below API 31 only; later versions auto-enter.</summary>
    internal void OnUserLeaveHint()
    {
        try
        {
            if (OperatingSystem.IsAndroidVersionAtLeast(31) || _isActive || !_leaveSetting || !IsSupported || !IsVideoPlaying)
            {
                return;
            }

            var activity = Platform.CurrentActivity;
            if (activity is null)
            {
                return;
            }

            activity.EnterPictureInPictureMode(BuildParams(activity));
        }
        catch (Exception ex)
        {
            Log.Warn(Tag, $"Entering Picture-in-Picture on leave failed: {ex}");
        }
    }

    private static bool IsAtLeastStarted(Activity activity) =>
        activity is ILifecycleOwner owner && !owner.Lifecycle.CurrentState.IsAtLeast(Lifecycle.State.Started!);

    private async Task CloseAsync()
    {
        try
        {
            await _coordinator.CloseAsync(CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Log.Warn(Tag, $"Closing playback after the Picture-in-Picture window closed failed: {ex}");
        }
    }

    private void RegisterReceiver(Activity activity)
    {
        if (_receiver is not null)
        {
            return;
        }

        var filter = new IntentFilter();
        filter.AddAction(ActionPrevious);
        filter.AddAction(ActionPlay);
        filter.AddAction(ActionPause);
        filter.AddAction(ActionNext);

        var receiver = new PipActionReceiver(HandleActionAsync);
        if (OperatingSystem.IsAndroidVersionAtLeast(33))
        {
            activity.RegisterReceiver(receiver, filter, ReceiverFlags.NotExported);
        }
        else
        {
            activity.RegisterReceiver(receiver, filter);
        }

        _receiver = receiver;
        _receiverHost = activity;
    }

    private void UnregisterReceiver()
    {
        var receiver = _receiver;
        var host = _receiverHost;
        _receiver = null;
        _receiverHost = null;
        if (receiver is null || host is null)
        {
            return;
        }

        try
        {
            host.UnregisterReceiver(receiver);
        }
        catch (Exception ex)
        {
            Log.Warn(Tag, $"Unregistering the Picture-in-Picture receiver failed: {ex}");
        }
        finally
        {
            receiver.Dispose();
        }
    }

    /// <summary>Runs off the main thread. Never throws.</summary>
    private async Task HandleActionAsync(string? action)
    {
        try
        {
            var ct = _lifetime.Token;
            switch (action)
            {
                case ActionPlay:
                    await _engine.PlayAsync(ct).ConfigureAwait(false);
                    break;
                case ActionPause:
                    await _engine.PauseAsync(ct).ConfigureAwait(false);
                    break;
                case ActionNext:
                    await _coordinator.NextAsync(ct).ConfigureAwait(false);
                    break;
                case ActionPrevious:
                    await _coordinator.PreviousAsync(ct).ConfigureAwait(false);
                    break;
            }
        }
        catch (OperationCanceledException)
        {
            // Disposed.
        }
        catch (Exception ex)
        {
            Log.Warn(Tag, $"Picture-in-Picture action '{action}' failed: {ex}");
        }
    }
}
