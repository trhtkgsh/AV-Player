using Android.Content;
using Android.Media.Session;
using Android.OS;
using Android.Views;
using Player.Core.Logic.Session;
using Player.Core.Models;
using Log = global::Android.Util.Log;

namespace Player.App.MediaControls;

// Commands and the session callback. Every command leaves the callback thread at once and never throws.
public sealed partial class MediaSessionBridge
{
    internal void Play() => Run("play", ct => _engine.PlayAsync(ct));

    internal void Pause() => Run("pause", ct => _engine.PauseAsync(ct));

    internal void TogglePlayPause() => Run("play/pause", ct =>
        _engine.Snapshot.Status == PlayerStatus.Playing ? _engine.PauseAsync(ct) : _engine.PlayAsync(ct));

    internal void Stop() => Run("stop", ct => _coordinator.CloseAsync(ct));

    internal void Next() => Run("next", ct => _coordinator.NextAsync(ct));

    internal void Previous() => Run("previous", ct => _coordinator.PreviousAsync(ct));

    internal void SeekTo(long positionMs) => Run("seek", ct => _engine.SeekAsync(positionMs, ct));

    /// <param name="direction">1 fast forward, -1 rewind.</param>
    internal void Skip(int direction) => Run(direction > 0 ? "fast forward" : "rewind", async ct =>
    {
        var seconds = (await _uiSettings.GetAsync(ct).ConfigureAwait(false)).SkipSeconds;
        await _coordinator.SkipAsync(direction * (seconds > 0 ? seconds : DefaultSkipSeconds) * 1000L, ct)
            .ConfigureAwait(false);
    });

    internal void OnHeadsetButton() => _buttons.Press();

    private void OnButtonDecision(MediaButtonAction action)
    {
        switch (action)
        {
            case MediaButtonAction.TogglePlayPause:
                TogglePlayPause();
                break;
            case MediaButtonAction.Next:
                Next();
                break;
            default:
                Previous();
                break;
        }
    }

    private static void Run(string name, Func<CancellationToken, Task> command)
    {
        Log.Debug(Tag, $"Command: {name}");
        _ = Task.Run(async () =>
        {
            try
            {
                await command(CancellationToken.None).ConfigureAwait(false);
            }
            catch (System.OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                Log.Warn(Tag, $"Command {name} failed: {ex}");
            }
        });
    }

    private sealed class SessionCallback(MediaSessionBridge bridge) : MediaSession.Callback
    {
        public override void OnPlay() => bridge.Play();

        public override void OnPause() => bridge.Pause();

        public override void OnStop() => bridge.Stop();

        public override void OnSkipToNext() => bridge.Next();

        public override void OnSkipToPrevious() => bridge.Previous();

        public override void OnSeekTo(long pos) => bridge.SeekTo(pos);

        public override void OnFastForward() => bridge.Skip(1);

        public override void OnRewind() => bridge.Skip(-1);

        // The extra button of the session. Other ids are not ours.
        public override void OnCustomAction(string action, Bundle? extras)
        {
            if (action == CloseActionId)
            {
                bridge.Stop();
            }
        }

        // The headset button (and the play/pause key of some remotes) is grouped into 1, 2 and 3 presses. Every other
        // key goes to the base class, which calls the methods above.
        public override bool OnMediaButtonEvent(Intent mediaButtonIntent)
        {
            var keyEvent = ReadKeyEvent(mediaButtonIntent);
            if (keyEvent is { KeyCode: Keycode.Headsethook or Keycode.MediaPlayPause })
            {
                if (keyEvent.Action == KeyEventActions.Down && keyEvent.RepeatCount == 0)
                {
                    bridge.OnHeadsetButton();
                }

                return true;
            }

            return base.OnMediaButtonEvent(mediaButtonIntent);
        }

        private static KeyEvent? ReadKeyEvent(Intent intent)
        {
            if (OperatingSystem.IsAndroidVersionAtLeast(33))
            {
                return intent.GetParcelableExtra(Intent.ExtraKeyEvent, Java.Lang.Class.FromType(typeof(KeyEvent))) as KeyEvent;
            }

#pragma warning disable CA1422 // The typed overload exists only from API 33.
            return intent.GetParcelableExtra(Intent.ExtraKeyEvent) as KeyEvent;
#pragma warning restore CA1422
        }
    }
}
