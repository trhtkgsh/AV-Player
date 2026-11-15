using Microsoft.Extensions.Time.Testing;
using Player.Core.Logic.Session;

namespace Player.Core.Tests.Session;

public sealed class MultiPressDetectorTests
{
    private static readonly TimeSpan Ms399 = TimeSpan.FromMilliseconds(399);
    private static readonly TimeSpan Ms400 = TimeSpan.FromMilliseconds(400);

    private sealed class Rig
    {
        public Rig(TimeSpan? window = null)
        {
            Time = new FakeTimeProvider();
            Decisions = new List<MediaButtonAction>();
            Detector = new MultiPressDetector(Time, Decisions.Add, window);
        }

        public FakeTimeProvider Time { get; }

        public List<MediaButtonAction> Decisions { get; }

        public MultiPressDetector Detector { get; }

        public void Press(int count)
        {
            for (var i = 0; i < count; i++)
            {
                Detector.Press();
            }
        }
    }

    [Fact]
    public void Window_Default_Is400Ms()
    {
        var rig = new Rig();

        Assert.Equal(Ms400, rig.Detector.Window);
        Assert.Equal(Ms400, MultiPressDetector.DefaultWindow);
    }

    [Fact]
    public void Press_OnePress_NothingBeforeWindowCloses()
    {
        var rig = new Rig();

        rig.Press(1);
        rig.Time.Advance(Ms399);

        Assert.Empty(rig.Decisions);
    }

    [Fact]
    public void Press_OnePress_ReportsTogglePlayPauseWhenWindowCloses()
    {
        var rig = new Rig();

        rig.Press(1);
        rig.Time.Advance(Ms400);

        Assert.Equal(new[] { MediaButtonAction.TogglePlayPause }, rig.Decisions);
    }

    [Fact]
    public void Press_TwoPresses_NothingBeforeWindowCloses()
    {
        var rig = new Rig();

        rig.Press(2);
        rig.Time.Advance(Ms399);

        Assert.Empty(rig.Decisions);
    }

    [Fact]
    public void Press_TwoPressesInsideWindow_ReportsNextWhenWindowCloses()
    {
        var rig = new Rig();

        rig.Detector.Press();
        rig.Time.Advance(TimeSpan.FromMilliseconds(200));
        rig.Detector.Press();
        rig.Time.Advance(Ms400);

        Assert.Equal(new[] { MediaButtonAction.Next }, rig.Decisions);
    }

    [Fact]
    public void Press_ThreePresses_ReportsPreviousAtOnce()
    {
        var rig = new Rig();

        rig.Press(3);

        Assert.Equal(new[] { MediaButtonAction.Previous }, rig.Decisions);
    }

    [Fact]
    public void Press_ThreePresses_ReportsNothingMoreWhenWindowCloses()
    {
        var rig = new Rig();

        rig.Press(3);
        rig.Time.Advance(TimeSpan.FromSeconds(5));

        Assert.Single(rig.Decisions);
    }

    [Fact]
    public void Press_FourthPressAfterThree_StartsNewCount()
    {
        var rig = new Rig();

        rig.Press(4);
        Assert.Equal(new[] { MediaButtonAction.Previous }, rig.Decisions);

        rig.Time.Advance(Ms400);

        Assert.Equal(new[] { MediaButtonAction.Previous, MediaButtonAction.TogglePlayPause }, rig.Decisions);
    }

    [Fact]
    public void Press_SixPresses_ReportsPreviousTwice()
    {
        var rig = new Rig();

        rig.Press(6);
        rig.Time.Advance(TimeSpan.FromSeconds(5));

        Assert.Equal(new[] { MediaButtonAction.Previous, MediaButtonAction.Previous }, rig.Decisions);
    }

    [Fact]
    public void Press_SecondPressExactlyAtWindowEdge_WindowClosedFirstSoTwoSeparateToggles()
    {
        // Pinned: the timer fires at exactly the window (elapsed == Window is not "< Window"), so the group
        // is already decided when a press arrives at the edge. That press starts a new group.
        var rig = new Rig();

        rig.Detector.Press();
        rig.Time.Advance(Ms400);
        rig.Detector.Press();
        rig.Time.Advance(Ms400);

        Assert.Equal(new[] { MediaButtonAction.TogglePlayPause, MediaButtonAction.TogglePlayPause }, rig.Decisions);
    }

    [Fact]
    public void Press_SecondPressOneMsBeforeWindowEdge_JoinsGroupAsNext()
    {
        var rig = new Rig();

        rig.Detector.Press();
        rig.Time.Advance(Ms399);
        rig.Detector.Press();
        rig.Time.Advance(Ms400);

        Assert.Equal(new[] { MediaButtonAction.Next }, rig.Decisions);
    }

    [Fact]
    public void Press_EachPressRestartsWindow()
    {
        var rig = new Rig();

        rig.Detector.Press();
        rig.Time.Advance(TimeSpan.FromMilliseconds(300));
        rig.Detector.Press();
        rig.Time.Advance(TimeSpan.FromMilliseconds(300));
        Assert.Empty(rig.Decisions);

        rig.Time.Advance(TimeSpan.FromMilliseconds(100));

        Assert.Equal(new[] { MediaButtonAction.Next }, rig.Decisions);
    }

    [Fact]
    public void Press_CustomWindow_UsesIt()
    {
        var rig = new Rig(TimeSpan.FromMilliseconds(1000));

        rig.Press(1);
        rig.Time.Advance(TimeSpan.FromMilliseconds(999));
        Assert.Empty(rig.Decisions);

        rig.Time.Advance(TimeSpan.FromMilliseconds(1));

        Assert.Equal(new[] { MediaButtonAction.TogglePlayPause }, rig.Decisions);
        Assert.Equal(TimeSpan.FromMilliseconds(1000), rig.Detector.Window);
    }

    [Fact]
    public void Press_AfterDecision_NextGroupCountsFromZero()
    {
        var rig = new Rig();

        rig.Press(2);
        rig.Time.Advance(Ms400);
        rig.Press(1);
        rig.Time.Advance(Ms400);

        Assert.Equal(new[] { MediaButtonAction.Next, MediaButtonAction.TogglePlayPause }, rig.Decisions);
    }

    [Fact]
    public void Press_AfterDispose_ReportsNothingAndDoesNotThrow()
    {
        var rig = new Rig();
        rig.Detector.Dispose();

        var ex = Record.Exception(() => rig.Press(3));
        rig.Time.Advance(TimeSpan.FromSeconds(5));

        Assert.Null(ex);
        Assert.Empty(rig.Decisions);
    }

    [Fact]
    public void Dispose_WithPendingPress_ReportsNothing()
    {
        var rig = new Rig();

        rig.Press(1);
        rig.Detector.Dispose();
        rig.Time.Advance(TimeSpan.FromSeconds(5));

        Assert.Empty(rig.Decisions);
    }

    [Fact]
    public void Dispose_CalledTwice_DoesNotThrow()
    {
        var rig = new Rig();

        rig.Detector.Dispose();
        var ex = Record.Exception(rig.Detector.Dispose);

        Assert.Null(ex);
    }

    [Fact]
    public void Press_CallbackCalledOncePerDecision()
    {
        var rig = new Rig();

        rig.Press(1);
        rig.Time.Advance(Ms400);
        rig.Time.Advance(Ms400);
        rig.Time.Advance(Ms400);

        Assert.Single(rig.Decisions);
    }

    [Fact]
    public void Press_CallbackThrowsOnThirdPress_ExceptionReachesCallerAndNextPressStartsNewGroup()
    {
        // Pinned: a throwing callback is not swallowed on the immediate (third press) path. The count was
        // already reset under the lock, so the detector stays usable.
        var time = new FakeTimeProvider();
        var calls = new List<MediaButtonAction>();
        var shouldThrow = true;
        using var detector = new MultiPressDetector(time, action =>
        {
            calls.Add(action);
            if (shouldThrow)
            {
                throw new InvalidOperationException("boom");
            }
        });

        detector.Press();
        detector.Press();
        Assert.Throws<InvalidOperationException>(detector.Press);

        shouldThrow = false;
        detector.Press();
        time.Advance(Ms400);

        Assert.Equal(new[] { MediaButtonAction.Previous, MediaButtonAction.TogglePlayPause }, calls);
    }

    [Fact]
    public void Constructor_NullArguments_Throw()
    {
        var time = new FakeTimeProvider();

        Assert.Throws<ArgumentNullException>(() => new MultiPressDetector(null!, _ => { }));
        Assert.Throws<ArgumentNullException>(() => new MultiPressDetector(time, null!));
    }
}
