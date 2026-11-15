using Player.Core.Contracts;

namespace Player.Core.Tests.StateRecording;

/// <summary>An <see cref="IAppLifecycle"/> whose events a test raises by hand.</summary>
internal sealed class FakeAppLifecycle : IAppLifecycle
{
    private readonly object _lock = new();
    private EventHandler? _foregrounded;
    private EventHandler? _backgrounded;
    private EventHandler? _stopping;
    private int _foregroundedCount;
    private int _backgroundedCount;
    private int _stoppingCount;

    public bool IsForeground { get; set; } = true;

    public int ForegroundedSubscribers => Read(ref _foregroundedCount);
    public int BackgroundedSubscribers => Read(ref _backgroundedCount);
    public int StoppingSubscribers => Read(ref _stoppingCount);

    public event EventHandler? Foregrounded
    {
        add => Change(ref _foregrounded, ref _foregroundedCount, value, 1);
        remove => Change(ref _foregrounded, ref _foregroundedCount, value, -1);
    }

    public event EventHandler? Backgrounded
    {
        add => Change(ref _backgrounded, ref _backgroundedCount, value, 1);
        remove => Change(ref _backgrounded, ref _backgroundedCount, value, -1);
    }

    public event EventHandler? Stopping
    {
        add => Change(ref _stopping, ref _stoppingCount, value, 1);
        remove => Change(ref _stopping, ref _stoppingCount, value, -1);
    }

    public void RaiseForegrounded()
    {
        IsForeground = true;
        Invoke(ref _foregrounded);
    }

    public void RaiseBackgrounded()
    {
        IsForeground = false;
        Invoke(ref _backgrounded);
    }

    public void RaiseStopping() => Invoke(ref _stopping);

    private int Read(ref int count)
    {
        lock (_lock)
        {
            return count;
        }
    }

    private void Change(ref EventHandler? field, ref int count, EventHandler? handler, int delta)
    {
        lock (_lock)
        {
            field = delta > 0 ? field + handler : field - handler;
            count += delta;
        }
    }

    private void Invoke(ref EventHandler? field)
    {
        EventHandler? handlers;
        lock (_lock)
        {
            handlers = field;
        }

        handlers?.Invoke(this, EventArgs.Empty);
    }
}
