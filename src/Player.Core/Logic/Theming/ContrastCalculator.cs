namespace Player.Core.Logic.Theming;

/// <summary>WCAG 2.x relative luminance and contrast ratio. The alpha channel is ignored.</summary>
public static class ContrastCalculator
{
    private const uint Black = 0xFF000000;
    private const uint White = 0xFFFFFFFF;

    public static double RelativeLuminance(uint argb)
    {
        var r = Linearize((argb >> 16) & 0xFF);
        var g = Linearize((argb >> 8) & 0xFF);
        var b = Linearize(argb & 0xFF);
        return (0.2126 * r) + (0.7152 * g) + (0.0722 * b);
    }

    /// <summary>Ratio from 1 to 21. Symmetric in its arguments.</summary>
    public static double ContrastRatio(uint first, uint second)
    {
        var a = RelativeLuminance(first);
        var b = RelativeLuminance(second);
        var (lighter, darker) = a >= b ? (a, b) : (b, a);
        return (lighter + 0.05) / (darker + 0.05);
    }

    /// <summary>Opaque black or white, whichever reads better on <paramref name="background"/>.</summary>
    public static uint ReadableOn(uint background) =>
        ContrastRatio(background, White) > ContrastRatio(background, Black) ? White : Black;

    private static double Linearize(uint channel)
    {
        var c = channel / 255.0;
        return c <= 0.03928 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);
    }
}
