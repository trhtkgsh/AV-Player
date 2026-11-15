namespace Player.Core.Logic.Theming;

/// <summary>The fixed list of 12 accent colors the user can pick from (opaque ARGB). Original colors.</summary>
public static class AccentPalette
{
    /// <summary>The accent of a fresh install. It is one of <see cref="Colors"/>.</summary>
    public const uint Default = 0xFF4C9AFF;

    public static IReadOnlyList<uint> Colors { get; } =
    [
        0xFF4C9AFF, // sky blue (default)
        0xFF2EC4D6, // cyan
        0xFF2FBF9B, // teal
        0xFF57C75E, // green
        0xFFB5D334, // lime
        0xFFFFD400, // yellow
        0xFFFFA23A, // amber
        0xFFFF6F4A, // coral
        0xFFE5484D, // red
        0xFFF06BAA, // pink
        0xFFB07CFF, // violet
        0xFF7C83FF, // indigo
    ];
}
