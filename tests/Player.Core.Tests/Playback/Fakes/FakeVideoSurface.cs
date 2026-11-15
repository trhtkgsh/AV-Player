using Player.Core.Contracts;
using Player.Core.Models;

namespace Player.Core.Tests.Playback.Fakes;

/// <summary>Records every <see cref="SetAspect"/> call. Can be told to throw. Can also write into a shared call log.</summary>
internal sealed class FakeVideoSurface : IVideoSurface
{
    private readonly object _lock = new();
    private readonly List<AspectMode> _aspects = new();
    private readonly Action<string>? _log;

    public FakeVideoSurface(Action<string>? log = null)
    {
        _log = log;
    }

    public Exception? SetAspectException { get; set; }

    public IReadOnlyList<AspectMode> AspectCalls
    {
        get
        {
            lock (_lock)
            {
                return _aspects.ToArray();
            }
        }
    }

    public AspectMode Aspect { get; private set; } = AspectMode.Fit;
    public ViewTransform Transform { get; private set; } = ViewTransform.Identity;
    public double WindowBrightness => -1;

    public event EventHandler? Changed
    {
        add { }
        remove { }
    }

    public void SetAspect(AspectMode mode)
    {
        lock (_lock)
        {
            _aspects.Add(mode);
        }

        _log?.Invoke($"SetAspect:{mode}");
        if (SetAspectException is { } ex)
        {
            throw ex;
        }

        Aspect = mode;
    }

    public void SetImmersive(bool enabled)
    {
    }

    public void SetTransform(ViewTransform transform) => Transform = transform;

    public void SetWindowBrightness(double? value)
    {
    }
}
