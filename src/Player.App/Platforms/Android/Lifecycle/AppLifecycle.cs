using Player.Core.Contracts;

namespace Player.App.Platforms.Android.Lifecycle;

public sealed class AppLifecycle : IAppLifecycle
{
	private volatile bool _isForeground;

	public bool IsForeground => _isForeground;

	public event EventHandler? Foregrounded;
	public event EventHandler? Backgrounded;
	public event EventHandler? Stopping;

	internal void NotifyForegrounded()
	{
		_isForeground = true;
		Foregrounded?.Invoke(this, EventArgs.Empty);
	}

	internal void NotifyBackgrounded()
	{
		_isForeground = false;
		Backgrounded?.Invoke(this, EventArgs.Empty);
	}

	internal void NotifyStopping()
	{
		_isForeground = false;
		Stopping?.Invoke(this, EventArgs.Empty);
	}
}
