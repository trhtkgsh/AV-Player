using Player.Data.GlobalDefaultsStorage;

namespace Player.Data.Tests.GlobalDefaultsStorage;

public sealed class LeaveChoiceMapTests
{
    [Theory]
    [InlineData(true, true, LeaveChoice.Window)]
    [InlineData(true, false, LeaveChoice.Window)]
    [InlineData(false, true, LeaveChoice.Sound)]
    [InlineData(false, false, LeaveChoice.Pause)]
    public void FromFields_Pair_ReturnsChoice(bool pip, bool audio, LeaveChoice expected)
    {
        Assert.Equal(expected, LeaveChoiceMap.FromFields(pip, audio));
    }

    [Fact]
    public void ToFields_Window_PipAndAudioOn()
    {
        Assert.Equal((true, true), LeaveChoiceMap.ToFields(LeaveChoice.Window));
    }

    [Fact]
    public void ToFields_Sound_PipOffAudioOn()
    {
        Assert.Equal((false, true), LeaveChoiceMap.ToFields(LeaveChoice.Sound));
    }

    [Fact]
    public void ToFields_Pause_BothOff()
    {
        Assert.Equal((false, false), LeaveChoiceMap.ToFields(LeaveChoice.Pause));
    }

    [Theory]
    [InlineData(LeaveChoice.Window)]
    [InlineData(LeaveChoice.Sound)]
    [InlineData(LeaveChoice.Pause)]
    public void RoundTrip_ChoiceToFieldsToChoice_IsUnchanged(LeaveChoice choice)
    {
        var (pip, audio) = LeaveChoiceMap.ToFields(choice);

        Assert.Equal(choice, LeaveChoiceMap.FromFields(pip, audio));
    }
}
