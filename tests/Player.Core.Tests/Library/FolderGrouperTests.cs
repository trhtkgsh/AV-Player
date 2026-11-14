using Player.Core.Logic.Library;
using Player.Core.Models;

namespace Player.Core.Tests.Library;

public sealed class FolderGrouperTests
{
    private const long Now = 2_000_000_000L;
    private const long OneDay = 86_400L;
    private const int NewTagDays = 7;

    private static IReadOnlyList<MediaFolder> Group(
        IReadOnlyList<MediaItem> items,
        Dictionary<MediaFileId, PlaybackState>? states = null) =>
        FolderGrouper.Group(items, states ?? new Dictionary<MediaFileId, PlaybackState>(), NewTagDays, Now);

    [Fact]
    public void Group_ItemsSharingBucket_FormOneFolderWithCount()
    {
        var items = new List<MediaItem>
        {
            TestData.Item(1, bucketId: 10),
            TestData.Item(2, bucketId: 10),
            TestData.Item(3, bucketId: 10),
            TestData.Item(4, bucketId: 20),
        };

        var result = Group(items);

        Assert.Equal(new long[] { 10, 20 }, result.Select(f => f.BucketId).ToArray());
        Assert.Equal(3, Assert.Single(result, f => f.BucketId == 10).ItemCount);
    }

    [Fact]
    public void Group_MixedNewAndOldItems_CountsOnlyNewItems()
    {
        var recent = Now - 3_600;
        var old = Now - 30 * OneDay;
        var items = new List<MediaItem>
        {
            TestData.Item(1, dateAddedSeconds: recent),
            TestData.Item(2, dateAddedSeconds: recent),
            TestData.Item(3, dateAddedSeconds: old),
            TestData.Item(4, dateAddedSeconds: recent),
        };
        var states = new Dictionary<MediaFileId, PlaybackState>
        {
            [TestData.Id(2)] = new(TestData.Id(2)) { PositionMs = 500 },
        };

        var folder = Assert.Single(Group(items, states));

        Assert.Equal(4, folder.ItemCount);
        Assert.Equal(2, folder.NewCount);
    }

    [Fact]
    public void Group_NameAndRelativePath_TakenFromFirstItemOfBucket()
    {
        var items = new List<MediaItem>
        {
            TestData.Item(1, bucketId: 10, folderName: "Camera"),
            TestData.Item(2, bucketId: 10, folderName: "Other"),
        };

        var folder = Assert.Single(Group(items));

        Assert.Equal("Camera", folder.Name);
        Assert.Equal("Camera/", folder.RelativePath);
    }

    [Fact]
    public void Group_FolderNames_OrderedIgnoringCase()
    {
        var items = new List<MediaItem>
        {
            TestData.Item(1, bucketId: 1, folderName: "beta"),
            TestData.Item(2, bucketId: 2, folderName: "Alpha"),
            TestData.Item(3, bucketId: 3, folderName: "gamma"),
        };

        var result = Group(items);

        Assert.Equal(new[] { "Alpha", "beta", "gamma" }, result.Select(f => f.Name).ToArray());
    }

    [Fact]
    public void Group_SameNameFolders_OrderedByBucketId()
    {
        var items = new List<MediaItem>
        {
            TestData.Item(1, bucketId: 30, folderName: "Camera"),
            TestData.Item(2, bucketId: 20, folderName: "camera"),
        };

        var result = Group(items);

        Assert.Equal(new long[] { 20, 30 }, result.Select(f => f.BucketId).ToArray());
    }

    [Fact]
    public void Group_EmptyInput_ReturnsEmptyList()
    {
        var result = Group(new List<MediaItem>());

        Assert.Empty(result);
    }
}
