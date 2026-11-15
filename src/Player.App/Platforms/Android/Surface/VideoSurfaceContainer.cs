using Android.Content;
using Android.Graphics;
using Android.Views;
using AndroidColor = Android.Graphics.Color;
using AndroidView = Android.Views.View;

namespace Player.App.Surface;

/// <summary>
/// Clipping container with one native SurfaceView. The container fills the space the page gives it,
/// the surface is laid out at the rectangle the controller computes (aspect mode, zoom, pan).
/// It reports the surface lifecycle to the engine and knows nothing else about it.
/// </summary>
internal sealed class VideoSurfaceContainer : ViewGroup, ISurfaceHolderCallback
{
    private readonly VideoSurfaceController _controller;
    private readonly SurfaceView _surface;
    private bool _surfaceReported;
    private int _reportedWidth;
    private int _reportedHeight;

    public VideoSurfaceContainer(Context context, VideoSurfaceController controller)
        : base(context)
    {
        _controller = controller;

        SetBackgroundColor(AndroidColor.Black);
        SetClipChildren(true);

        _surface = new SurfaceView(context);
        _surface.Holder!.AddCallback(this);
        AddView(_surface);
    }

    public void SurfaceCreated(ISurfaceHolder holder)
    {
        // Reported from SurfaceChanged, when the size is known.
    }

    public void SurfaceChanged(ISurfaceHolder holder, Format format, int width, int height)
    {
        if (holder.Surface is not { IsValid: true } surface)
            return;

        if (!_surfaceReported)
        {
            _surfaceReported = true;
            _controller.SurfaceReady(this, surface, width, height);
        }
        else if (width != _reportedWidth || height != _reportedHeight)
        {
            _controller.SurfaceSizeChanged(this, width, height);
        }

        _reportedWidth = width;
        _reportedHeight = height;
    }

    public void SurfaceDestroyed(ISurfaceHolder holder) => ReportLost();

    /// <summary>
    /// Called when the MAUI handler lets go of this view. The engine must never be left attached to a
    /// surface that nobody reports on any more.
    /// </summary>
    internal void Release()
    {
        _surface.Holder?.RemoveCallback(this);
        ReportLost();
    }

    private void ReportLost()
    {
        if (!_surfaceReported)
            return;

        _surfaceReported = false;
        _controller.SurfaceLost(this);
    }

    protected override void OnMeasure(int widthMeasureSpec, int heightMeasureSpec)
    {
        var width = AndroidView.GetDefaultSize(SuggestedMinimumWidth, widthMeasureSpec);
        var height = AndroidView.GetDefaultSize(SuggestedMinimumHeight, heightMeasureSpec);
        SetMeasuredDimension(width, height);

        var rect = _controller.ComputeRect(width, height);
        _surface.Measure(
            MeasureSpec.MakeMeasureSpec(Pixels(rect.Width), MeasureSpecMode.Exactly),
            MeasureSpec.MakeMeasureSpec(Pixels(rect.Height), MeasureSpecMode.Exactly));
    }

    protected override void OnLayout(bool changed, int left, int top, int right, int bottom)
    {
        var rect = _controller.ComputeRect(right - left, bottom - top);
        var surfaceLeft = (int)Math.Round(rect.Left);
        var surfaceTop = (int)Math.Round(rect.Top);
        _surface.Layout(surfaceLeft, surfaceTop, surfaceLeft + Pixels(rect.Width), surfaceTop + Pixels(rect.Height));

        // A rotation moves the system bars. Posted: subscribers may change their own layout.
        Post(() => _controller.RefreshInsets(this));
    }

    private static int Pixels(double value) => Math.Max(1, (int)Math.Round(value));
}
