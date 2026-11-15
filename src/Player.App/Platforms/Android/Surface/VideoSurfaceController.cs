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
    private bool _immersive;
    private bool _barsVisible;
    private EdgeInsets _insets;
    private VideoFormat? _layoutFormat;   // the last known format: kept after the engine stops, so the last picture is not stretched

    public VideoSurfaceController(IVideoOutputBinding binding)
    {
        _binding = binding;
        _layoutFormat = binding.Format;
        _binding.FormatChanged += (_, format) =>
        {
            // A stop reports "no format". The surface keeps its shape until the next file reports its own,
            // otherwise the last picture would fill the whole container while the page closes.
            if (format is null)
                return;

            lock (_sync)
                _layoutFormat = format;
            Relayout();
        };
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
                _layoutFormat, container?.Width ?? 0, container?.Height ?? 0, _aspect, transform);
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

    /// <summary>
    /// Player mode: the window is laid out edge to edge (also in a display cutout) and the system bars
    /// are hidden until <see cref="SetSystemBarsVisible"/> shows them. False restores the normal window.
    /// </summary>
    public void SetImmersive(bool enabled)
    {
        lock (_sync)
        {
            _immersive = enabled;
            _barsVisible = false;
        }

        MainThread.BeginInvokeOnMainThread(ApplyImmersive);
    }

    /// <summary>The bars are drawn over the window, so showing them never resizes the picture.</summary>
    public void SetSystemBarsVisible(bool visible)
    {
        lock (_sync)
        {
            if (_barsVisible == visible)
                return;

            _barsVisible = visible;
        }

        MainThread.BeginInvokeOnMainThread(ApplyImmersive);
    }

    public EdgeInsets SystemBarInsets { get { lock (_sync) return _insets; } }

    /// <summary>Also runs when a container attaches: a recreated activity starts with visible bars.</summary>
    private void ApplyImmersive()
    {
        bool immersive, barsVisible;
        lock (_sync)
        {
            immersive = _immersive;
            barsVisible = _barsVisible;
        }

        if (Platform.CurrentActivity?.Window is not { DecorView: { } decor } window)
            return;

        // Up to API 34 the window is not edge to edge by itself: without this the content would be
        // resized every time the bars appear.
        if (!OperatingSystem.IsAndroidVersionAtLeast(35))
            AndroidX.Core.View.WindowCompat.SetDecorFitsSystemWindows(window, !immersive);

        if (AndroidX.Core.View.WindowCompat.GetInsetsController(window, decor) is not { } insets)
            return;

        if (immersive && !barsVisible)
        {
            insets.SystemBarsBehavior = AndroidX.Core.View.WindowInsetsControllerCompat.BehaviorShowTransientBarsBySwipe;
            insets.Hide(AndroidX.Core.View.WindowInsetsCompat.Type.SystemBars());
        }
        else
        {
            insets.Show(AndroidX.Core.View.WindowInsetsCompat.Type.SystemBars());
            insets.SystemBarsBehavior = AndroidX.Core.View.WindowInsetsControllerCompat.BehaviorDefault;
        }

        if (OperatingSystem.IsAndroidVersionAtLeast(28) && window.Attributes is { } attributes)
        {
            attributes.LayoutInDisplayCutoutMode = immersive
                ? Android.Views.LayoutInDisplayCutoutMode.ShortEdges
                : Android.Views.LayoutInDisplayCutoutMode.Default;
            window.Attributes = attributes;
        }

        RefreshInsets(decor);
    }

    /// <summary>
    /// Reads the space the system bars and a display cutout take when they are shown, whether they are
    /// shown now or not. Called on the UI thread, after a mode change and after every layout of the
    /// container (a rotation moves the bars).
    /// </summary>
    internal void RefreshInsets(Android.Views.View view)
    {
        var insets = default(EdgeInsets);
        bool immersive;
        lock (_sync)
            immersive = _immersive;

        if (immersive && AndroidX.Core.View.ViewCompat.GetRootWindowInsets(view) is { } root
            && root.GetInsetsIgnoringVisibility(
                AndroidX.Core.View.WindowInsetsCompat.Type.SystemBars()
                | AndroidX.Core.View.WindowInsetsCompat.Type.DisplayCutout()) is { } bars)
        {
            var density = view.Resources?.DisplayMetrics?.Density ?? 1f;
            if (density <= 0)
                density = 1f;
            insets = new EdgeInsets(bars.Left / density, bars.Top / density, bars.Right / density, bars.Bottom / density);
        }

        lock (_sync)
        {
            if (_insets == insets)
                return;

            _insets = insets;
        }

        Changed?.Invoke(this, EventArgs.Empty);
    }

    internal SurfaceRect ComputeRect(int containerWidth, int containerHeight)
    {
        lock (_sync)
            return SurfaceGeometry.Compute(_layoutFormat, containerWidth, containerHeight, _aspect, _transform);
    }

    internal void Attach(VideoSurfaceContainer container)
    {
        lock (_sync)
        {
            _container = container;

            // A page for a file whose format is not known yet must not start with the previous file's shape.
            _layoutFormat = _binding.Format;
        }

        ApplyImmersive();
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
