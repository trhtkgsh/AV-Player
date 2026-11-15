using Player.Core.Models;

namespace Player.Core.Logic.Explorer;

/// <summary>The grid layout of the explorer pages: the number of columns and the tile width from the page width.</summary>
public static class GridTiles
{
    /// <summary>The narrowest tile the grid aims for, in device-independent units.</summary>
    public const double MinTileWidth = 180;

    /// <summary>The narrowest small tile the grid aims for, in device-independent units.</summary>
    public const double SmallMinTileWidth = 120;

    /// <summary>The fewest columns: two in portrait.</summary>
    public const int MinColumns = 2;

    /// <summary>The most columns: four in landscape.</summary>
    public const int MaxColumns = 4;

    /// <summary>The fewest small columns: three in portrait.</summary>
    public const int SmallMinColumns = 3;

    /// <summary>The most small columns: six in landscape.</summary>
    public const int SmallMaxColumns = 6;

    /// <summary>The space between two tiles of a row and between two rows.</summary>
    public const double Spacing = 8;

    /// <summary>The number of columns for a page of the given width: width / 180 rounded down, from 2 to 4.</summary>
    public static int Columns(double width) =>
        Math.Clamp((int)(width / MinTileWidth), MinColumns, MaxColumns);

    /// <summary>
    /// The number of columns for a page of the given width and item size. Large is <see cref="Columns(double)"/>.
    /// Small: width / 120 rounded down, from 3 to 6.
    /// </summary>
    public static int Columns(double width, ExplorerItemSize size) =>
        size == ExplorerItemSize.Small
            ? Math.Clamp((int)(width / SmallMinTileWidth), SmallMinColumns, SmallMaxColumns)
            : Columns(width);

    /// <summary>The width of one tile when the given number of columns shares the width and the spacing.</summary>
    public static double TileWidth(double width, int columns) =>
        (width - Spacing * (columns - 1)) / columns;
}
