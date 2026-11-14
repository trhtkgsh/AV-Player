using Player.Core.Models;

namespace Player.Core.Logic.Theming;

/// <summary>One theme's colors: token key to ARGB.</summary>
public sealed record ThemePalette(ThemeId Id, IReadOnlyDictionary<string, uint> Colors)
{
    public uint this[string token] => Colors[token];
}
