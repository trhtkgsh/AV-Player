using Android.App;
using Android.Content;
using Android.Util;
using AndroidX.Core.Content;
using AndroidX.Lifecycle;
using Microsoft.Maui.ApplicationModel;
using PowerManager = Android.OS.PowerManager;

namespace Player.App.Pip;

// The activity callbacks, the action receiver and the close rule. MainActivity forwards four events, all on the
// main thread. The close decision never reads Platform.CurrentActivity. The order of onStop and
// onPictureInPictureModeChanged(false) differs between Android versions, so both orders are handled: a mode
// change that finds the activity still started ends the session without a close and notes the time, and a stop
// right after it turns that into a close (see TryCloseAfterEarlyEnd).
public sealed partial class PictureInPictureController
{
    private static readonly TimeSpan StopCheckDelay = TimeSpan.FromMilliseconds(600);
    private static readonly TimeSpan ExpandCheckDelay = TimeSpan.FromMilliseconds(300);
    private const long EarlyEndWindowMs = 1000;

    private WeakReference<Activity>? _activity;
    private PipActionReceiver? _receiver;
    private Context? _receiverHost;
    private int _session;
    private long? _endedWithoutCloseAt;   // Environment.TickCount64 of a session end that did not close playback

    /// <summary>Called by MainActivity (main thread) from OnPictureInPictureModeChanged.</summary>
    internal void OnPictureInPictureModeChanged(Activity activity, bool isInPictureInPictureMode)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(activity);
            Remember(activity);
            if (isInPictureInPictureMode)
            {
                BeginPictureInPicture();
                return;
            }

            // Out of PiP. Expanded to full screen: the activity is started or resumed and playback goes on.
            // Dismissed: the activity is finishing, destroyed or below started.
            var closing = activity.IsFinishing || activity.IsDestroyed || !IsAtLeastStarted(activity);
            var wasActive = _isActive;
            EndPictureInPicture(closePlayback: closing);
            if (!closing)
            {
                // Either an expand to full screen, or a dismiss whose onStop has not arrived yet.
                if (wasActive && !IsResumed(activity))
                {
                    var mark = Environment.TickCount64;
                    _endedWithoutCloseAt = mark;
                    ScheduleExpandCheck(activity, mark);
                }

                ApplyParamsAgain();
            }
        }
        catch (Exception ex)
        {
            Log.Warn(Tag, $"Picture-in-Picture mode change failed: {ex}");
        }
    }

    /// <summary>Called by MainActivity (main thread) from OnStop.</summary>
    internal void OnActivityStopped(Activity activity)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(activity);
            Remember(activity);
            if (!_isActive)
            {
                TryCloseAfterEarlyEnd();
                return;
            }

            // The dismiss of the window may arrive without a mode callback. Look again after the system settled.
            var session = _session;
            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(StopCheckDelay, _lifetime.Token).ConfigureAwait(false);
                    MainThread.BeginInvokeOnMainThread(() => CheckDismissed(activity, session));
                }
                catch (OperationCanceledException)
                {
                    // Disposed.
                }
                catch (Exception ex)
                {
                    Log.Warn(Tag, $"Scheduling the dismiss check failed: {ex}");
                }
            });
        }
        catch (Exception ex)
        {
            Log.Warn(Tag, $"Activity stop handling failed: {ex}");
        }
    }

    /// <summary>Called by MainActivity (main thread) from OnDestroy.</summary>
    internal void OnActivityDestroyed(Activity activity)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(activity);
            if (_activity is not null && _activity.TryGetTarget(out var current) && ReferenceEquals(current, activity))
            {
                _activity = null;
            }

            if (_isActive)
            {
                EndPictureInPicture(closePlayback: activity.IsFinishing);
            }
            else if (activity.IsFinishing)
            {
                TryCloseAfterEarlyEnd();
            }
        }
        catch (Exception ex)
        {
            Log.Warn(Tag, $"Activity destroy handling failed: {ex}");
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

            var activity = GetActivity();
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

    // ---- Session ----

    private void BeginPictureInPicture()
    {
        if (_isActive)
        {
            return;
        }

        _isActive = true;
        _session++;
        _endedWithoutCloseAt = null;
        try
        {
            RegisterReceiver();
        }
        catch (Exception ex)
        {
            // Without the receiver the window has no working actions, but the session still counts.
            Log.Warn(Tag, $"Registering the Picture-in-Picture receiver failed: {ex}");
        }

        RaiseChanged();
        ApplyParamsAgain();
    }

    /// <summary>
    /// The one teardown of a PiP session (main thread). Does nothing when no session is active, so the
    /// several events of one dismiss end it once.
    /// </summary>
    private void EndPictureInPicture(bool closePlayback)
    {
        if (!_isActive)
        {
            return;
        }

        _isActive = false;
        UnregisterReceiver();
        RaiseChanged();
        if (closePlayback)
        {
            _ = Task.Run(CloseAsync);
        }
    }

    /// <summary>
    /// A session ended on the mode change with the activity still started, and now the activity stops (or is
    /// destroyed while finishing) right after: the window was dismissed and the mode callback came first.
    /// A screen that is off or a locked device is not a close.
    /// </summary>
    private void TryCloseAfterEarlyEnd()
    {
        var endedAt = _endedWithoutCloseAt;
        _endedWithoutCloseAt = null;
        if (endedAt is null || Environment.TickCount64 - endedAt.Value >= EarlyEndWindowMs || !IsScreenOnAndUnlocked())
        {
            return;
        }

        _ = Task.Run(CloseAsync);
    }

    private void CheckDismissed(Activity activity, int session)
    {
        try
        {
            if (!_isActive || session != _session || IsAtLeastStarted(activity) || !IsScreenOnAndUnlocked())
            {
                return;
            }

            EndPictureInPicture(closePlayback: true);
        }
        catch (Exception ex)
        {
            Log.Warn(Tag, $"The dismiss check failed: {ex}");
        }
    }

    // A screen that is off or a locked device stops the activity too. That is not a close.
    private static bool IsScreenOnAndUnlocked()
    {
        var context = Platform.AppContext;
        var power = context.GetSystemService(Context.PowerService) as PowerManager;
        var keyguard = context.GetSystemService(Context.KeyguardService) as KeyguardManager;
        return power?.IsInteractive == true && keyguard?.IsKeyguardLocked == false;
    }

    /// <summary>
    /// An expand to full screen resumes the activity, a dismissal never does. Shortly after the mark is set,
    /// a resumed activity clears it, so Home pressed soon after an expand is not taken for a dismissal.
    /// </summary>
    private void ScheduleExpandCheck(Activity activity, long mark)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(ExpandCheckDelay, _lifetime.Token).ConfigureAwait(false);
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    try
                    {
                        if (_endedWithoutCloseAt == mark && IsResumed(activity))
                        {
                            _endedWithoutCloseAt = null;
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Warn(Tag, $"The expand check failed: {ex}");
                    }
                });
            }
            catch (OperationCanceledException)
            {
                // Disposed.
            }
            catch (Exception ex)
            {
                Log.Warn(Tag, $"Scheduling the expand check failed: {ex}");
            }
        });
    }

    private static bool IsResumed(Activity activity) =>
        activity is ILifecycleOwner owner && owner.Lifecycle.CurrentState.IsAtLeast(Lifecycle.State.Resumed!);

    private static bool IsAtLeastStarted(Activity activity) =>
        activity is not ILifecycleOwner owner || owner.Lifecycle.CurrentState.IsAtLeast(Lifecycle.State.Started!);

    private void Remember(Activity activity)
    {
        if (_activity is null || !_activity.TryGetTarget(out var current) || !ReferenceEquals(current, activity))
        {
            _activity = new WeakReference<Activity>(activity);

            // A new activity has none of the params yet.
            ApplyParamsAgain();
        }
    }

    /// <summary>The activity MainActivity told us about, or the current one when none was.</summary>
    private Activity? GetActivity()
    {
        if (_activity is not null && _activity.TryGetTarget(out var activity) && !activity.IsDestroyed)
        {
            return activity;
        }

        return Platform.CurrentActivity;
    }

    private async Task CloseAsync()
    {
        try
        {
            await _coordinator.CloseAsync(_lifetime.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Disposed.
        }
        catch (Exception ex)
        {
            Log.Warn(Tag, $"Closing playback after the Picture-in-Picture window closed failed: {ex}");
        }
    }

    // ---- Actions ----

    private void RegisterReceiver()
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
        // Not exported on every API level, so no other app can send the actions. The application context
        // outlives the activity, so a destroyed activity cannot keep the receiver.
        var host = Platform.AppContext;
        ContextCompat.RegisterReceiver(host, receiver, filter, ContextCompat.ReceiverNotExported);

        _receiver = receiver;
        _receiverHost = host;
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
