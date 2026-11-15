namespace Player.Core.Models;

public sealed record UiSettings
{
    public int HudAutoHideSeconds { get; init; } = 3;

    /// <summary>Double-tap skip length.</summary>
    public int SkipSeconds { get; init; } = 10;

    public bool ShowClock { get; init; } = true;
    public bool ShowBattery { get; init; } = true;

    /// <summary>The details shown under a file name in the explorer. None by default: the name only.</summary>
    public ExplorerInfoFields ExplorerFields { get; init; } = ExplorerInfoFields.None;

    public ExplorerViewMode ViewMode { get; init; } = ExplorerViewMode.Folders;
    public SortField SortField { get; init; } = SortField.Name;
    public SortDirection SortDirection { get; init; } = SortDirection.Ascending;
    public int NewTagDays { get; init; } = 7;

    /// <summary>Language of the user interface.</summary>
    public AppLanguage Language { get; init; } = AppLanguage.System;

    /// <summary>Rows or tiles in the explorer.</summary>
    public ExplorerLayout Layout { get; init; } = ExplorerLayout.List;

    /// <summary>Size of the thumbnails in rows and of the tiles in the grid.</summary>
    public ExplorerItemSize ItemSize { get; init; } = ExplorerItemSize.Large;

    /// <summary>Scale of folder and file names in the explorer: 0.8 .. 1.6.</summary>
    public double ExplorerFontScale { get; init; } = 1.0;

    /// <summary>False hides folders that hold no video file.</summary>
    public bool IncludeAudioOnlyFolders { get; init; } = true;

    /// <summary>Playback speed while a finger is held on the picture: 0.25 .. 4.0.</summary>
    public float LongPressSpeed { get; init; } = 2.0f;

    /// <summary>What is shown over the picture while the player controls are hidden.</summary>
    public PlaybackOverlaySettings Overlay { get; init; } = new();
}

/// <summary>
/// The quiet display during playback, when the controls are hidden: a thin progress line along one
/// edge of the screen and one line of text (playback times, clock, battery) in one corner of the screen.
/// </summary>
public sealed record PlaybackOverlaySettings
{
    /// <summary>The thin progress line. It uses the theme's progress color.</summary>
    public bool ProgressLine { get; init; } = true;

    /// <summary>Height of the progress line, in device-independent units: 1 .. 12.</summary>
    public int ProgressLineThickness { get; init; } = 2;

    public OverlayEdge ProgressLineEdge { get; init; } = OverlayEdge.Bottom;
    public bool Clock { get; init; } = true;
    public bool Battery { get; init; } = true;
    public OverlayTimeMode Time { get; init; } = OverlayTimeMode.ElapsedTotal;

    /// <summary>The corner of the screen (not of the picture) that holds the playback times.</summary>
    public OverlayCorner Corner { get; init; } = OverlayCorner.TopRight;

    /// <summary>The corner of the clock. Texts that share a corner are joined into one line.</summary>
    public OverlayCorner ClockCorner { get; init; } = OverlayCorner.TopLeft;

    /// <summary>The corner of the battery level.</summary>
    public OverlayCorner BatteryCorner { get; init; } = OverlayCorner.TopLeft;

    /// <summary>Distance of the text from the screen edges, in device-independent units: 0 .. 48.</summary>
    public int MarginDp { get; init; }

    /// <summary>Text size: 10 .. 24.</summary>
    public int FontSize { get; init; } = 12;

    /// <summary>Text color, ARGB. White by default.</summary>
    public uint ColorArgb { get; init; } = 0xFFFFFFFF;

    public const int MinMarginDp = 0;
    public const int MaxMarginDp = 48;
    public const int MinFontSize = 10;
    public const int MaxFontSize = 24;
    public const int MinProgressLineThickness = 1;
    public const int MaxProgressLineThickness = 12;
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
