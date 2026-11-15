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
    private SavedBars? _savedBars;        // UI thread only: how the bars looked before player mode
    private bool _edgeToEdge;             // UI thread only: this class made the window edge to edge (up to API 34)
    private int _topInsetPx;              // the height of the status bar while in player mode, else 0
    private VideoFormat? _layoutFormat;   // the last known format: kept after the engine stops, so the last picture is not stretched

    private readonly record struct SavedBars(int StatusColor, int NavigationColor, bool LightStatus, bool LightNavigation);

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
            var height = container?.Height ?? 0;
            _transform = SurfaceGeometry.Clamp(
                _layoutFormat, container?.Width ?? 0, height - Math.Clamp(_topInsetPx, 0, height / 2), _aspect, transform);
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
        var insetsCleared = false;
        lock (_sync)
        {
            _immersive = enabled;
            _barsVisible = false;

            // Outside player mode there are no insets to keep clear of, whether or not a window
            // exists to read them from.
            if (!enabled && _insets != default)
            {
                _insets = default;
                insetsCleared = true;
            }

            if (!enabled)
                _topInsetPx = 0;
        }

        if (insetsCleared)
            Changed?.Invoke(this, EventArgs.Empty);

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
        {
            AndroidX.Core.View.WindowCompat.SetDecorFitsSystemWindows(window, !immersive);
            if (_edgeToEdge && !immersive)
                ClearBarPaddingOfContent(decor);
            _edgeToEdge = immersive;
        }

        if (AndroidX.Core.View.WindowCompat.GetInsetsController(window, decor) is not { } insets)
            return;

        ApplyBarLook(window, insets, immersive);

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
    /// The pages under the player are padded by MAUI for the bars while the window is edge to edge.
    /// Once the window fits the bars again, Android uses up the bar insets at the top of the view tree
    /// and does not pass them on (a used-up dispatch stops there), so MAUI never hears that the bars
    /// take no space inside the content any more: the padding would stay, an empty band above the
    /// navigation bar. The content is told directly, now and once more after the next layout.
    /// </summary>
    private void ClearBarPaddingOfContent(Android.Views.View decor)
    {
        DispatchInsetsInsideBars(decor);
        if (decor.ViewTreeObserver is { IsAlive: true } observer)
            observer.AddOnGlobalLayoutListener(new InsetsAfterLayout(this, decor));
    }

    private static void DispatchInsetsInsideBars(Android.Views.View decor)
    {
        if (decor.FindViewById(Android.Resource.Id.Content) is not { } content
            || AndroidX.Core.View.ViewCompat.GetRootWindowInsets(decor) is not { } root)
            return;

        // Zero, but not marked as used up: a used-up dispatch would not reach the children.
        var none = AndroidX.Core.Graphics.Insets.None;
        var builder = new AndroidX.Core.View.WindowInsetsCompat.Builder(root);
        builder.SetInsets(AndroidX.Core.View.WindowInsetsCompat.Type.SystemBars(), none);
        builder.SetInsets(AndroidX.Core.View.WindowInsetsCompat.Type.DisplayCutout(), none);
        AndroidX.Core.View.ViewCompat.DispatchApplyWindowInsets(content, builder.Build());
    }

    private sealed class InsetsAfterLayout(VideoSurfaceController owner, Android.Views.View decor)
        : Java.Lang.Object, Android.Views.ViewTreeObserver.IOnGlobalLayoutListener
    {
        public void OnGlobalLayout()
        {
            if (decor.ViewTreeObserver is { IsAlive: true } observer)
                observer.RemoveOnGlobalLayoutListener(this);

            // Not when player mode has started again in the meantime: the window is edge to edge then.
            if (!owner._edgeToEdge)
                DispatchInsetsInsideBars(decor);
        }
    }

    /// <summary>
    /// In player mode the bars lie over the picture: they are transparent with white symbols, in every
    /// theme. Up to API 34 the window paints the bars in the theme's color, which would cover the edges
    /// of the picture. The look the theme had set is kept and put back when player mode ends.
    /// </summary>
    private void ApplyBarLook(
        Android.Views.Window window, AndroidX.Core.View.WindowInsetsControllerCompat insets, bool immersive)
    {
        var paintsBars = !OperatingSystem.IsAndroidVersionAtLeast(35);
        if (immersive)
        {
#pragma warning disable CA1422 // Bar colors are obsolete from API 35 only; guarded by paintsBars.
            _savedBars ??= new SavedBars(
                paintsBars ? window.StatusBarColor : 0,
                paintsBars ? window.NavigationBarColor : 0,
                insets.AppearanceLightStatusBars,
                insets.AppearanceLightNavigationBars);

            // Set on every call: the activity puts the theme's look back when it is created or resumed.
            // WindowFocused applies player mode again after that.
            if (paintsBars)
            {
                window.SetStatusBarColor(Android.Graphics.Color.Transparent);
                window.SetNavigationBarColor(Android.Graphics.Color.Transparent);
            }

            insets.AppearanceLightStatusBars = false;
            insets.AppearanceLightNavigationBars = false;
        }
        else if (_savedBars is { } saved)
        {
            _savedBars = null;
            if (paintsBars)
            {
                window.SetStatusBarColor(new Android.Graphics.Color(saved.StatusColor));
                window.SetNavigationBarColor(new Android.Graphics.Color(saved.NavigationColor));
            }
#pragma warning restore CA1422

            insets.AppearanceLightStatusBars = saved.LightStatus;
            insets.AppearanceLightNavigationBars = saved.LightNavigation;
        }
    }

    /// <summary>
    /// Reads the space the system bars and a display cutout take when they are shown, whether they are
    /// shown now or not. Called on the UI thread, after a mode change and after every layout of the
    /// container (a rotation moves the bars).
    /// </summary>
    internal void RefreshInsets(Android.Views.View view)
    {
        // A posted call can arrive after the view left its window: it has no insets to report then.
        if (!view.IsAttachedToWindow)
            return;

        var insets = default(EdgeInsets);
        var topPx = 0;
        bool immersive;
        lock (_sync)
            immersive = _immersive;

        var root = immersive ? AndroidX.Core.View.ViewCompat.GetRootWindowInsets(view) : null;
        if (root?.GetInsetsIgnoringVisibility(
                AndroidX.Core.View.WindowInsetsCompat.Type.SystemBars()
                | AndroidX.Core.View.WindowInsetsCompat.Type.DisplayCutout()) is { } bars)
        {
            var density = view.Resources?.DisplayMetrics?.Density ?? 1f;
            if (density <= 0)
                density = 1f;
            insets = new EdgeInsets(bars.Left / density, bars.Top / density, bars.Right / density, bars.Bottom / density);
        }

        // The small Picture-in-Picture window has no status bar of its own.
        if (Platform.CurrentActivity is not { IsInPictureInPictureMode: true }
            && root?.GetInsetsIgnoringVisibility(AndroidX.Core.View.WindowInsetsCompat.Type.StatusBars()) is { } status)
            topPx = status.Top;

        bool insetsChanged, topChanged;
        lock (_sync)
        {
            insetsChanged = _insets != insets;
            topChanged = _topInsetPx != topPx;
            _insets = insets;
            _topInsetPx = topPx;
        }

        if (topChanged)
            Relayout();
        if (insetsChanged)
            Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// The picture is laid out below the status bar: it may reach the bar but never lie under it, whether
    /// the bar is shown or hidden (so it does not move when the bar appears).
    /// </summary>
    internal SurfaceRect ComputeRect(int containerWidth, int containerHeight)
    {
        lock (_sync)
        {
            var top = Math.Clamp(_topInsetPx, 0, containerHeight / 2);
            var rect = SurfaceGeometry.Compute(_layoutFormat, containerWidth, containerHeight - top, _aspect, _transform);
            return top == 0 ? rect : rect with { Top = rect.Top + top };
        }
    }

    /// <summary>The height of the strip at the top of the container that the picture keeps clear of.</summary>
    internal int TopInsetPx { get { lock (_sync) return _topInsetPx; } }

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

    /// <summary>
    /// The window of the attached container has the focus again (the app came back to the foreground,
    /// or a dialog closed). The activity has put the theme's bar look back by then.
    /// </summary>
    internal void WindowFocused(VideoSurfaceContainer container)
    {
        lock (_sync)
        {
            if (!ReferenceEquals(_container, container))
                return;
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
