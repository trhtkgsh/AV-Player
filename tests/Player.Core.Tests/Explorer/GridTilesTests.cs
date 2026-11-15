using Player.Core.Logic.Explorer;
using Player.Core.Models;

namespace Player.Core.Tests.Explorer;

public sealed class GridTilesTests
{
    [Theory]
    [InlineData(100)]
    [InlineData(360)]
    [InlineData(500)]
    [InlineData(800)]
    [InlineData(2000)]
    public void Columns_LargeSize_EqualsWidthOnlyOverload(double width)
    {
        Assert.Equal(GridTiles.Columns(width), GridTiles.Columns(width, ExplorerItemSize.Large));
    }

    [Fact]
    public void Columns_SmallPortrait360_IsThree()
    {
        Assert.Equal(3, GridTiles.Columns(360, ExplorerItemSize.Small));
    }

    [Fact]
    public void Columns_SmallLandscape_IsMoreThanPortrait()
    {
        Assert.True(GridTiles.Columns(640, ExplorerItemSize.Small) > GridTiles.Columns(360, ExplorerItemSize.Small));
        Assert.Equal(5, GridTiles.Columns(640, ExplorerItemSize.Small));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(100)]
    public void Columns_SmallNarrow_ClampsToSmallMinimum(double width)
    {
        Assert.Equal(GridTiles.SmallMinColumns, GridTiles.Columns(width, ExplorerItemSize.Small));
    }

    [Theory]
    [InlineData(900)]
    [InlineData(5000)]
    public void Columns_SmallVeryWide_ClampsToSmallMaximum(double width)
    {
        Assert.Equal(GridTiles.SmallMaxColumns, GridTiles.Columns(width, ExplorerItemSize.Small));
    }

    [Fact]
    public void TileWidth_SmallColumns_SharesWidthAndSpacing()
    {
        var columns = GridTiles.Columns(360, ExplorerItemSize.Small);

        var tile = GridTiles.TileWidth(360, columns);

        Assert.Equal((360 - GridTiles.Spacing * 2) / 3, tile, 6);
        Assert.Equal(360, tile * columns + GridTiles.Spacing * (columns - 1), 6);
    }

    [Fact]
    public void TileWidth_SmallTileIsNarrowerThanLargeTile()
    {
        var small = GridTiles.TileWidth(360, GridTiles.Columns(360, ExplorerItemSize.Small));
        var large = GridTiles.TileWidth(360, GridTiles.Columns(360, ExplorerItemSize.Large));

        Assert.True(small < large);
    }
}
