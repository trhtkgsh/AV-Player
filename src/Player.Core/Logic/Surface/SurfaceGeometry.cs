using Player.Core.Models;

namespace Player.Core.Logic.Surface;

/// <summary>Position and size of the video surface inside its container, in pixels.</summary>
public readonly record struct SurfaceRect(double Left, double Top, double Width, double Height);

/// <summary>
/// Where the video surface goes for a given video, container, aspect mode and zoom/pan.
/// Zoom changes the surface size itself, so the frame is rendered at the zoomed resolution
/// instead of being upscaled (docs/SPIKE.md finding 8).
/// </summary>
public static class SurfaceGeometry
{
    public const double MinScale = 1.0;
    public const double MaxScale = 4.0;

    public static SurfaceRect Compute(
        VideoFormat? format, double containerWidth, double containerHeight, AspectMode mode, ViewTransform transform)
    {
        ArgumentNullException.ThrowIfNull(transform);
        if (containerWidth <= 0 || containerHeight <= 0)
            return default;

        var (baseWidth, baseHeight) = BaseSize(format, containerWidth, containerHeight, mode);
        var clamped = Clamp(format, containerWidth, containerHeight, mode, transform);
        var width = baseWidth * clamped.Scale;
        var height = baseHeight * clamped.Scale;

        return new SurfaceRect(
            (containerWidth - width) / 2 + clamped.PanX * containerWidth,
            (containerHeight - height) / 2 + clamped.PanY * containerHeight,
            width,
            height);
    }

    /// <summary>
    /// Limits the scale to 100% .. 400% and the pan so that a picture larger than the container
    /// always covers it. An axis on which the picture is not larger than the container cannot be panned.
    /// </summary>
    public static ViewTransform Clamp(
        VideoFormat? format, double containerWidth, double containerHeight, AspectMode mode, ViewTransform transform)
    {
        ArgumentNullException.ThrowIfNull(transform);
        var scale = double.IsNaN(transform.Scale) ? MinScale : Math.Clamp(transform.Scale, MinScale, MaxScale);
        if (containerWidth <= 0 || containerHeight <= 0)
            return new ViewTransform(scale, 0, 0);

        var (baseWidth, baseHeight) = BaseSize(format, containerWidth, containerHeight, mode);
        return new ViewTransform(
            scale,
            ClampPan(transform.PanX, baseWidth * scale, containerWidth),
            ClampPan(transform.PanY, baseHeight * scale, containerHeight));
    }

    private static double ClampPan(double pan, double size, double container)
    {
        var limit = Math.Max(0, (size - container) / 2 / container);
        return double.IsNaN(pan) ? 0 : Math.Clamp(pan, -limit, limit);
    }

    private static (double Width, double Height) BaseSize(
        VideoFormat? format, double containerWidth, double containerHeight, AspectMode mode)
    {
        if (format is null || format.Width <= 0 || format.Height <= 0 || mode == AspectMode.Stretch)
            return (containerWidth, containerHeight);

        // Display size of the video in square pixels.
        var sar = format.SarNumerator > 0 && format.SarDenominator > 0
            ? (double)format.SarNumerator / format.SarDenominator
            : 1.0;
        var videoWidth = format.Width * sar;
        var videoHeight = (double)format.Height;

        var factor = mode switch
        {
            AspectMode.Fit => Math.Min(containerWidth / videoWidth, containerHeight / videoHeight),
            AspectMode.Crop => Math.Max(containerWidth / videoWidth, containerHeight / videoHeight),
            AspectMode.Original => 1.0,
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null),
        };

        return (videoWidth * factor, videoHeight * factor);
    }
}
