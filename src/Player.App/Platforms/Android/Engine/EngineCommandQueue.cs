using System.Collections.Concurrent;

namespace Player.App.Engine;

/// <summary>
/// One dedicated thread that runs engine commands in order. All LibVLC calls happen here,
/// never on the UI thread and never inside a LibVLC callback (docs/SPIKE.md findings 1 and 2).
/// </summary>
internal sealed class EngineCommandQueue : IDisposable
{
    private readonly BlockingCollection<Action> _commands = [];

    public EngineCommandQueue(string name)
    {
        new Thread(Run) { IsBackground = true, Name = name }.Start();
    }

    /// <summary>Fire and forget. Returns false when the queue is shut down.</summary>
    public bool Post(Action command)
    {
        try
        {
            _commands.Add(command);
            return true;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    /// <summary>The task completes when the command ran, faults when it threw.</summary>
    public Task Enqueue(Action command, CancellationToken ct)
    {
        if (ct.IsCancellationRequested)
            return Task.FromCanceled(ct);

        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var posted = Post(() =>
        {
            if (ct.IsCancellationRequested)
            {
                completion.TrySetCanceled(ct);
                return;
            }

            try
            {
                command();
                completion.TrySetResult();
            }
            catch (Exception ex)
            {
                completion.TrySetException(ex);
            }
        });

        if (!posted)
            completion.TrySetException(new ObjectDisposedException(nameof(EngineCommandQueue)));
        return completion.Task;
    }

    public void Dispose() => _commands.CompleteAdding();

    private void Run()
    {
        foreach (var command in _commands.GetConsumingEnumerable())
        {
            try
            {
                command();
            }
            catch (Exception ex)
            {
                // A failing event subscriber must not stop the engine thread.
                global::Android.Util.Log.Error(VlcPlayerEngine.Tag, $"Engine command failed: {ex}");
            }
        }
    }
}
