using Player.Core.Logic.Explorer;
using Player.Core.Models;

namespace Player.Core.Tests.Explorer;

public sealed class ItemSizesTests
{
    private const ExplorerItemSize Small = ExplorerItemSize.Small;
    private const ExplorerItemSize Large = ExplorerItemSize.Large;

    [Fact]
    public void ListThumbnailWidth_Small_IsSmallerThanLarge() =>
        Assert.True(ItemSizes.ListThumbnailWidth(Small) < ItemSizes.ListThumbnailWidth(Large));

    [Fact]
    public void ListThumbnailHeight_Small_IsSmallerThanLarge() =>
        Assert.True(ItemSizes.ListThumbnailHeight(Small) < ItemSizes.ListThumbnailHeight(Large));

    [Fact]
    public void FolderIconWidth_Small_IsSmallerThanLarge() =>
        Assert.True(ItemSizes.FolderIconWidth(Small) < ItemSizes.FolderIconWidth(Large));

    [Fact]
    public void FolderIconHeight_Small_IsSmallerThanLarge() =>
        Assert.True(ItemSizes.FolderIconHeight(Small) < ItemSizes.FolderIconHeight(Large));

    [Fact]
    public void ListThumbnail_Small_KeepsAspectRatioOfLarge()
    {
        var large = ItemSizes.ListThumbnailWidth(Large) / ItemSizes.ListThumbnailHeight(Large);
        var small = ItemSizes.ListThumbnailWidth(Small) / ItemSizes.ListThumbnailHeight(Small);

        Assert.Equal(large, small, 1);
    }

    [Fact]
    public void FolderIcon_Small_KeepsAspectRatioOfLarge()
    {
        var large = ItemSizes.FolderIconWidth(Large) / ItemSizes.FolderIconHeight(Large);
        var small = ItemSizes.FolderIconWidth(Small) / ItemSizes.FolderIconHeight(Small);

        Assert.Equal(large, small, 1);
    }

    [Theory]
    [InlineData(Small)]
    [InlineData(Large)]
    public void AllSizes_ArePositive(ExplorerItemSize size)
    {
        Assert.True(ItemSizes.ListThumbnailWidth(size) > 0);
        Assert.True(ItemSizes.ListThumbnailHeight(size) > 0);
        Assert.True(ItemSizes.FolderIconWidth(size) > 0);
        Assert.True(ItemSizes.FolderIconHeight(size) > 0);
    }
}
