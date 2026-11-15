using Android.Content;
using Android.Util;

namespace Player.App.Pip;

/// <summary>
/// Receives the three PiP actions. It is registered in code while PiP is active and never declared in the manifest.
/// The work leaves the receiver's thread at once.
/// </summary>
internal sealed class PipActionReceiver : BroadcastReceiver
{
    private const string Tag = "ArielPip";

    private readonly Func<string?, Task> _handler;

    public PipActionReceiver(Func<string?, Task> handler)
    {
        _handler = handler ?? throw new ArgumentNullException(nameof(handler));
    }

    public override void OnReceive(Context? context, Intent? intent)
    {
        try
        {
            var action = intent?.Action;
            _ = Task.Run(() => _handler(action));
        }
        catch (Exception ex)
        {
            Log.Warn(Tag, $"Handling a Picture-in-Picture action failed: {ex}");
        }
    }
}
