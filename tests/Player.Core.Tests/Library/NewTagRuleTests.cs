using Player.Core.Logic.Library;
using Player.Core.Models;

namespace Player.Core.Tests.Library;

public sealed class NewTagRuleTests
{
    private const long Now = 2_000_000_000L;
    private const long OneDay = 86_400L;

    private static PlaybackState State() => new(TestData.Id()) { PositionMs = 0 };

    [Fact]
    public void IsNew_AddedOneDayAgoWithoutState_ReturnsTrue()
    {
        var item = TestData.Item(dateAddedSeconds: Now - OneDay);

        Assert.True(NewTagRule.IsNew(item, null, 7, Now));
    }

    [Fact]
    public void IsNew_AddedExactlySevenDaysAgo_ReturnsTrue()
    {
        var item = TestData.Item(dateAddedSeconds: Now - 7 * OneDay);

        Assert.True(NewTagRule.IsNew(item, null, 7, Now));
    }

    [Fact]
    public void IsNew_AddedOneSecondOlderThanWindow_ReturnsFalse()
    {
        var item = TestData.Item(dateAddedSeconds: Now - 7 * OneDay - 1);

        Assert.False(NewTagRule.IsNew(item, null, 7, Now));
    }

    [Fact]
    public void IsNew_HasAnyState_ReturnsFalse()
    {
        var item = TestData.Item(dateAddedSeconds: Now - OneDay);

        Assert.False(NewTagRule.IsNew(item, State(), 7, Now));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void IsNew_NonPositiveWindow_ReturnsFalse(int newTagDays)
    {
        var item = TestData.Item(dateAddedSeconds: Now - OneDay);

        Assert.False(NewTagRule.IsNew(item, null, newTagDays, Now));
    }

    [Fact]
    public void IsNew_DateInFuture_ReturnsTrue()
    {
        var item = TestData.Item(dateAddedSeconds: Now + 3_600);

        Assert.True(NewTagRule.IsNew(item, null, 7, Now));
    }

    [Fact]
    public void IsNew_NullItem_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => NewTagRule.IsNew(null!, null, 7, Now));
    }
}
