using Player.Core.Logic.Explorer;
using Player.Core.Models;

namespace Player.Core.Tests.Explorer;

public sealed class MediaListEditsTests
{
    private static List<MediaItem> Items() =>
    [
        TestData.Item(1, "a.mp4"),
        TestData.Item(2, "b.mp4"),
        TestData.Item(3, "c.mp4"),
    ];

    [Fact]
    public void Replace_ItemFound_PutsReplacementInItsPlace()
    {
        var items = Items();
        var replacement = TestData.Item(2, "renamed.mp4");

        var result = MediaListEdits.Replace(items, items[1].Id, replacement);

        Assert.Equal(["a.mp4", "renamed.mp4", "c.mp4"], result.Select(i => i.DisplayName));
    }

    [Fact]
    public void Replace_ReplacementWithOtherId_TakesTheOldPosition()
    {
        var items = Items();
        var replacement = TestData.Item(9, "new.mp4");

        var result = MediaListEdits.Replace(items, items[0].Id, replacement);

        Assert.Same(replacement, result[0]);
        Assert.Equal(3, result.Count);
    }

    [Fact]
    public void Replace_ItemNotFound_ReturnsEqualList()
    {
        var items = Items();

        var result = MediaListEdits.Replace(items, TestData.Id(99), TestData.Item(99, "x.mp4"));

        Assert.Equal(items, result);
    }

    [Fact]
    public void Replace_Always_DoesNotMutateInput()
    {
        var items = Items();
        var before = items.ToList();

        _ = MediaListEdits.Replace(items, items[1].Id, TestData.Item(2, "renamed.mp4"));

        Assert.Equal(before, items);
    }

    [Fact]
    public void Replace_EmptyList_ReturnsEmpty()
    {
        Assert.Empty(MediaListEdits.Replace([], TestData.Id(1), TestData.Item(1)));
    }

    [Fact]
    public void Replace_NullArguments_Throw()
    {
        Assert.Throws<ArgumentNullException>(() => MediaListEdits.Replace(null!, TestData.Id(1), TestData.Item(1)));
        Assert.Throws<ArgumentNullException>(() => MediaListEdits.Replace(Items(), TestData.Id(1), null!));
    }

    [Fact]
    public void Remove_ItemFound_DropsOnlyThatItem()
    {
        var items = Items();

        var result = MediaListEdits.Remove(items, items[1].Id);

        Assert.Equal(["a.mp4", "c.mp4"], result.Select(i => i.DisplayName));
    }

    [Fact]
    public void Remove_FirstAndLastItem_KeepsOrderOfTheRest()
    {
        var items = Items();

        var withoutFirst = MediaListEdits.Remove(items, items[0].Id);
        var withoutLast = MediaListEdits.Remove(items, items[2].Id);

        Assert.Equal(["b.mp4", "c.mp4"], withoutFirst.Select(i => i.DisplayName));
        Assert.Equal(["a.mp4", "b.mp4"], withoutLast.Select(i => i.DisplayName));
    }

    [Fact]
    public void Remove_ItemNotFound_ReturnsEqualList()
    {
        var items = Items();

        var result = MediaListEdits.Remove(items, TestData.Id(99));

        Assert.Equal(items, result);
    }

    [Fact]
    public void Remove_Always_DoesNotMutateInput()
    {
        var items = Items();
        var before = items.ToList();

        _ = MediaListEdits.Remove(items, items[1].Id);

        Assert.Equal(before, items);
    }

    [Fact]
    public void Remove_EmptyList_ReturnsEmpty()
    {
        Assert.Empty(MediaListEdits.Remove([], TestData.Id(1)));
    }

    [Fact]
    public void Remove_NullList_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => MediaListEdits.Remove(null!, TestData.Id(1)));
    }
}
