using Player.Core.Logic.Explorer;
using Player.Core.Models;

namespace Player.Core.Tests.Explorer;

public sealed class SelectionRulesTests
{
    [Theory]
    [InlineData(SelectionAction.Delete)]
    [InlineData(SelectionAction.Move)]
    [InlineData(SelectionAction.Copy)]
    [InlineData(SelectionAction.Rename)]
    public void Allows_NothingSelected_IsFalse(SelectionAction action)
    {
        Assert.False(SelectionRules.Allows(action, 0, 0));
    }

    [Theory]
    [InlineData(SelectionAction.Delete)]
    [InlineData(SelectionAction.Move)]
    [InlineData(SelectionAction.Copy)]
    public void Allows_OneFile_IsTrue(SelectionAction action)
    {
        Assert.True(SelectionRules.Allows(action, 1, 0));
    }

    [Theory]
    [InlineData(SelectionAction.Delete)]
    [InlineData(SelectionAction.Move)]
    [InlineData(SelectionAction.Copy)]
    public void Allows_OnlyFolders_IsTrue(SelectionAction action)
    {
        Assert.True(SelectionRules.Allows(action, 0, 2));
    }

    [Theory]
    [InlineData(SelectionAction.Delete)]
    [InlineData(SelectionAction.Move)]
    [InlineData(SelectionAction.Copy)]
    public void Allows_FilesAndFolders_IsTrue(SelectionAction action)
    {
        Assert.True(SelectionRules.Allows(action, 3, 1));
    }

    [Fact]
    public void Allows_RenameOneFileNoFolder_IsTrue()
    {
        Assert.True(SelectionRules.Allows(SelectionAction.Rename, 1, 0));
    }

    [Theory]
    [InlineData(2, 0)]
    [InlineData(1, 1)]
    [InlineData(0, 1)]
    public void Allows_RenameOtherCombinations_IsFalse(int files, int folders)
    {
        Assert.False(SelectionRules.Allows(SelectionAction.Rename, files, folders));
    }

    [Fact]
    public void Distinct_Duplicates_KeepsFirstSeenOrder()
    {
        var items = new[] { (1, "a"), (2, "b"), (1, "c"), (3, "d"), (2, "e") };

        var result = SelectionRules.Distinct(items, i => i.Item1);

        Assert.Equal(new[] { (1, "a"), (2, "b"), (3, "d") }, result);
    }

    [Fact]
    public void Distinct_Empty_ReturnsEmpty()
    {
        Assert.Empty(SelectionRules.Distinct(Array.Empty<int>(), i => i));
    }

    [Fact]
    public void SingleBucket_AllSame_ReturnsIt()
    {
        Assert.Equal(7L, SelectionRules.SingleBucket([7L, 7L, 7L]));
    }

    [Fact]
    public void SingleBucket_One_ReturnsIt()
    {
        Assert.Equal(4L, SelectionRules.SingleBucket([4L]));
    }

    [Fact]
    public void SingleBucket_Several_ReturnsNull()
    {
        Assert.Null(SelectionRules.SingleBucket([7L, 7L, 8L]));
    }

    [Fact]
    public void SingleBucket_None_ReturnsNull()
    {
        Assert.Null(SelectionRules.SingleBucket([]));
    }

    private static List<MediaFolder> Folders() =>
    [
        new MediaFolder(3, "C", "c/", 1, 0),
        new MediaFolder(1, "A", "a/", 1, 0),
        new MediaFolder(2, "B", "b/", 1, 0),
    ];

    [Fact]
    public void TargetChoices_SourceBucket_LeavesItOutKeepsOrder()
    {
        var result = SelectionRules.TargetChoices(Folders(), 1);

        Assert.Equal(new long[] { 3, 2 }, result.Select(f => f.BucketId));
    }

    [Fact]
    public void TargetChoices_NullSource_KeepsAllInOrder()
    {
        var result = SelectionRules.TargetChoices(Folders(), null);

        Assert.Equal(new long[] { 3, 1, 2 }, result.Select(f => f.BucketId));
    }

    [Fact]
    public void TargetChoices_UnknownSource_KeepsAll()
    {
        Assert.Equal(3, SelectionRules.TargetChoices(Folders(), 99).Count);
    }
}
