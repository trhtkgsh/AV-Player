namespace Player.Core.Models;

public enum MediaKind { Video, Audio }

public enum DecoderMode { Hardware, Software }

/// <summary><see cref="Original"/> shows the video at 100% (one video pixel per screen pixel).</summary>
public enum AspectMode { Fit, Stretch, Crop, Original }

public enum RepeatMode { None, File, Folder }

public enum PlayerStatus { Idle, Opening, Playing, Paused, Ended, Error }

public enum SortField { Name, DateAdded, Size, Duration }

public enum SortDirection { Ascending, Descending }

/// <summary>
/// What the explorer lists. Folders: every folder that holds media. Flat: all files in one list.
/// Tree: the same folders as a directory tree from the storage root. Favorites: the folders the user marked.
/// </summary>
public enum ExplorerViewMode { Folders, Flat, Tree, Favorites }

/// <summary>Rows, or tiles with a large thumbnail.</summary>
public enum ExplorerLayout { List, Grid }

public enum ThemeId { DarkOled, DarkSlate, Light, HighContrast }

/// <summary>The language of the user interface. System follows the language of the device.</summary>
public enum AppLanguage { System, English, Hebrew }

public enum ScreenSide { Left, Right }

public enum GesturePhase { Began, Changed, Ended, Cancelled }

public enum GestureKind { Tap, DoubleTap, VerticalDrag, HorizontalDrag, Pinch, Pan, LongPress }

public enum FileOperationStatus { Success, Cancelled, Denied, Failed }

[Flags]
public enum ExplorerInfoFields
{
    None = 0,
    Duration = 1,
    Resolution = 2,
    Size = 4,
    Date = 8,
    Path = 16,
}
