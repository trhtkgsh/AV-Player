using Player.Core.Logic.Surface;
using Player.Core.Models;

namespace Player.Core.Tests.Surface;

public sealed class SurfaceGeometryTests
{
    private const double ContainerWidth = 1080;
    private const double ContainerHeight = 2208;

    private static readonly VideoFormat Landscape = new(1920, 1080, 1, 1);
    private static readonly VideoFormat Portrait = new(1080, 1920, 1, 1);

    private static void AssertRect(
        double left, double top, double width, double height, SurfaceRect actual)
    {
        Assert.Equal(left, actual.Left, 3);
        Assert.Equal(top, actual.Top, 3);
        Assert.Equal(width, actual.Width, 3);
        Assert.Equal(height, actual.Height, 3);
    }

    private static SurfaceRect ComputeLandscape(AspectMode mode, ViewTransform transform) =>
        SurfaceGeometry.Compute(Landscape, ContainerWidth, ContainerHeight, mode, transform);

    private static ViewTransform ClampLandscapeFit(ViewTransform transform) =>
        SurfaceGeometry.Clamp(Landscape, ContainerWidth, ContainerHeight, AspectMode.Fit, transform);

    [Fact]
    public void Compute_FitLandscapeInPortraitContainer_CentersVertically()
    {
        var rect = ComputeLandscape(AspectMode.Fit, ViewTransform.Identity);

        AssertRect(0, 800.25, 1080, 607.5, rect);
    }

    [Fact]
    public void Compute_FitPortraitInLandscapeContainer_CentersHorizontally()
    {
        var rect = SurfaceGeometry.Compute(Portrait, 1920, 1080, AspectMode.Fit, ViewTransform.Identity);

        AssertRect(656.25, 0, 607.5, 1080, rect);
    }

    [Fact]
    public void Compute_CropLandscapeInPortraitContainer_FillsHeightAndOverflowsWidth()
    {
        var rect = ComputeLandscape(AspectMode.Crop, ViewTransform.Identity);

        AssertRect(-1422.667, 0, 3925.333, 2208, rect);
    }

    [Theory]
    [InlineData(1920, 1080)]
    [InlineData(1080, 1920)]
    [InlineData(320, 240)]
    public void Compute_Stretch_FillsContainerForAnyVideo(int width, int height)
    {
        var rect = SurfaceGeometry.Compute(
            new VideoFormat(width, height, 1, 1), ContainerWidth, ContainerHeight, AspectMode.Stretch,
            ViewTransform.Identity);

        AssertRect(0, 0, 1080, 2208, rect);
    }

    [Fact]
    public void Compute_OriginalLandscape_UsesVideoSizeCentered()
    {
        var rect = ComputeLandscape(AspectMode.Original, ViewTransform.Identity);

        AssertRect(-420, 564, 1920, 1080, rect);
    }

    [Fact]
    public void Compute_NonSquareSampleAspectRatio_WidensDisplaySize()
    {
        var rect = SurfaceGeometry.Compute(
            new VideoFormat(720, 576, 16, 15), 768, 576, AspectMode.Fit, ViewTransform.Identity);

        AssertRect(0, 0, 768, 576, rect);
    }

    [Theory]
    [InlineData(AspectMode.Fit)]
    [InlineData(AspectMode.Stretch)]
    [InlineData(AspectMode.Crop)]
    [InlineData(AspectMode.Original)]
    public void Compute_NullFormat_ReturnsContainerRect(AspectMode mode)
    {
        var rect = SurfaceGeometry.Compute(null, ContainerWidth, ContainerHeight, mode, ViewTransform.Identity);

        AssertRect(0, 0, 1080, 2208, rect);
    }

    [Theory]
    [InlineData(0, 2208)]
    [InlineData(1080, 0)]
    [InlineData(0, 0)]
    public void Compute_ZeroContainerSide_ReturnsDefaultRect(double width, double height)
    {
        var rect = SurfaceGeometry.Compute(Landscape, width, height, AspectMode.Fit, ViewTransform.Identity);

        Assert.Equal(default(SurfaceRect), rect);
    }

    [Fact]
    public void Compute_ZoomTwo_DoublesSurfaceSizeAroundCenter()
    {
        var rect = ComputeLandscape(AspectMode.Fit, new ViewTransform(2, 0, 0));

        AssertRect(-540, 496.5, 2160, 1215, rect);
    }

    [Theory]
    [InlineData(0.5, 1)]
    [InlineData(9, 4)]
    [InlineData(double.NaN, 1)]
    [InlineData(2.5, 2.5)]
    public void Clamp_Scale_IsLimitedToOneThroughFour(double scale, double expected)
    {
        var result = ClampLandscapeFit(new ViewTransform(scale, 0, 0));

        Assert.Equal(expected, result.Scale, 3);
    }

    [Fact]
    public void Clamp_PictureNotLargerThanContainer_ForcesPanToZero()
    {
        var result = ClampLandscapeFit(new ViewTransform(1, 0.3, 0.3));

        Assert.Equal(0, result.PanX, 3);
        Assert.Equal(0, result.PanY, 3);
    }

    [Theory]
    [InlineData(0.9, 0.5)]
    [InlineData(-0.9, -0.5)]
    [InlineData(0.2, 0.2)]
    public void Clamp_PanXBeyondLimit_IsLimitedToHalfOfOverflow(double panX, double expected)
    {
        var result = ClampLandscapeFit(new ViewTransform(2, panX, 0));

        Assert.Equal(expected, result.PanX, 3);
    }

    [Fact]
    public void Compute_ZoomTwoAtMaxPositivePan_AlignsLeftEdgeToContainer()
    {
        var rect = ComputeLandscape(AspectMode.Fit, new ViewTransform(2, 0.5, 0));

        Assert.Equal(0, rect.Left, 3);
    }

    [Fact]
    public void Compute_ZoomTwoAtMaxNegativePan_AlignsRightEdgeToContainer()
    {
        var rect = ComputeLandscape(AspectMode.Fit, new ViewTransform(2, -0.5, 0));

        Assert.Equal(1080, rect.Left + rect.Width, 3);
    }

    [Fact]
    public void Clamp_PanYWhenPictureShorterThanContainer_ForcesPanYToZero()
    {
        var result = ClampLandscapeFit(new ViewTransform(2, 0, 0.4));

        Assert.Equal(0, result.PanY, 3);
    }

    [Fact]
    public void Compute_PanBeyondLimit_EqualsComputeAtLimit()
    {
        var beyond = ComputeLandscape(AspectMode.Fit, new ViewTransform(2, 0.9, 0));
        var atLimit = ComputeLandscape(AspectMode.Fit, new ViewTransform(2, 0.5, 0));

        Assert.Equal(atLimit, beyond);
    }

    [Fact]
    public void Compute_NullTransform_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            SurfaceGeometry.Compute(Landscape, ContainerWidth, ContainerHeight, AspectMode.Fit, null!));
    }

    [Fact]
    public void Clamp_NullTransform_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            SurfaceGeometry.Clamp(Landscape, ContainerWidth, ContainerHeight, AspectMode.Fit, null!));
    }
}
