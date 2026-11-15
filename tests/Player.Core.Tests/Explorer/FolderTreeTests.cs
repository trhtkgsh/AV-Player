using Player.Core.Logic.Explorer;
using Player.Core.Models;

namespace Player.Core.Tests.Explorer;

public sealed class FolderTreeTests
{
    private static MediaFolder Folder(string relativePath, int items = 1, int news = 0, long bucketId = 0, string? name = null)
    {
        var trimmed = relativePath.Trim('/');
        var lastSegment = trimmed.Length == 0 ? "root" : trimmed[(trimmed.LastIndexOf('/') + 1)..];
        return new MediaFolder(bucketId, name ?? lastSegment, relativePath, items, news);
    }

    private static List<MediaFolder> MixedSet() =>
    [
        Folder("Movies/Series/", items: 5, news: 1, bucketId: 1),
        Folder("Movies/Demo/", items: 3, news: 2, bucketId: 2),
        Folder("Movies/", items: 2, news: 0, bucketId: 3),
        Folder("Music/Tones/", items: 7, news: 4, bucketId: 4),
        Folder("DCIM/Camera/", items: 11, news: 6, bucketId: 5),
    ];

    private static FolderTreeNode Node(IReadOnlyList<FolderTreeNode> nodes, string name) =>
        Assert.Single(nodes, n => n.Name == name);

    [Fact]
    public void GetChildren_EmptyList_ReturnsEmpty()
    {
        var result = FolderTree.GetChildren(new List<MediaFolder>(), "");

        Assert.Empty(result);
    }

    [Fact]
    public void GetChildren_MixedSetAtRoot_ReturnsDistinctFirstSegmentsSortedByName()
    {
        var result = FolderTree.GetChildren(MixedSet(), "");

        Assert.Equal(new[] { "DCIM", "Movies", "Music" }, result.Select(n => n.Name));
        Assert.Equal(new[] { "DCIM", "Movies", "Music" }, result.Select(n => n.Path));
    }

    [Fact]
    public void GetChildren_MixedSetAtRoot_MoviesTotalsCoverItsOwnItemsAndSubFolders()
    {
        var movies = Node(FolderTree.GetChildren(MixedSet(), ""), "Movies");

        Assert.Equal(3, movies.FolderCount);
        Assert.Equal(10, movies.ItemCount);
        Assert.Equal(3, movies.NewCount);
    }

    [Fact]
    public void GetChildren_MixedSetAtRoot_ContainerMusicHasNullFolderAndSubTotals()
    {
        var music = Node(FolderTree.GetChildren(MixedSet(), ""), "Music");

        Assert.Null(music.Folder);
        Assert.Equal(1, music.FolderCount);
        Assert.Equal(7, music.ItemCount);
        Assert.Equal(4, music.NewCount);
    }

    [Fact]
    public void GetChildren_DeeperLevel_ReturnsOnlyChildrenOfParentSorted()
    {
        var result = FolderTree.GetChildren(MixedSet(), "Movies");

        Assert.Equal(new[] { "Demo", "Series" }, result.Select(n => n.Name));
        Assert.Equal(new[] { "Movies/Demo", "Movies/Series" }, result.Select(n => n.Path));
    }

    [Fact]
    public void GetChildren_DeeperLevel_LeafNodeCarriesItsFolderAndOwnCounts()
    {
        var folders = MixedSet();

        var series = Node(FolderTree.GetChildren(folders, "Movies"), "Series");

        Assert.Same(folders[0], series.Folder);
        Assert.Equal(1, series.FolderCount);
        Assert.Equal(5, series.ItemCount);
        Assert.Equal(1, series.NewCount);
    }

    [Fact]
    public void GetChildren_NodeThatIsFolderAndContainer_HasFolderSetAndTotalsIncludeOwnItems()
    {
        var folders = new List<MediaFolder>
        {
            Folder("Movies/", items: 4, news: 1, bucketId: 1),
            Folder("Movies/Series/", items: 6, news: 2, bucketId: 2),
        };

        var movies = Node(FolderTree.GetChildren(folders, ""), "Movies");

        Assert.Same(folders[0], movies.Folder);
        Assert.Equal(2, movies.FolderCount);
        Assert.Equal(10, movies.ItemCount);
        Assert.Equal(3, movies.NewCount);
    }

    [Fact]
    public void GetChildren_FolderAndContainerListedInReverseOrder_FolderIsStillTheOwnPathFolder()
    {
        var folders = new List<MediaFolder>
        {
            Folder("Movies/Series/", items: 6, news: 2, bucketId: 2),
            Folder("Movies/", items: 4, news: 1, bucketId: 1),
        };

        var movies = Node(FolderTree.GetChildren(folders, ""), "Movies");

        Assert.Same(folders[1], movies.Folder);
        Assert.Equal(10, movies.ItemCount);
    }

    [Fact]
    public void GetChildren_ThreeLevels_RootTotalsSumAllLevels()
    {
        var folders = new List<MediaFolder>
        {
            Folder("A/", items: 1, news: 1),
            Folder("A/B/", items: 2, news: 0),
            Folder("A/B/C/", items: 4, news: 3),
        };

        var a = Node(FolderTree.GetChildren(folders, ""), "A");

        Assert.Equal(3, a.FolderCount);
        Assert.Equal(7, a.ItemCount);
        Assert.Equal(4, a.NewCount);
    }

    [Fact]
    public void GetChildren_ThreeLevels_MiddleNodeTotalsCoverItselfAndBelowOnly()
    {
        var folders = new List<MediaFolder>
        {
            Folder("A/", items: 1, news: 1),
            Folder("A/B/", items: 2, news: 0),
            Folder("A/B/C/", items: 4, news: 3),
        };

        var b = Node(FolderTree.GetChildren(folders, "A"), "B");

        Assert.Equal("A/B", b.Path);
        Assert.Equal(2, b.FolderCount);
        Assert.Equal(6, b.ItemCount);
        Assert.Equal(3, b.NewCount);
    }

    [Fact]
    public void GetChildren_ThreeLevels_DeepestParentReturnsLeafOnly()
    {
        var folders = new List<MediaFolder>
        {
            Folder("A/", items: 1),
            Folder("A/B/", items: 2),
            Folder("A/B/C/", items: 4, news: 3),
        };

        var result = FolderTree.GetChildren(folders, "A/B");

        var c = Assert.Single(result);
        Assert.Equal("A/B/C", c.Path);
        Assert.NotNull(c.Folder);
        Assert.Equal(1, c.FolderCount);
    }

    [Fact]
    public void GetChildren_PureContainerWithDeepFolder_HasNullFolder()
    {
        var folders = new List<MediaFolder> { Folder("Storage/Backups/Old/", items: 9, news: 2) };

        var storage = Node(FolderTree.GetChildren(folders, ""), "Storage");

        Assert.Null(storage.Folder);
        Assert.Equal(1, storage.FolderCount);
        Assert.Equal(9, storage.ItemCount);
        Assert.Equal(2, storage.NewCount);
    }

    [Fact]
    public void GetChildren_PureContainerAtSecondLevel_HasNullFolder()
    {
        var folders = new List<MediaFolder> { Folder("Storage/Backups/Old/") };

        var backups = Node(FolderTree.GetChildren(folders, "Storage"), "Backups");

        Assert.Null(backups.Folder);
        Assert.Equal("Storage/Backups", backups.Path);
    }

    [Fact]
    public void GetChildren_SiblingWhoseNameStartsWithAnotherName_AreDifferentNodes()
    {
        var folders = new List<MediaFolder>
        {
            Folder("Movies/", items: 2, bucketId: 1),
            Folder("Movies2/", items: 3, bucketId: 2),
        };

        var result = FolderTree.GetChildren(folders, "");

        Assert.Equal(new[] { "Movies", "Movies2" }, result.Select(n => n.Name));
        Assert.Equal(2, Node(result, "Movies").ItemCount);
        Assert.Equal(3, Node(result, "Movies2").ItemCount);
        Assert.Equal(1, Node(result, "Movies").FolderCount);
    }

    [Fact]
    public void GetChildren_ParentWithSiblingSharingItsPrefix_DoesNotTreatSiblingAsChild()
    {
        var folders = new List<MediaFolder>
        {
            Folder("Movies/Series/"),
            Folder("Movies2/Other/"),
            Folder("Movies2/"),
        };

        var result = FolderTree.GetChildren(folders, "Movies");

        var series = Assert.Single(result);
        Assert.Equal("Series", series.Name);
    }

    [Theory]
    [InlineData("Movies/Series")]
    [InlineData("/Movies/Series")]
    [InlineData("Movies//Series/")]
    [InlineData("//Movies//Series//")]
    public void GetChildren_RelativePathWithSlashVariants_NormalizesToSamePath(string relativePath)
    {
        var folders = new List<MediaFolder> { Folder(relativePath, items: 5) };

        var result = FolderTree.GetChildren(folders, "Movies");

        var node = Assert.Single(result);
        Assert.Equal("Series", node.Name);
        Assert.Equal("Movies/Series", node.Path);
        Assert.Same(folders[0], node.Folder);
    }

    [Theory]
    [InlineData("Movies")]
    [InlineData("Movies/")]
    [InlineData("/Movies")]
    [InlineData("/Movies/")]
    [InlineData("//Movies//")]
    public void GetChildren_ParentPathWithSlashVariants_GivesSameResult(string parentPath)
    {
        var result = FolderTree.GetChildren(MixedSet(), parentPath);

        Assert.Equal(new[] { "Movies/Demo", "Movies/Series" }, result.Select(n => n.Path));
    }

    [Theory]
    [InlineData("/")]
    [InlineData("//")]
    public void GetChildren_ParentPathOnlySlashes_IsTheRoot(string parentPath)
    {
        var result = FolderTree.GetChildren(MixedSet(), parentPath);

        Assert.Equal(new[] { "DCIM", "Movies", "Music" }, result.Select(n => n.Name));
    }

    [Fact]
    public void GetChildren_DoubleSlashInsideParentPath_MatchesSingleSlashFolder()
    {
        var folders = new List<MediaFolder> { Folder("Movies/Series/Ep1/") };

        var result = FolderTree.GetChildren(folders, "Movies//Series");

        var node = Assert.Single(result);
        Assert.Equal("Movies/Series/Ep1", node.Path);
    }

    [Fact]
    public void GetChildren_DifferentCase_AreDifferentNodes()
    {
        var folders = new List<MediaFolder>
        {
            Folder("movies/", items: 1, bucketId: 1),
            Folder("Movies/", items: 2, bucketId: 2),
        };

        var result = FolderTree.GetChildren(folders, "");

        Assert.Equal(2, result.Count);
        Assert.Equal(1, Node(result, "movies").ItemCount);
        Assert.Equal(2, Node(result, "Movies").ItemCount);
    }

    [Fact]
    public void GetChildren_ParentPathWithDifferentCase_ReturnsEmpty()
    {
        var result = FolderTree.GetChildren(MixedSet(), "movies");

        Assert.Empty(result);
    }

    [Fact]
    public void GetChildren_NamesWithNumbers_AreInNaturalOrder()
    {
        var folders = new List<MediaFolder>
        {
            Folder("Show/ep10/"),
            Folder("Show/ep2/"),
            Folder("Show/ep1/"),
        };

        var result = FolderTree.GetChildren(folders, "Show");

        Assert.Equal(new[] { "ep1", "ep2", "ep10" }, result.Select(n => n.Name));
    }

    [Fact]
    public void GetChildren_NamesWithNumbersAtRoot_AreInNaturalOrder()
    {
        var folders = new List<MediaFolder> { Folder("ep10/"), Folder("ep2/") };

        var result = FolderTree.GetChildren(folders, "");

        Assert.Equal(new[] { "ep2", "ep10" }, result.Select(n => n.Name));
    }

    [Fact]
    public void GetChildren_ParentPathThatDoesNotExist_ReturnsEmpty()
    {
        var result = FolderTree.GetChildren(MixedSet(), "Pictures");

        Assert.Empty(result);
    }

    [Fact]
    public void GetChildren_ParentPathDeeperThanAnyFolder_ReturnsEmpty()
    {
        var result = FolderTree.GetChildren(MixedSet(), "Movies/Series/Season 1");

        Assert.Empty(result);
    }

    [Fact]
    public void GetChildren_ParentIsMediaFolderWithoutSubFolders_ReturnsEmpty()
    {
        var result = FolderTree.GetChildren(MixedSet(), "Music/Tones");

        Assert.Empty(result);
    }

    [Fact]
    public void GetChildren_MediaFolderAtParentPath_IsNotItsOwnChild()
    {
        var folders = new List<MediaFolder> { Folder("Movies/", items: 4), Folder("Movies/Series/", items: 1) };

        var result = FolderTree.GetChildren(folders, "Movies");

        Assert.Equal(new[] { "Series" }, result.Select(n => n.Name));
    }

    [Fact]
    public void GetChildren_EmptyRelativePath_IsRootNodeNamedByFolderName()
    {
        var folders = new List<MediaFolder> { Folder("", items: 3, news: 1, bucketId: 1, name: "Downloads") };

        var result = FolderTree.GetChildren(folders, "");

        var node = Assert.Single(result);
        Assert.Equal("Downloads", node.Name);
        Assert.Equal("Downloads", node.Path);
        Assert.Same(folders[0], node.Folder);
        Assert.Equal(1, node.FolderCount);
        Assert.Equal(3, node.ItemCount);
        Assert.Equal(1, node.NewCount);
    }

    [Fact]
    public void GetChildren_SeveralEmptyRelativePaths_AreSeparateRootNodesSorted()
    {
        var folders = new List<MediaFolder>
        {
            Folder("", bucketId: 1, name: "Zeta"),
            Folder("", bucketId: 2, name: "Alpha"),
        };

        var result = FolderTree.GetChildren(folders, "");

        Assert.Equal(new[] { "Alpha", "Zeta" }, result.Select(n => n.Name));
    }

    [Fact]
    public void GetChildren_EmptyRelativePathFolder_IsNotAChildOfANamedParent()
    {
        var folders = new List<MediaFolder> { Folder("", name: "Downloads"), Folder("Movies/Series/") };

        var result = FolderTree.GetChildren(folders, "Movies");

        Assert.Equal(new[] { "Series" }, result.Select(n => n.Name));
    }

    [Fact]
    public void GetChildren_EmptyRelativePathFolderWithSameNameAsDirectory_MergesIntoOneNode()
    {
        var folders = new List<MediaFolder>
        {
            Folder("", items: 2, bucketId: 1, name: "Movies"),
            Folder("Movies/Series/", items: 5, bucketId: 2),
        };

        var node = Assert.Single(FolderTree.GetChildren(folders, ""));

        Assert.Same(folders[0], node.Folder);
        Assert.Equal(2, node.FolderCount);
        Assert.Equal(7, node.ItemCount);
    }

    [Fact]
    public void GetChildren_TwoFoldersWithSameNormalizedPath_BothCountedAndFirstFolderIsKept()
    {
        // Pinned behaviour: duplicates are not merged away. Both add to the totals and the first one in the list is Folder.
        var folders = new List<MediaFolder>
        {
            Folder("Movies/", items: 2, news: 1, bucketId: 1),
            Folder("/Movies", items: 3, news: 2, bucketId: 2),
        };

        var node = Assert.Single(FolderTree.GetChildren(folders, ""));

        Assert.Same(folders[0], node.Folder);
        Assert.Equal(2, node.FolderCount);
        Assert.Equal(5, node.ItemCount);
        Assert.Equal(3, node.NewCount);
    }

    [Fact]
    public void GetChildren_AnyInput_DoesNotModifyTheInputList()
    {
        var folders = MixedSet();
        var snapshot = folders.ToList();

        _ = FolderTree.GetChildren(folders, "");
        _ = FolderTree.GetChildren(folders, "Movies");

        Assert.Equal(snapshot, folders);
        Assert.All(snapshot.Zip(folders), pair => Assert.Same(pair.First, pair.Second));
    }

    [Fact]
    public void GetChildren_CalledTwice_ReturnsEqualResults()
    {
        var folders = MixedSet();

        var first = FolderTree.GetChildren(folders, "");
        var second = FolderTree.GetChildren(folders, "");

        Assert.Equal(first, second);
    }

    [Fact]
    public void GetChildren_NullFolders_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => FolderTree.GetChildren(null!, ""));
    }

    [Fact]
    public void GetChildren_NullParentPath_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => FolderTree.GetChildren(MixedSet(), null!));
    }

    [Fact]
    public void GetChildren_NullEntryInList_IsSkipped()
    {
        var folders = new List<MediaFolder> { null!, Folder("Movies/", items: 2) };

        var node = Assert.Single(FolderTree.GetChildren(folders, ""));

        Assert.Equal("Movies", node.Name);
        Assert.Equal(1, node.FolderCount);
    }
}
