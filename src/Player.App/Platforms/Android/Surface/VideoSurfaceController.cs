using Player.Core.Contracts;
using Player.Core.Logic.Surface;
using Player.Core.Models;

namespace Player.App.Surface;

/// <summary>
/// Aspect mode, zoom/pan and window brightness of the video view. A singleton that outlives the
/// player page: the native container attaches while it is on screen.
/// </summary>
internal sealed class VideoSurfaceController : IVideoSurface
{
    private const double SystemBrightness = -1;

    private readonly IVideoOutputBinding _binding;
    private readonly object _sync = new();
    private VideoSurfaceContainer? _container;
    private AspectMode _aspect = AspectMode.Fit;
    private ViewTransform _transform = ViewTransform.Identity;
    private double _brightness = SystemBrightness;

    public VideoSurfaceController(IVideoOutputBinding binding)
    {
        _binding = binding;
        _binding.FormatChanged += (_, _) => Relayout();
    }

    public AspectMode Aspect { get { lock (_sync) return _aspect; } }
    public ViewTransform Transform { get { lock (_sync) return _transform; } }
    public double WindowBrightness { get { lock (_sync) return _brightness; } }

    public event EventHandler? Changed;

    public void SetAspect(AspectMode mode)
    {
        lock (_sync)
        {
            _aspect = mode;
            _transform = ViewTransform.Identity;
        }

        Relayout();
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void SetTransform(ViewTransform transform)
    {
        ArgumentNullException.ThrowIfNull(transform);
        lock (_sync)
        {
            var container = _container;
            _transform = SurfaceGeometry.Clamp(
                _binding.Format, container?.Width ?? 0, container?.Height ?? 0, _aspect, transform);
        }

        Relayout();
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void SetWindowBrightness(double? value)
    {
        var brightness = value is { } v && !double.IsNaN(v) ? Math.Clamp(v, 0.01, 1.0) : SystemBrightness;
        lock (_sync)
            _brightness = brightness;

        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (Platform.CurrentActivity?.Window is not { Attributes: { } attributes } window)
                return;

            attributes.ScreenBrightness = (float)brightness;
            window.Attributes = attributes;
        });
        Changed?.Invoke(this, EventArgs.Empty);
    }

    internal SurfaceRect ComputeRect(int containerWidth, int containerHeight)
    {
        lock (_sync)
            return SurfaceGeometry.Compute(_binding.Format, containerWidth, containerHeight, _aspect, _transform);
    }

    internal void Attach(VideoSurfaceContainer container)
    {
        lock (_sync)
            _container = container;
    }

    // The three surface reports below run on the UI thread (surface holder callbacks and the handler).
    // The engine has one surface. Only the view that owns it may change it or report it lost: a page
    // that is torn down late must not detach the surface of the page that replaced it.
    private VideoSurfaceContainer? _surfaceOwner;

    internal void SurfaceReady(VideoSurfaceContainer owner, object nativeSurface, int widthPx, int heightPx)
    {
        _surfaceOwner = owner;
        _binding.SurfaceReady(nativeSurface, widthPx, heightPx);
    }

    internal void SurfaceSizeChanged(VideoSurfaceContainer owner, int widthPx, int heightPx)
    {
        if (ReferenceEquals(_surfaceOwner, owner))
            _binding.SurfaceSizeChanged(widthPx, heightPx);
    }

    internal void SurfaceLost(VideoSurfaceContainer owner)
    {
        if (!ReferenceEquals(_surfaceOwner, owner))
            return;

        _surfaceOwner = null;
        _binding.SurfaceLost();
    }

    internal void Detach(VideoSurfaceContainer container)
    {
        lock (_sync)
        {
            if (ReferenceEquals(_container, container))
                _container = null;
        }
    }

    /// <summary>Safe from any thread: the engine raises FormatChanged on its own thread.</summary>
    private void Relayout()
    {
        VideoSurfaceContainer? container;
        lock (_sync)
            container = _container;

        container?.Post(container.RequestLayout);
    }
}
