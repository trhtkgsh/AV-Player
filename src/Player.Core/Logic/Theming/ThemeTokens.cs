namespace Player.Core.Logic.Theming;

/// <summary>The resource keys every theme defines. Pages use these through DynamicResource only.</summary>
public static class ThemeTokens
{
    public const string ColorBackground = nameof(ColorBackground);
    public const string ColorSurface = nameof(ColorSurface);
    public const string ColorOnSurface = nameof(ColorOnSurface);
    public const string ColorOnSurfaceMuted = nameof(ColorOnSurfaceMuted);
    public const string ColorAccent = nameof(ColorAccent);
    public const string ColorOnAccent = nameof(ColorOnAccent);
    public const string ColorDivider = nameof(ColorDivider);
    public const string ColorProgress = nameof(ColorProgress);
    public const string ColorWatched = nameof(ColorWatched);
    public const string ColorDanger = nameof(ColorDanger);
    public const string ColorHudBackground = nameof(ColorHudBackground);
    public const string ColorHudOnBackground = nameof(ColorHudOnBackground);

    public static IReadOnlyList<string> All { get; } =
    [
        ColorBackground,
        ColorSurface,
        ColorOnSurface,
        ColorOnSurfaceMuted,
        ColorAccent,
        ColorOnAccent,
        ColorDivider,
        ColorProgress,
        ColorWatched,
        ColorDanger,
        ColorHudBackground,
        ColorHudOnBackground,
    ];
}
