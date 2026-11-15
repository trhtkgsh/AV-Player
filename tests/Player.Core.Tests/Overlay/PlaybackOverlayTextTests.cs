using Player.Core.Logic.Overlay;
using Player.Core.Models;

namespace Player.Core.Tests.Overlay;

public sealed class PlaybackOverlayTextTests
{
    private const long Min = 60_000;
    private const long Hour = 3_600_000;

    // 12:30 of 45:00.
    private const long Pos = 12 * Min + 30_000;
    private const long Dur = 45 * Min;

    [Theory]
    [InlineData(0, "0:00")]
    [InlineData(5_000, "0:05")]
    [InlineData(59_000, "0:59")]
    [InlineData(Pos, "12:30")]
    [InlineData(Hour - 1000, "59:59")]
    [InlineData(Hour, "1:00:00")]
    [InlineData(Hour + 2 * Min + 3_000, "1:02:03")]
    [InlineData(11 * Hour + 59 * Min + 59_000, "11:59:59")]
    [InlineData(-5_000, "0:00")]
    [InlineData(long.MinValue, "0:00")]
    [InlineData(999, "0:00")]
    [InlineData(1_999, "0:01")]
    [InlineData(Hour - 1, "59:59")]
    public void FormatTime_Value_FormatsAsExpected(long ms, string expected) =>
        Assert.Equal(expected, PlaybackOverlayText.FormatTime(ms));

    [Theory]
    [InlineData(Pos, Dur, "-32:30")]
    [InlineData(0, Dur, "-45:00")]
    [InlineData(Dur, Dur, "-0:00")]
    [InlineData(Dur + 10_000, Dur, "-0:00")]
    [InlineData(-10_000, Dur, "-45:00")]
    [InlineData(0, 2 * Hour, "-2:00:00")]
    [InlineData(Pos, 0, "")]
    [InlineData(Pos, -1, "")]
    [InlineData(0, -5_000, "")]
    public void FormatRemaining_Values_FormatsAsExpected(long pos, long dur, string expected) =>
        Assert.Equal(expected, PlaybackOverlayText.FormatRemaining(pos, dur));

    [Theory]
    [InlineData(OverlayTimeMode.None, "")]
    [InlineData(OverlayTimeMode.Elapsed, "12:30")]
    [InlineData(OverlayTimeMode.ElapsedTotal, "12:30 / 45:00")]
    [InlineData(OverlayTimeMode.Remaining, "-32:30")]
    [InlineData(OverlayTimeMode.ElapsedRemaining, "12:30 / -32:30")]
    [InlineData(OverlayTimeMode.ElapsedTotalRemaining, "12:30 / 45:00 / -32:30")]
    public void FormatTimes_KnownDuration_EveryMode(OverlayTimeMode mode, string expected) =>
        Assert.Equal(expected, PlaybackOverlayText.FormatTimes(mode, Pos, Dur));

    [Theory]
    [InlineData(OverlayTimeMode.None, "")]
    [InlineData(OverlayTimeMode.Elapsed, "12:30")]
    [InlineData(OverlayTimeMode.ElapsedTotal, "12:30")]
    [InlineData(OverlayTimeMode.Remaining, "")]
    [InlineData(OverlayTimeMode.ElapsedRemaining, "12:30")]
    [InlineData(OverlayTimeMode.ElapsedTotalRemaining, "12:30")]
    public void FormatTimes_UnknownDuration_LeavesOutDurationParts(OverlayTimeMode mode, string expected)
    {
        Assert.Equal(expected, PlaybackOverlayText.FormatTimes(mode, Pos, 0));
        Assert.Equal(expected, PlaybackOverlayText.FormatTimes(mode, Pos, -1));
    }

    [Fact]
    public void FormatTimes_PositionBeyondDuration_ClampsToDuration() =>
        Assert.Equal("45:00 / 45:00 / -0:00",
            PlaybackOverlayText.FormatTimes(OverlayTimeMode.ElapsedTotalRemaining, Dur + Min, Dur));

    [Fact]
    public void FormatTimes_NegativePositionKnownDuration_ClampsToZero() =>
        Assert.Equal("0:00 / 45:00 / -45:00",
            PlaybackOverlayText.FormatTimes(OverlayTimeMode.ElapsedTotalRemaining, -5_000, Dur));

    [Fact]
    public void FormatTimes_NegativePositionUnknownDuration_ClampsToZero() =>
        Assert.Equal("0:00", PlaybackOverlayText.FormatTimes(OverlayTimeMode.Elapsed, -5_000, 0));

    [Fact]
    public void FormatTimes_UndefinedMode_IsEmpty() =>
        Assert.Equal("", PlaybackOverlayText.FormatTimes((OverlayTimeMode)99, Pos, Dur));

    private static PlaybackOverlaySettings Settings(
        OverlayTimeMode time = OverlayTimeMode.Elapsed, bool clock = true, bool battery = true) =>
        new() { Time = time, Clock = clock, Battery = battery };

    [Fact]
    public void FormatLine_AllOn_OrdersTimesClockBatteryWithTwoSpaces() =>
        Assert.Equal("12:30 / 45:00  21:05  80%",
            PlaybackOverlayText.FormatLine(Settings(OverlayTimeMode.ElapsedTotal), Pos, Dur, "21:05", 80));

    [Fact]
    public void FormatLine_TimesOff_ShowsClockAndBattery() =>
        Assert.Equal("21:05  80%",
            PlaybackOverlayText.FormatLine(Settings(OverlayTimeMode.None), Pos, Dur, "21:05", 80));

    [Fact]
    public void FormatLine_ClockOff_ShowsTimesAndBattery() =>
        Assert.Equal("12:30  80%",
            PlaybackOverlayText.FormatLine(Settings(clock: false), Pos, Dur, "21:05", 80));

    [Fact]
    public void FormatLine_BatteryOff_ShowsTimesAndClock() =>
        Assert.Equal("12:30  21:05",
            PlaybackOverlayText.FormatLine(Settings(battery: false), Pos, Dur, "21:05", 80));

    [Fact]
    public void FormatLine_NullClock_LeavesClockOut() =>
        Assert.Equal("12:30  80%", PlaybackOverlayText.FormatLine(Settings(), Pos, Dur, null, 80));

    [Fact]
    public void FormatLine_NullBattery_LeavesBatteryOut() =>
        Assert.Equal("12:30  21:05", PlaybackOverlayText.FormatLine(Settings(), Pos, Dur, "21:05", null));

    [Fact]
    public void FormatLine_NullClockAndBattery_ShowsOnlyTimes() =>
        Assert.Equal("12:30", PlaybackOverlayText.FormatLine(Settings(), Pos, Dur, null, null));

    [Theory]
    [InlineData(-5, "0%")]
    [InlineData(0, "0%")]
    [InlineData(100, "100%")]
    [InlineData(150, "100%")]
    public void FormatLine_BatteryOutOfRange_IsClamped(int level, string expected) =>
        Assert.Equal(expected,
            PlaybackOverlayText.FormatLine(Settings(OverlayTimeMode.None, clock: false), Pos, Dur, null, level));

    [Fact]
    public void FormatLine_AllOff_IsEmpty() =>
        Assert.Equal("",
            PlaybackOverlayText.FormatLine(
                Settings(OverlayTimeMode.None, clock: false, battery: false), Pos, Dur, "21:05", 80));

    [Fact]
    public void FormatLine_UnknownDuration_HasNoDanglingSeparators() =>
        Assert.Equal("21:05  80%",
            PlaybackOverlayText.FormatLine(Settings(OverlayTimeMode.Remaining), Pos, 0, "21:05", 80));

    [Fact]
    public void FormatLine_NullSettings_Throws() =>
        Assert.Throws<ArgumentNullException>(
            () => PlaybackOverlayText.FormatLine(null!, Pos, Dur, "21:05", 80));

    [Theory]
    [InlineData(0, Dur, 0.0)]
    [InlineData(Dur / 2, Dur, 0.5)]
    [InlineData(Dur, Dur, 1.0)]
    [InlineData(Dur + Min, Dur, 1.0)]
    [InlineData(-Min, Dur, 0.0)]
    [InlineData(Pos, 0, 0.0)]
    [InlineData(Pos, -1, 0.0)]
    public void Progress_Values_AreClampedShare(long pos, long dur, double expected) =>
        Assert.Equal(expected, PlaybackOverlayText.Progress(pos, dur), 6);
}
