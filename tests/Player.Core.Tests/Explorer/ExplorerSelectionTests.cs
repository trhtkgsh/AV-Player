using Player.Core.Logic.Explorer;

namespace Player.Core.Tests.Explorer;

public sealed class ExplorerSelectionTests
{
    [Fact]
    public void New_Empty_IsNotActiveWithZeroCount()
    {
        var selection = new ExplorerSelection<long>();

        Assert.Equal(0, selection.Count);
        Assert.False(selection.IsActive);
    }

    [Fact]
    public void Toggle_UnselectedKey_ReturnsTrueAndSelects()
    {
        var selection = new ExplorerSelection<long>();

        var result = selection.Toggle(5);

        Assert.True(result);
        Assert.True(selection.Contains(5));
        Assert.Equal(1, selection.Count);
        Assert.True(selection.IsActive);
    }

    [Fact]
    public void Toggle_SelectedKey_ReturnsFalseAndLeavesModeWhenLast()
    {
        var selection = new ExplorerSelection<long>();
        selection.Toggle(5);

        var result = selection.Toggle(5);

        Assert.False(result);
        Assert.False(selection.Contains(5));
        Assert.Equal(0, selection.Count);
        Assert.False(selection.IsActive);
    }

    [Fact]
    public void Add_SameKeyTwice_CountsOnce()
    {
        var selection = new ExplorerSelection<long>();

        selection.Add(1);
        selection.Add(1);

        Assert.Equal(1, selection.Count);
    }

    [Fact]
    public void AddRange_WithDuplicates_KeepsEachKeyOnce()
    {
        var selection = new ExplorerSelection<long>();
        selection.Add(2);

        selection.AddRange([1, 2, 3, 3]);

        Assert.Equal(3, selection.Count);
        Assert.Equal(new long[] { 1, 2, 3 }, selection.Keys.OrderBy(k => k));
    }

    [Fact]
    public void AddRange_Empty_ChangesNothing()
    {
        var selection = new ExplorerSelection<long>();

        selection.AddRange([]);

        Assert.False(selection.IsActive);
    }

    [Fact]
    public void Remove_SelectedKey_DeselectsIt()
    {
        var selection = new ExplorerSelection<long>();
        selection.AddRange([1, 2]);

        selection.Remove(1);

        Assert.False(selection.Contains(1));
        Assert.True(selection.Contains(2));
        Assert.Equal(1, selection.Count);
    }

    [Fact]
    public void Remove_UnknownKey_ChangesNothing()
    {
        var selection = new ExplorerSelection<long>();
        selection.Add(1);

        selection.Remove(9);

        Assert.Equal(1, selection.Count);
    }

    [Fact]
    public void Clear_WithKeys_EmptiesAndLeavesMode()
    {
        var selection = new ExplorerSelection<long>();
        selection.AddRange([1, 2, 3]);

        selection.Clear();

        Assert.Equal(0, selection.Count);
        Assert.False(selection.IsActive);
        Assert.False(selection.Contains(1));
    }

    [Fact]
    public void Contains_UnknownKey_ReturnsFalse()
    {
        var selection = new ExplorerSelection<string>();
        selection.Add("a");

        Assert.False(selection.Contains("b"));
        Assert.True(selection.Contains("a"));
    }
}
