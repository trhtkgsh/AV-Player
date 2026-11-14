using Player.Core.Logic.Explorer;
using Player.Core.Models;

namespace Player.Core.Tests.Explorer;

public sealed class SearchFilterTests
{
    private static List<MediaItem> Items() =>
    [
        TestData.Item(1, "Cat video.mp4"),
        TestData.Item(2, "Dog.mp4"),
        TestData.Item(3, "Kitten.mkv"),
    ];

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Apply_BlankText_ReturnsAllItemsInSameOrder(string? text)
    {
        var items = Items();

        var result = SearchFilter.Apply(items, text);

        Assert.Equal(items, result);
    }

    [Fact]
    public void Apply_UpperCaseTextAgainstLowerCaseName_Matches()
    {
        var items = new List<MediaItem> { TestData.Item(1, "cat clip.mp4") };

        var result = SearchFilter.Apply(items, "CAT");

        Assert.Single(result);
    }

    [Fact]
    public void Apply_SubstringInsideName_Matches()
    {
        var result = SearchFilter.Apply(Items(), "deo");

        Assert.Equal(new[] { "Cat video.mp4" }, result.Select(i => i.DisplayName));
    }

    [Fact]
    public void Apply_PaddedText_IsTrimmedBeforeMatching()
    {
        var result = SearchFilter.Apply(Items(), "  cat ");

        Assert.Equal(new[] { "Cat video.mp4" }, result.Select(i => i.DisplayName));
    }

    [Fact]
    public void Apply_SeveralMatches_KeepsInputOrder()
    {
        var items = new List<MediaItem>
        {
            TestData.Item(1, "Zebra cat.mp4"),
            TestData.Item(2, "Apple.mp4"),
            TestData.Item(3, "Apple cat.mp4"),
            TestData.Item(4, "Cat.mp4"),
        };

        var result = SearchFilter.Apply(items, "cat");

        Assert.Equal(new[] { "Zebra cat.mp4", "Apple cat.mp4", "Cat.mp4" }, result.Select(i => i.DisplayName));
    }

    [Fact]
    public void Apply_NoMatch_ReturnsEmptyList()
    {
        var result = SearchFilter.Apply(Items(), "elephant");

        Assert.Empty(result);
    }

    [Fact]
    public void Apply_TextOnlyInFolderName_DoesNotMatch()
    {
        var items = new List<MediaItem> { TestData.Item(1, "x.mp4", folderName: "Holiday") };

        var result = SearchFilter.Apply(items, "holiday");

        Assert.Empty(result);
    }
}
