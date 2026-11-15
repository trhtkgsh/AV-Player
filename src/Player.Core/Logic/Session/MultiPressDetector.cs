namespace Player.Core.Logic.Session;

/// <summary>What a run of headset button presses means.</summary>
public enum MediaButtonAction { TogglePlayPause, Next, Previous }

/// <summary>
/// Groups presses of the headset button: 1 press is a toggle, 2 are next, 3 are previous. A group ends when
/// <see cref="Window"/> passes after the last press, or at once on the third press. The decision is reported
/// through the callback, on the timer's thread or on the caller of <see cref="Press"/>, never under a lock.
/// </summary>
public sealed class MultiPressDetector : IDisposable
{
    public static readonly TimeSpan DefaultWindow = TimeSpan.FromMilliseconds(400);
    public const int MaxPresses = 3;

    private readonly Action<MediaButtonAction> _decided;
    private readonly TimeProvider _time;
    private readonly object _gate = new();
    private ITimer? _timer;
    private int _count;
    private long _generation;
    private bool _disposed;

    public MultiPressDetector(TimeProvider time, Action<MediaButtonAction> decided, TimeSpan? window = null)
    {
        ArgumentNullException.ThrowIfNull(time);
        ArgumentNullException.ThrowIfNull(decided);
        Window = window ?? DefaultWindow;
        _time = time;
        _decided = decided;
    }

    /// <summary>The longest gap between two presses of one group.</summary>
    public TimeSpan Window { get; }

    /// <summary>Registers one press.</summary>
    public void Press()
    {
        MediaButtonAction? decision = null;
        lock (_gate)
        {
            if (_disposed)
            {
                return;
            }

            _generation++;
            _count++;
            DisarmLocked();
            if (_count >= MaxPresses)
            {
                _count = 0;
                decision = Map(MaxPresses);
            }
            else
            {
                // Every arming is its own timer and carries its generation: a firing of an older arming is stale.
                _timer = _time.CreateTimer(OnTimer, _generation, Window, Timeout.InfiniteTimeSpan);
            }
        }

        if (decision is { } action)
        {
            _decided(action);
        }
    }

    public void Dispose()
    {
        lock (_gate)
        {
            _disposed = true;
            _count = 0;
            DisarmLocked();
        }
    }

    private void DisarmLocked()
    {
        _timer?.Dispose();
        _timer = null;
    }

    private void OnTimer(object? state)
    {
        MediaButtonAction? decision = null;
        lock (_gate)
        {
            // A firing of an older arming (a press came after it) is stale; the newer arming fires on its own.
            if (_disposed || _count == 0 || state is not long armed || armed != _generation)
            {
                return;
            }

            decision = Map(_count);
            _count = 0;
            DisarmLocked();
        }

        if (decision is { } action)
        {
            _decided(action);
        }
    }

    private static MediaButtonAction Map(int presses) => presses switch
    {
        1 => MediaButtonAction.TogglePlayPause,
        2 => MediaButtonAction.Next,
        _ => MediaButtonAction.Previous,
    };
}
