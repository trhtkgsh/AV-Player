using Android.Content;
using AndroidX.Activity;
using AndroidX.Activity.Result;
using AndroidX.Activity.Result.Contract;
using AndroidX.Lifecycle;

namespace Player.App.FileOps;

/// <summary>
/// Shows a system consent dialog (an IntentSender) through the current activity's ActivityResultRegistry,
/// so MainActivity needs no override. One dialog at a time.
/// </summary>
public sealed class ConsentLauncher
{
	private readonly SemaphoreSlim _gate = new(1, 1);

	public async Task<ConsentOutcome> AskAsync(IntentSender sender, CancellationToken ct)
	{
		ArgumentNullException.ThrowIfNull(sender);
		try
		{
			await _gate.WaitAsync(ct).ConfigureAwait(false);
		}
		catch (OperationCanceledException)
		{
			return ConsentOutcome.Declined;
		}

		try
		{
			return await MainThread.InvokeOnMainThreadAsync(() => LaunchAsync(sender, ct)).ConfigureAwait(false);
		}
		finally
		{
			_gate.Release();
		}
	}

	private static async Task<ConsentOutcome> LaunchAsync(IntentSender sender, CancellationToken ct)
	{
		if (Platform.CurrentActivity is not ComponentActivity activity || activity.IsFinishing || activity.IsDestroyed)
		{
			return ConsentOutcome.Unavailable;
		}

		var done = new TaskCompletionSource<ConsentOutcome>(TaskCreationOptions.RunContinuationsAsynchronously);
		var key = "fileops-consent-" + Guid.NewGuid().ToString("N");
		using var callback = new ResultCallback(done);
		using var contract = new ActivityResultContracts.StartIntentSenderForResult();
		var launcher = activity.ActivityResultRegistry.Register(key, contract, callback);
		using var watcher = new DestroyWatcher(done);
		activity.Lifecycle.AddObserver(watcher);
		using var cancellation = ct.Register(() => done.TrySetResult(ConsentOutcome.Declined));
		try
		{
			using var request = new IntentSenderRequest.Builder(sender).Build();
			launcher.Launch(request);
			return await done.Task.ConfigureAwait(true);
		}
		finally
		{
			launcher.Unregister();
			activity.Lifecycle.RemoveObserver(watcher);
		}
	}

	private sealed class ResultCallback(TaskCompletionSource<ConsentOutcome> done) : Java.Lang.Object, IActivityResultCallback
	{
		public void OnActivityResult(Java.Lang.Object? result)
		{
			var granted = result is ActivityResult { ResultCode: (int)global::Android.App.Result.Ok };
			done.TrySetResult(granted ? ConsentOutcome.Granted : ConsentOutcome.Declined);
		}
	}

	private sealed class DestroyWatcher(TaskCompletionSource<ConsentOutcome> done) : Java.Lang.Object, ILifecycleEventObserver
	{
		public void OnStateChanged(ILifecycleOwner source, Lifecycle.Event e)
		{
			if (e == Lifecycle.Event.OnDestroy)
			{
				done.TrySetResult(ConsentOutcome.Declined);
			}
		}
	}
}
