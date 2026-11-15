using Player.Core.Models;

namespace Player.Core.Logic.Explorer;

/// <summary>
/// The sizes of the thumbnails and icons of the explorer for each <see cref="ExplorerItemSize"/>, in device-independent units.
/// Small is half the width of Large, with the same aspect ratio. Large is the explorer's look before the setting existed.
/// </summary>
public static class ItemSizes
{
    /// <summary>The width of a file thumbnail in a list row.</summary>
    public static double ListThumbnailWidth(ExplorerItemSize size) => size == ExplorerItemSize.Small ? 48 : 96;

    /// <summary>The height of a file thumbnail in a list row: 16:9 of the width.</summary>
    public static double ListThumbnailHeight(ExplorerItemSize size) => size == ExplorerItemSize.Small ? 27 : 54;

    /// <summary>The width of the folder symbol on a grid tile.</summary>
    public static double FolderIconWidth(ExplorerItemSize size) => size == ExplorerItemSize.Small ? 36 : 72;

    /// <summary>The height of the folder symbol on a grid tile: the same aspect as Large.</summary>
    public static double FolderIconHeight(ExplorerItemSize size) => size == ExplorerItemSize.Small ? 28 : 56;
}
