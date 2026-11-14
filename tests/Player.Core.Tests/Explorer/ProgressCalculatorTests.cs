using Player.Core.Logic.Explorer;
using Player.Core.Models;

namespace Player.Core.Tests.Explorer;

public sealed class ProgressCalculatorTests
{
    private static PlaybackState State(long positionMs, long durationMs = 1_000, bool finished = false) =>
        new(TestData.Id())
        {
            PositionMs = positionMs,
            DurationMs = durationMs,
            Finished = finished,
        };

    [Fact]
    public void Fraction_NullState_ReturnsZero()
    {
        Assert.Equal(0, ProgressCalculator.Fraction(null));
    }

    [Fact]
    public void Fraction_ZeroDuration_ReturnsZero()
    {
        Assert.Equal(0, ProgressCalculator.Fraction(State(positionMs: 500, durationMs: 0)));
    }

    [Fact]
    public void Fraction_HalfwayThrough_ReturnsHalf()
    {
        Assert.Equal(0.5, ProgressCalculator.Fraction(State(positionMs: 500, durationMs: 1_000)));
    }

    [Fact]
    public void Fraction_PositionAboveDuration_ReturnsOne()
    {
        Assert.Equal(1, ProgressCalculator.Fraction(State(positionMs: 1_500, durationMs: 1_000)));
    }

    [Fact]
    public void Fraction_NegativePosition_ReturnsZero()
    {
        Assert.Equal(0, ProgressCalculator.Fraction(State(positionMs: -200, durationMs: 1_000)));
    }

    [Fact]
    public void IsWatched_NullState_ReturnsFalse()
    {
        Assert.False(ProgressCalculator.IsWatched(null));
    }

    [Fact]
    public void IsWatched_FinishedAtPositionZero_ReturnsTrue()
    {
        Assert.True(ProgressCalculator.IsWatched(State(positionMs: 0, finished: true)));
    }

    [Fact]
    public void IsWatched_PositionExactlyAt95Percent_ReturnsFalse()
    {
        Assert.False(ProgressCalculator.IsWatched(State(positionMs: 950, durationMs: 1_000)));
    }

    [Fact]
    public void IsWatched_PositionAbove95Percent_ReturnsTrue()
    {
        Assert.True(ProgressCalculator.IsWatched(State(positionMs: 960, durationMs: 1_000)));
    }
}
