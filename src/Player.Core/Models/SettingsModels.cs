namespace Player.Core.Models;

public sealed record UiSettings
{
    public int HudAutoHideSeconds { get; init; } = 3;

    /// <summary>Double-tap skip length.</summary>
    public int SkipSeconds { get; init; } = 10;

    public bool ShowClock { get; init; } = true;
    public bool ShowBattery { get; init; } = true;

    public ExplorerInfoFields ExplorerFields { get; init; } =
        ExplorerInfoFields.Duration | ExplorerInfoFields.Resolution | ExplorerInfoFields.Size;

    public ExplorerViewMode ViewMode { get; init; } = ExplorerViewMode.Folders;
    public SortField SortField { get; init; } = SortField.Name;
    public SortDirection SortDirection { get; init; } = SortDirection.Ascending;
    public int NewTagDays { get; init; } = 7;

    /// <summary>Language of the user interface.</summary>
    public AppLanguage Language { get; init; } = AppLanguage.System;
}

/// <summary>One text of the user interface in every supported language.</summary>
public readonly record struct LocalizedText(string English, string Hebrew);

public sealed record ThemeSettings
{
    public ThemeId Theme { get; init; } = ThemeId.DarkOled;
    public uint AccentColorArgb { get; init; } = 0xFF4C9AFF;

    /// <summary>0 .. 1.</summary>
    public double HudOpacity { get; init; } = 0.6;
}
