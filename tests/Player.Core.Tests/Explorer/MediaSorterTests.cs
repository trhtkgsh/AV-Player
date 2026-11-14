using Player.Core.Logic.Explorer;
using Player.Core.Models;

namespace Player.Core.Tests.Explorer;

public sealed class MediaSorterTests
{
    private static IReadOnlyList<MediaItem> Sort(
        IReadOnlyList<MediaItem> items,
        SortField field,
        SortDirection direction,
        params MediaMetadata[] metadata) =>
        MediaSorter.Sort(items, field, direction, metadata.ToDictionary(m => m.Id));

    private static string[] NamesOf(IEnumerable<MediaItem> items) => items.Select(i => i.DisplayName).ToArray();

    private static List<MediaItem> Trio() =>
    [
        TestData.Item(1, "a.mp4"),
        TestData.Item(2, "b.mp4"),
        TestData.Item(3, "c.mp4"),
    ];

    [Fact]
    public void Sort_InputList_IsNotModified()
    {
        var items = new List<MediaItem> { TestData.Item(1, "b.mp4"), TestData.Item(2, "a.mp4") };

        Sort(items, SortField.Name, SortDirection.Ascending);

        Assert.Equal(new[] { "b.mp4", "a.mp4" }, NamesOf(items));
    }

    [Fact]
    public void Sort_AnyInput_ReturnsNewList()
    {
        var items = new List<MediaItem> { TestData.Item(1, "a.mp4") };

        var result = Sort(items, SortField.Name, SortDirection.Ascending);

        Assert.NotSame(items, result);
    }

    [Fact]
    public void Sort_NameAscending_UsesNaturalOrder()
    {
        var items = new List<MediaItem>
        {
            TestData.Item(1, "ep10.mp4"),
            TestData.Item(2, "ep2.mp4"),
            TestData.Item(3, "Ep1.mp4"),
        };

        var result = Sort(items, SortField.Name, SortDirection.Ascending);

        Assert.Equal(new[] { "Ep1.mp4", "ep2.mp4", "ep10.mp4" }, NamesOf(result));
    }

    [Fact]
    public void Sort_NameDescending_ReversesNaturalOrder()
    {
        var items = new List<MediaItem>
        {
            TestData.Item(1, "ep10.mp4"),
            TestData.Item(2, "ep2.mp4"),
            TestData.Item(3, "Ep1.mp4"),
        };

        var result = Sort(items, SortField.Name, SortDirection.Descending);

        Assert.Equal(new[] { "ep10.mp4", "ep2.mp4", "Ep1.mp4" }, NamesOf(result));
    }

    [Fact]
    public void Sort_DateAddedAscending_OrdersByDateAdded()
    {
        var items = new List<MediaItem>
        {
            TestData.Item(1, "c.mp4", dateAddedSeconds: 300),
            TestData.Item(2, "a.mp4", dateAddedSeconds: 100),
            TestData.Item(3, "b.mp4", dateAddedSeconds: 200),
        };

        var result = Sort(items, SortField.DateAdded, SortDirection.Ascending);

        Assert.Equal(new[] { "a.mp4", "b.mp4", "c.mp4" }, NamesOf(result));
    }

    [Fact]
    public void Sort_DateAddedDescending_OrdersByDateAddedDescending()
    {
        var items = new List<MediaItem>
        {
            TestData.Item(1, "c.mp4", dateAddedSeconds: 300),
            TestData.Item(2, "a.mp4", dateAddedSeconds: 100),
            TestData.Item(3, "b.mp4", dateAddedSeconds: 200),
        };

        var result = Sort(items, SortField.DateAdded, SortDirection.Descending);

        Assert.Equal(new[] { "c.mp4", "b.mp4", "a.mp4" }, NamesOf(result));
    }

    [Fact]
    public void Sort_SizeAscending_OrdersBySize()
    {
        var items = new List<MediaItem>
        {
            TestData.Item(1, "a.mp4", sizeBytes: 300),
            TestData.Item(2, "b.mp4", sizeBytes: 100),
            TestData.Item(3, "c.mp4", sizeBytes: 200),
        };

        var result = Sort(items, SortField.Size, SortDirection.Ascending);

        Assert.Equal(new[] { "b.mp4", "c.mp4", "a.mp4" }, NamesOf(result));
    }

    [Fact]
    public void Sort_SizeDescending_OrdersBySizeDescending()
    {
        var items = new List<MediaItem>
        {
            TestData.Item(1, "a.mp4", sizeBytes: 300),
            TestData.Item(2, "b.mp4", sizeBytes: 100),
            TestData.Item(3, "c.mp4", sizeBytes: 200),
        };

        var result = Sort(items, SortField.Size, SortDirection.Descending);

        Assert.Equal(new[] { "a.mp4", "c.mp4", "b.mp4" }, NamesOf(result));
    }

    [Fact]
    public void Sort_DurationAscending_OrdersByDuration()
    {
        var items = Trio();
        var metadata = new[]
        {
            TestData.Metadata(TestData.Id(1), 5000),
            TestData.Metadata(TestData.Id(2), 1000),
            TestData.Metadata(TestData.Id(3), 3000),
        };

        var result = Sort(items, SortField.Duration, SortDirection.Ascending, metadata);

        Assert.Equal(new[] { "b.mp4", "c.mp4", "a.mp4" }, NamesOf(result));
    }

    [Fact]
    public void Sort_DurationDescending_OrdersByDurationDescending()
    {
        var items = Trio();
        var metadata = new[]
        {
            TestData.Metadata(TestData.Id(1), 5000),
            TestData.Metadata(TestData.Id(2), 1000),
            TestData.Metadata(TestData.Id(3), 3000),
        };

        var result = Sort(items, SortField.Duration, SortDirection.Descending, metadata);

        Assert.Equal(new[] { "a.mp4", "c.mp4", "b.mp4" }, NamesOf(result));
    }

    [Fact]
    public void Sort_DurationAscending_ItemWithoutMetadataSortsLast()
    {
        var items = Trio();
        var metadata = new[]
        {
            TestData.Metadata(TestData.Id(2), 1000),
            TestData.Metadata(TestData.Id(3), 2000),
        };

        var result = Sort(items, SortField.Duration, SortDirection.Ascending, metadata);

        Assert.Equal(new[] { "b.mp4", "c.mp4", "a.mp4" }, NamesOf(result));
    }

    [Fact]
    public void Sort_DurationDescending_ItemWithoutMetadataSortsLast()
    {
        var items = Trio();
        var metadata = new[]
        {
            TestData.Metadata(TestData.Id(2), 1000),
            TestData.Metadata(TestData.Id(3), 2000),
        };

        var result = Sort(items, SortField.Duration, SortDirection.Descending, metadata);

        Assert.Equal(new[] { "c.mp4", "b.mp4", "a.mp4" }, NamesOf(result));
    }

    [Fact]
    public void Sort_DurationAscending_ZeroDurationSortsLast()
    {
        var items = Trio();
        var metadata = new[]
        {
            TestData.Metadata(TestData.Id(1), 0),
            TestData.Metadata(TestData.Id(2), 1000),
            TestData.Metadata(TestData.Id(3), 2000),
        };

        var result = Sort(items, SortField.Duration, SortDirection.Ascending, metadata);

        Assert.Equal(new[] { "b.mp4", "c.mp4", "a.mp4" }, NamesOf(result));
    }

    [Fact]
    public void Sort_DurationDescending_ZeroDurationSortsLast()
    {
        var items = Trio();
        var metadata = new[]
        {
            TestData.Metadata(TestData.Id(1), 0),
            TestData.Metadata(TestData.Id(2), 1000),
            TestData.Metadata(TestData.Id(3), 2000),
        };

        var result = Sort(items, SortField.Duration, SortDirection.Descending, metadata);

        Assert.Equal(new[] { "c.mp4", "b.mp4", "a.mp4" }, NamesOf(result));
    }

    [Fact]
    public void Sort_TiesOnKeyDescending_BreaksTiesByNameAscending()
    {
        var items = new List<MediaItem>
        {
            TestData.Item(1, "c.mp4", sizeBytes: 500),
            TestData.Item(2, "a.mp4", sizeBytes: 500),
            TestData.Item(3, "b.mp4", sizeBytes: 500),
        };

        var result = Sort(items, SortField.Size, SortDirection.Descending);

        Assert.Equal(new[] { "a.mp4", "b.mp4", "c.mp4" }, NamesOf(result));
    }

    [Fact]
    public void Sort_TiesOnKeyAndName_BreaksTiesByMediaStoreIdAscending()
    {
        var items = new List<MediaItem>
        {
            TestData.Item(9, "same.mp4"),
            TestData.Item(3, "same.mp4"),
        };

        var result = Sort(items, SortField.Name, SortDirection.Ascending);

        Assert.Equal(new long[] { 3, 9 }, result.Select(i => i.Id.MediaStoreId).ToArray());
    }

    [Fact]
    public void Sort_EmptyList_ReturnsEmptyList()
    {
        var result = Sort(new List<MediaItem>(), SortField.Name, SortDirection.Ascending);

        Assert.Empty(result);
    }
}
