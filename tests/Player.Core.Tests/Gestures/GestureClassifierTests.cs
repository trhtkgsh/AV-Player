using Player.Core.Logic.Gestures;
using Player.Core.Models;

namespace Player.Core.Tests.GestureLogic;

public sealed class GestureClassifierTests
{
    private const GestureKind Tap = GestureKind.Tap;
    private const GestureKind DoubleTap = GestureKind.DoubleTap;
    private const GestureKind HDrag = GestureKind.HorizontalDrag;
    private const GestureKind VDrag = GestureKind.VerticalDrag;
    private const GestureKind Pinch = GestureKind.Pinch;
    private const GestureKind Pan = GestureKind.Pan;

    private const GesturePhase Began = GesturePhase.Began;
    private const GesturePhase Changed = GesturePhase.Changed;
    private const GesturePhase Ended = GesturePhase.Ended;
    private const GesturePhase Cancelled = GesturePhase.Cancelled;

    private sealed class Rig
    {
        public Rig(double width = 1000, double height = 500)
        {
            Events = new List<GestureEvent>();
            Classifier = new GestureClassifier(Events.Add) { ViewWidth = width, ViewHeight = height };
        }

        public List<GestureEvent> Events { get; }

        public GestureClassifier Classifier { get; }

        public GestureEvent Last => Events[^1];

        public void Down(double x, double y, long t = 0, int id = 1) =>
            Classifier.Process(PointerAction.Down, id, x, y, t);

        public void Move(double x, double y, long t = 0, int id = 1) =>
            Classifier.Process(PointerAction.Move, id, x, y, t);

        public void Up(double x, double y, long t = 0, int id = 1) =>
            Classifier.Process(PointerAction.Up, id, x, y, t);

        public void Cancel(long t = 0) => Classifier.Process(PointerAction.Cancel, 1, 0, 0, t);

        public void PointerDown(int id, double x, double y, long t = 0) =>
            Classifier.Process(PointerAction.PointerDown, id, x, y, t);

        public void PointerUp(int id, double x, double y, long t = 0) =>
            Classifier.Process(PointerAction.PointerUp, id, x, y, t);

        public void Tick(long t) => Classifier.Tick(t);

        public (GestureKind, GesturePhase)[] Shape() => Events.Select(e => (e.Kind, e.Phase)).ToArray();

        /// <summary>A complete tap: Down and Up at the same point.</summary>
        public void TapAt(double x, double y, long downMs, long upMs)
        {
            Down(x, y, downMs);
            Up(x, y, upMs);
        }

        /// <summary>Two fingers down at (400,250) and (600,250). Finger 1 is the first touch.</summary>
        public void StartPinch()
        {
            Down(400, 250);
            PointerDown(2, 600, 250);
        }
    }

    private static (GestureKind, GesturePhase) E(GestureKind kind, GesturePhase phase) => (kind, phase);

    private static void AssertFinite(GestureEvent e)
    {
        Assert.True(double.IsFinite(e.DeltaX), $"DeltaX {e}");
        Assert.True(double.IsFinite(e.DeltaY), $"DeltaY {e}");
        Assert.True(double.IsFinite(e.Scale), $"Scale {e}");
        Assert.True(double.IsFinite(e.FocusX), $"FocusX {e}");
        Assert.True(double.IsFinite(e.FocusY), $"FocusY {e}");
    }

    // ---------------------------------------------------------------- Tap

    [Theory]
    [InlineData(0)]
    [InlineData(10)]
    [InlineData(16)]
    public void Process_MoveWithinSlopThenUp_IsStillATap(double dx)
    {
        var rig = new Rig();
        rig.Down(500, 250, 0);
        rig.Move(500 + dx, 250, 10);
        rig.Up(500 + dx, 250, 20);

        Assert.Empty(rig.Events);
        Assert.Equal(320, rig.Classifier.NextTickMs);
        rig.Tick(320);
        Assert.Equal(new[] { E(Tap, Ended) }, rig.Shape());
    }

    [Theory]
    [InlineData(16.5)]
    [InlineData(17)]
    [InlineData(100)]
    public void Process_MoveBeyondSlop_IsADrag(double dx)
    {
        var rig = new Rig();
        rig.Down(500, 250);
        rig.Move(500 + dx, 250, 10);
        rig.Up(500 + dx, 250, 20);

        Assert.Equal(new[] { E(HDrag, Began), E(HDrag, Ended) }, rig.Shape());
    }

    [Fact]
    public void Process_MoveBeyondSlopToTheLeft_IsADrag()
    {
        var rig = new Rig();
        rig.Down(500, 250);
        rig.Move(483, 250, 10);

        Assert.Equal(new[] { E(HDrag, Began) }, rig.Shape());
    }

    [Fact]
    public void Process_CustomTouchSlop_IsUsedForTheTapDragBoundary()
    {
        var rig = new Rig();
        rig.Classifier.Options = new GestureClassifierOptions(TouchSlop: 5);
        rig.Down(500, 250);
        rig.Move(505, 250);
        Assert.Empty(rig.Events);

        rig.Move(506, 250);
        Assert.Equal(new[] { E(HDrag, Began) }, rig.Shape());
    }

    [Fact]
    public void NextTickMs_AfterTapUp_IsUpTimePlusWindow()
    {
        var rig = new Rig();
        rig.TapAt(500, 250, 100, 180);

        Assert.Equal(480, rig.Classifier.NextTickMs);
    }

    [Fact]
    public void NextTickMs_NoTouchYet_IsNull()
    {
        Assert.Null(new Rig().Classifier.NextTickMs);
    }

    [Fact]
    public void NextTickMs_WhileFirstTouchIsDown_IsDownTimePlusLongPressMs()
    {
        var rig = new Rig();
        rig.Down(500, 250, 100);

        Assert.Equal(600, rig.Classifier.NextTickMs);
    }

    [Fact]
    public void Tick_BeforeNextTickMs_ReportsNothing()
    {
        var rig = new Rig();
        rig.TapAt(500, 250, 0, 50);

        rig.Tick(349);

        Assert.Empty(rig.Events);
        Assert.Equal(350, rig.Classifier.NextTickMs);
    }

    [Theory]
    [InlineData(350)]
    [InlineData(351)]
    [InlineData(5000)]
    public void Tick_AtOrAfterNextTickMs_ReportsOneTapEnded(long now)
    {
        var rig = new Rig();
        rig.TapAt(500, 250, 0, 50);

        rig.Tick(now);

        Assert.Equal(new[] { E(Tap, Ended) }, rig.Shape());
        Assert.Null(rig.Classifier.NextTickMs);
    }

    [Fact]
    public void Tick_CalledAgainAfterTheTap_ReportsNothing()
    {
        var rig = new Rig();
        rig.TapAt(500, 250, 0, 50);
        rig.Tick(350);

        rig.Tick(351);
        rig.Tick(10_000);

        Assert.Single(rig.Events);
    }

    [Fact]
    public void Tick_WithNothingPending_ReportsNothing()
    {
        var rig = new Rig();

        rig.Tick(10_000);

        Assert.Empty(rig.Events);
    }

    [Fact]
    public void Tick_AfterTap_ReportsFocusAsFractionsOfTheView()
    {
        var rig = new Rig();
        rig.TapAt(250, 100, 0, 50);
        rig.Tick(350);

        Assert.Equal(0.25, rig.Last.FocusX, 6);
        Assert.Equal(0.2, rig.Last.FocusY, 6);
        Assert.Equal(0, rig.Last.DeltaX);
        Assert.Equal(0, rig.Last.DeltaY);
    }

    [Fact]
    public void Options_CustomDoubleTapWindow_ChangesNextTickMs()
    {
        var rig = new Rig();
        rig.Classifier.Options = new GestureClassifierOptions(DoubleTapWindowMs: 500);
        rig.TapAt(500, 250, 0, 50);

        Assert.Equal(550, rig.Classifier.NextTickMs);
    }

    // ---------------------------------------------------------------- Double tap

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(150)]
    [InlineData(299)]
    public void Process_SecondDownWithinWindow_IsOneDoubleTapAndNoTap(long gapMs)
    {
        var rig = new Rig();
        rig.TapAt(500, 250, 0, 50);
        rig.Down(520, 250, 50 + gapMs);
        rig.Up(520, 250, 50 + gapMs + 30);
        rig.Tick(100_000);

        Assert.Equal(new[] { E(DoubleTap, Ended) }, rig.Shape());
        Assert.Null(rig.Classifier.NextTickMs);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(50)]
    [InlineData(100)]
    public void Process_SecondDownWithinDistance_IsADoubleTap(double distance)
    {
        var rig = new Rig();
        rig.TapAt(300, 250, 0, 50);
        rig.TapAt(300 + distance, 250, 100, 150);

        Assert.Equal(new[] { E(DoubleTap, Ended) }, rig.Shape());
    }

    [Fact]
    public void Process_SecondDownBeyondDistanceOnYAxis_IsNotADoubleTap()
    {
        var rig = new Rig();
        rig.TapAt(300, 100, 0, 50);
        rig.Down(300, 100.5 + 100, 100);

        Assert.Equal(new[] { E(Tap, Ended) }, rig.Shape());
    }

    [Theory]
    [InlineData(300)]
    [InlineData(301)]
    [InlineData(2000)]
    public void Process_SecondDownAtOrAfterWindow_ReportsTapThenTreatsSecondTouchAsNew(long gapMs)
    {
        var rig = new Rig();
        rig.TapAt(500, 250, 0, 50);
        rig.Down(500, 250, 50 + gapMs);

        Assert.Equal(new[] { E(Tap, Ended) }, rig.Shape());

        rig.Up(500, 250, 50 + gapMs + 20);
        Assert.Equal(50 + gapMs + 20 + 300, rig.Classifier.NextTickMs);
        rig.Tick(50 + gapMs + 20 + 300);

        Assert.Equal(new[] { E(Tap, Ended), E(Tap, Ended) }, rig.Shape());
    }

    [Theory]
    [InlineData(100.5)]
    [InlineData(101)]
    [InlineData(500)]
    public void Process_SecondDownBeyondDistance_ReportsTapThenTreatsSecondTouchAsNew(double distance)
    {
        var rig = new Rig();
        rig.TapAt(300, 250, 0, 50);
        rig.Down(300 + distance, 250, 100);

        Assert.Equal(new[] { E(Tap, Ended) }, rig.Shape());

        rig.Up(300 + distance, 250, 120);
        rig.Tick(420);

        Assert.Equal(new[] { E(Tap, Ended), E(Tap, Ended) }, rig.Shape());
    }

    [Fact]
    public void Process_SecondDownBeyondDistance_FirstTapKeepsSideOfFirstTouch()
    {
        var rig = new Rig();
        rig.TapAt(100, 250, 0, 50);
        rig.Down(900, 250, 100);
        rig.Up(900, 250, 120);
        rig.Tick(420);

        Assert.Equal(ScreenSide.Left, rig.Events[0].Side);
        Assert.Equal(ScreenSide.Right, rig.Events[1].Side);
    }

    [Fact]
    public void NextTickMs_WhileSecondTouchIsDown_IsTheEarlierOfPendingTapDueAndItsLongPressTime()
    {
        var rig = new Rig();
        rig.TapAt(500, 250, 0, 50);
        rig.Down(510, 250, 100);

        // Pending tap is due at 50 + 300 = 350, the second touch's long press at 100 + 500 = 600.
        Assert.Equal(350, rig.Classifier.NextTickMs);
    }

    [Fact]
    public void NextTickMs_WhileSecondTouchIsDownAndItsLongPressIsEarlier_IsItsLongPressTime()
    {
        var rig = new Rig();
        rig.Classifier.Options = new GestureClassifierOptions(LongPressMs: 100);
        rig.TapAt(500, 250, 0, 50);
        rig.Down(510, 250, 100);

        // Long press at 100 + 100 = 200, pending tap due at 350.
        Assert.Equal(200, rig.Classifier.NextTickMs);
    }

    [Fact]
    public void Tick_WhileSecondTouchIsDownBeforeItsLongPress_ReportsNothing()
    {
        var rig = new Rig();
        rig.TapAt(500, 250, 0, 50);
        rig.Down(510, 250, 100);

        rig.Tick(599);

        Assert.Empty(rig.Events);
    }

    [Fact]
    public void Process_SecondTouchBecomesDrag_ReportsPendingTapBeforeTheDrag()
    {
        var rig = new Rig();
        rig.TapAt(500, 250, 0, 50);
        rig.Down(500, 250, 100);
        rig.Move(560, 250, 120);
        rig.Up(560, 250, 140);

        Assert.Equal(new[] { E(Tap, Ended), E(HDrag, Began), E(HDrag, Ended) }, rig.Shape());
        Assert.Null(rig.Classifier.NextTickMs);
    }

    [Fact]
    public void Tick_SecondTouchWasCancelled_ReportsThePendingTap()
    {
        var rig = new Rig();
        rig.TapAt(500, 250, 0, 50);
        rig.Down(500, 250, 100);
        rig.Cancel(120);

        Assert.Empty(rig.Events);
        Assert.Equal(350, rig.Classifier.NextTickMs);
        rig.Tick(350);

        Assert.Equal(new[] { E(Tap, Ended) }, rig.Shape());
    }

    [Fact]
    public void Process_SecondTouchBecomesPinch_ReportsPendingTapBeforeThePinch()
    {
        var rig = new Rig();
        rig.TapAt(500, 250, 0, 50);
        rig.Down(500, 250, 100);
        rig.PointerDown(2, 600, 250, 110);

        Assert.Equal(new[] { E(Tap, Ended), E(Pinch, Began) }, rig.Shape());
    }

    [Fact]
    public void Process_DoubleTapSecondTapOnTheRight_HasSideRight()
    {
        var rig = new Rig();
        rig.TapAt(460, 250, 0, 50);
        rig.TapAt(540, 250, 100, 150);

        Assert.Equal(DoubleTap, rig.Last.Kind);
        Assert.Equal(ScreenSide.Right, rig.Last.Side);
    }

    [Fact]
    public void Process_DoubleTapSecondTapOnTheLeft_HasSideLeft()
    {
        var rig = new Rig();
        rig.TapAt(540, 250, 0, 50);
        rig.TapAt(460, 250, 100, 150);

        Assert.Equal(DoubleTap, rig.Last.Kind);
        Assert.Equal(ScreenSide.Left, rig.Last.Side);
    }

    [Fact]
    public void Process_AfterDoubleTap_NextTouchIsNew()
    {
        var rig = new Rig();
        rig.TapAt(500, 250, 0, 50);
        rig.TapAt(500, 250, 100, 150);
        rig.TapAt(500, 250, 200, 250);

        Assert.Equal(550, rig.Classifier.NextTickMs);
        rig.Tick(550);
        Assert.Equal(new[] { E(DoubleTap, Ended), E(Tap, Ended) }, rig.Shape());
    }

    // ---------------------------------------------------------------- Side

    [Theory]
    [InlineData(0, ScreenSide.Left)]
    [InlineData(499, ScreenSide.Left)]
    [InlineData(500, ScreenSide.Right)]
    [InlineData(501, ScreenSide.Right)]
    [InlineData(1000, ScreenSide.Right)]
    public void Process_TapSide_DependsOnTouchDownXAgainstHalfTheWidth(double x, ScreenSide expected)
    {
        var rig = new Rig();
        rig.TapAt(x, 250, 0, 50);
        rig.Tick(350);

        Assert.Equal(expected, rig.Last.Side);
    }

    [Theory]
    [InlineData(499, ScreenSide.Left)]
    [InlineData(500, ScreenSide.Right)]
    [InlineData(501, ScreenSide.Right)]
    public void Process_LockedTapSide_DependsOnTouchDownX(double x, ScreenSide expected)
    {
        var rig = new Rig();
        rig.Classifier.Locked = true;
        rig.TapAt(x, 250, 0, 50);

        Assert.Equal(expected, rig.Last.Side);
    }

    [Fact]
    public void Process_DragSide_IsTheSideOfTheTouchDownPointNotOfTheCurrentPosition()
    {
        var rig = new Rig();
        rig.Down(499, 250);
        rig.Move(900, 250, 10);
        rig.Move(950, 250, 20);

        Assert.All(rig.Events, e => Assert.Equal(ScreenSide.Left, e.Side));
    }

    [Fact]
    public void Process_ViewWidthChange_MovesTheSideBoundary()
    {
        var rig = new Rig();
        rig.Classifier.ViewWidth = 400;
        rig.TapAt(250, 250, 0, 50);
        rig.Tick(350);

        Assert.Equal(ScreenSide.Right, rig.Last.Side);
    }

    // ---------------------------------------------------------------- Drag

    [Theory]
    [InlineData(20, 10, GestureKind.HorizontalDrag)]
    [InlineData(10, 20, GestureKind.VerticalDrag)]
    [InlineData(20, 20, GestureKind.HorizontalDrag)]
    [InlineData(-20, -20, GestureKind.HorizontalDrag)]
    [InlineData(-30, 5, GestureKind.HorizontalDrag)]
    [InlineData(5, -30, GestureKind.VerticalDrag)]
    [InlineData(0, -17, GestureKind.VerticalDrag)]
    [InlineData(17, 0, GestureKind.HorizontalDrag)]
    public void Process_DragStart_LocksTheLargerAxisAndTieGoesToHorizontal(double dx, double dy, GestureKind expected)
    {
        var rig = new Rig();
        rig.Down(500, 250);
        rig.Move(500 + dx, 250 + dy, 10);

        Assert.Equal(new[] { E(expected, Began) }, rig.Shape());
    }

    [Theory]
    [InlineData(100, 200, 900, 200, GestureKind.VerticalDrag)]
    [InlineData(200, 100, 200, 450, GestureKind.HorizontalDrag)]
    public void Process_AfterAxisLock_AxisNeverChanges(
        double startX, double startY, double farX, double farY, GestureKind expected)
    {
        var rig = new Rig();
        rig.Down(100, 100);
        rig.Move(startX, startY, 10);
        rig.Move(farX, farY, 20);
        rig.Move(farX + 1, farY + 1, 30);
        rig.Up(farX + 1, farY + 1, 40);

        Assert.All(rig.Events, e => Assert.Equal(expected, e.Kind));
        Assert.Equal(Ended, rig.Last.Phase);
    }

    [Fact]
    public void Process_DragMoves_EmitBeganChangedChangedEnded()
    {
        var rig = new Rig();
        rig.Down(100, 100);
        rig.Move(200, 100, 10);
        rig.Move(250, 100, 20);
        rig.Move(300, 100, 30);
        rig.Up(300, 100, 40);

        Assert.Equal(
            new[] { E(HDrag, Began), E(HDrag, Changed), E(HDrag, Changed), E(HDrag, Ended) },
            rig.Shape());
    }

    [Fact]
    public void Process_HorizontalDrag_DeltasAreFractionsOfTheViewSinceTouchDown()
    {
        var rig = new Rig();
        rig.Down(100, 100);
        rig.Move(200, 100, 10);

        Assert.Equal(0.1, rig.Last.DeltaX, 6);
        Assert.Equal(0.0, rig.Last.DeltaY, 6);

        rig.Move(300, 150, 20);
        Assert.Equal(0.2, rig.Last.DeltaX, 6);
        Assert.Equal(0.0, rig.Last.DeltaY);

        rig.Up(300, 150, 30);
        Assert.Equal(Ended, rig.Last.Phase);
        Assert.Equal(0.2, rig.Last.DeltaX, 6);
        Assert.Equal(0.0, rig.Last.DeltaY);
    }

    [Fact]
    public void Process_HorizontalDragMovingDiagonally_DeltaYIsExactlyZero()
    {
        var rig = new Rig();
        rig.Down(100, 100);
        rig.Move(200, 130, 10);
        rig.Move(300, 200, 20);
        rig.Move(350, 400, 30);
        rig.Up(350, 400, 40);

        Assert.Equal(new[] { E(HDrag, Began), E(HDrag, Changed), E(HDrag, Changed), E(HDrag, Ended) }, rig.Shape());
        Assert.All(rig.Events, e => Assert.Equal(0.0, e.DeltaY));
        Assert.Equal(0.25, rig.Last.DeltaX, 6);
    }

    [Fact]
    public void Process_VerticalDragMovingDiagonally_DeltaXIsExactlyZero()
    {
        var rig = new Rig();
        rig.Down(100, 100);
        rig.Move(130, 200, 10);
        rig.Move(200, 300, 20);
        rig.Move(600, 350, 30);
        rig.Up(600, 350, 40);

        Assert.Equal(new[] { E(VDrag, Began), E(VDrag, Changed), E(VDrag, Changed), E(VDrag, Ended) }, rig.Shape());
        Assert.All(rig.Events, e => Assert.Equal(0.0, e.DeltaX));
        Assert.Equal(0.5, rig.Last.DeltaY, 6);
    }

    [Fact]
    public void Process_DragMovingOffAxisAfterBegan_KeepsOffAxisDeltaZero()
    {
        var rig = new Rig();
        rig.Down(100, 100);
        rig.Move(200, 100, 10);
        rig.Move(200, 120, 20);

        Assert.All(rig.Events, e => Assert.Equal(0.0, e.DeltaY));
        Assert.Equal(0.1, rig.Last.DeltaX, 6);
    }

    [Fact]
    public void Process_VerticalDrag_DeltaYIsFractionOfViewHeight()
    {
        var rig = new Rig();
        rig.Down(100, 100);
        rig.Move(100, 250, 10);

        Assert.Equal(VDrag, rig.Last.Kind);
        Assert.Equal(0.3, rig.Last.DeltaY, 6);
        Assert.Equal(0.0, rig.Last.DeltaX, 6);
    }

    [Fact]
    public void Process_DragUpwards_HasNegativeDelta()
    {
        var rig = new Rig();
        rig.Down(100, 400);
        rig.Move(100, 300, 10);

        Assert.Equal(-0.2, rig.Last.DeltaY, 6);
    }

    [Fact]
    public void Process_DragDeltas_StartAtTouchDownNotAtTheSlopCrossing()
    {
        var rig = new Rig();
        rig.Down(100, 100);
        rig.Move(120, 100, 10);

        Assert.Equal(Began, rig.Last.Phase);
        Assert.Equal(0.02, rig.Last.DeltaX, 6);
    }

    [Fact]
    public void Process_DragMoveToSamePosition_EmitsNothing()
    {
        var rig = new Rig();
        rig.Down(100, 100);
        rig.Move(200, 100, 10);
        rig.Move(200, 100, 20);
        rig.Move(200, 100, 30);

        Assert.Equal(new[] { E(HDrag, Began) }, rig.Shape());
    }

    [Fact]
    public void Process_DragMoveToNewPositionAfterRepeat_EmitsChanged()
    {
        var rig = new Rig();
        rig.Down(100, 100);
        rig.Move(200, 100, 10);
        rig.Move(200, 100, 20);
        rig.Move(210, 100, 30);

        Assert.Equal(new[] { E(HDrag, Began), E(HDrag, Changed) }, rig.Shape());
    }

    [Fact]
    public void Process_CancelDuringDrag_EmitsCancelled()
    {
        var rig = new Rig();
        rig.Down(100, 100);
        rig.Move(200, 100, 10);
        rig.Cancel(20);

        Assert.Equal(new[] { E(HDrag, Began), E(HDrag, Cancelled) }, rig.Shape());
        Assert.Null(rig.Classifier.NextTickMs);
    }

    [Fact]
    public void Tick_AfterDrag_ReportsNoTap()
    {
        var rig = new Rig();
        rig.Down(100, 100);
        rig.Move(200, 100, 10);
        rig.Up(200, 100, 20);
        var count = rig.Events.Count;

        Assert.Null(rig.Classifier.NextTickMs);
        rig.Tick(10_000);

        Assert.Equal(count, rig.Events.Count);
        Assert.DoesNotContain(rig.Events, e => e.Kind == Tap);
    }

    [Fact]
    public void Process_MoveOfUnknownPointer_IsIgnored()
    {
        var rig = new Rig();
        rig.Down(100, 100);
        rig.Move(300, 100, 10, id: 7);

        Assert.Empty(rig.Events);
    }

    [Fact]
    public void Process_MoveWithoutTouch_IsIgnored()
    {
        var rig = new Rig();
        rig.Move(300, 100, 10);
        rig.Up(300, 100, 20);

        Assert.Empty(rig.Events);
        Assert.Null(rig.Classifier.NextTickMs);
    }

    [Fact]
    public void ProcessMove_OnePointerBeyondSlop_StartsADrag()
    {
        var rig = new Rig();
        rig.Down(100, 100);
        rig.Classifier.ProcessMove(new[] { new PointerSample(1, 200, 100) }, 10);

        Assert.Equal(new[] { E(HDrag, Began) }, rig.Shape());
        Assert.Equal(0.1, rig.Last.DeltaX, 6);
    }

    // ---------------------------------------------------------------- Pan mode

    [Fact]
    public void Process_PanModeOneFingerDrag_IsPanWithSameDeltas()
    {
        var rig = new Rig();
        rig.Classifier.PanMode = true;
        rig.Down(100, 100);
        rig.Move(200, 150, 10);
        rig.Move(300, 250, 20);
        rig.Up(300, 250, 30);

        Assert.Equal(new[] { E(Pan, Began), E(Pan, Changed), E(Pan, Ended) }, rig.Shape());
        Assert.Equal(0.1, rig.Events[0].DeltaX, 6);
        Assert.Equal(0.1, rig.Events[0].DeltaY, 6);
        Assert.Equal(0.2, rig.Events[2].DeltaX, 6);
        Assert.Equal(0.3, rig.Events[2].DeltaY, 6);
    }

    [Fact]
    public void Process_PanModeVerticalMove_IsStillPan()
    {
        var rig = new Rig();
        rig.Classifier.PanMode = true;
        rig.Down(100, 100);
        rig.Move(100, 300, 10);

        Assert.Equal(new[] { E(Pan, Began) }, rig.Shape());
    }

    [Fact]
    public void Process_PanModeTurnedOffDuringPan_StaysPan()
    {
        var rig = new Rig();
        rig.Classifier.PanMode = true;
        rig.Down(100, 100);
        rig.Move(200, 100, 10);
        rig.Classifier.PanMode = false;
        rig.Move(300, 100, 20);

        Assert.Equal(new[] { E(Pan, Began), E(Pan, Changed) }, rig.Shape());
    }

    [Fact]
    public void Process_PanModeTurnedOnDuringDrag_StaysADrag()
    {
        var rig = new Rig();
        rig.Down(100, 100);
        rig.Move(200, 100, 10);
        rig.Classifier.PanMode = true;
        rig.Move(300, 100, 20);

        Assert.Equal(new[] { E(HDrag, Began), E(HDrag, Changed) }, rig.Shape());
    }

    [Fact]
    public void Process_PanModeTap_IsStillATap()
    {
        var rig = new Rig();
        rig.Classifier.PanMode = true;
        rig.TapAt(500, 250, 0, 50);
        rig.Tick(350);

        Assert.Equal(new[] { E(Tap, Ended) }, rig.Shape());
    }

    // ---------------------------------------------------------------- Pinch

    [Fact]
    public void Process_SecondPointerDown_BeginsPinchWithScaleOne()
    {
        var rig = new Rig();
        rig.StartPinch();

        Assert.Equal(new[] { E(Pinch, Began) }, rig.Shape());
        Assert.Equal(1.0, rig.Last.Scale, 6);
        Assert.Equal(0.0, rig.Last.DeltaX, 6);
        Assert.Equal(0.0, rig.Last.DeltaY, 6);
        Assert.Equal(ScreenSide.Left, rig.Last.Side);
    }

    [Fact]
    public void Process_PinchBegan_FocusIsTheMidpointAsFractions()
    {
        var rig = new Rig();
        rig.Down(400, 200);
        rig.PointerDown(2, 600, 300);

        Assert.Equal(0.5, rig.Last.FocusX, 6);
        Assert.Equal(0.5, rig.Last.FocusY, 6);
    }

    [Theory]
    [InlineData(800, 2.0, 0.6)]
    [InlineData(500, 0.5, 0.45)]
    [InlineData(600, 1.0, 0.5)]
    public void Process_PinchMove_ScaleIsCurrentOverStartDistanceAndFocusFollowsMidpoint(
        double secondX, double expectedScale, double expectedFocusX)
    {
        var rig = new Rig();
        rig.StartPinch();
        rig.Move(secondX, 250, 10, id: 2);

        var last = rig.Last;
        Assert.Equal(Pinch, last.Kind);
        Assert.Equal(expectedScale, last.Scale, 6);
        Assert.Equal(expectedFocusX, last.FocusX, 6);
        Assert.Equal(0.5, last.FocusY, 6);
    }

    [Fact]
    public void ProcessMove_BothPointers_UpdatesPinchOnce()
    {
        var rig = new Rig();
        rig.StartPinch();
        rig.Classifier.ProcessMove(new[] { new PointerSample(1, 300, 250), new PointerSample(2, 700, 250) }, 10);

        Assert.Equal(new[] { E(Pinch, Began), E(Pinch, Changed) }, rig.Shape());
        Assert.Equal(2.0, rig.Last.Scale, 6);
        Assert.Equal(0.5, rig.Last.FocusX, 6);
    }

    [Fact]
    public void Process_PinchMoveToSamePosition_EmitsNothing()
    {
        var rig = new Rig();
        rig.StartPinch();
        rig.Move(600, 250, 10, id: 2);

        Assert.Equal(new[] { E(Pinch, Began) }, rig.Shape());
    }

    [Fact]
    public void Process_DragInProgressWhenSecondPointerComesDown_IsCancelledThenPinchBegins()
    {
        var rig = new Rig();
        rig.Down(100, 100);
        rig.Move(200, 100, 10);
        rig.PointerDown(2, 400, 100, 20);

        Assert.Equal(new[] { E(HDrag, Began), E(HDrag, Cancelled), E(Pinch, Began) }, rig.Shape());
        Assert.Equal(1.0, rig.Last.Scale, 6);
    }

    [Fact]
    public void Process_SecondPointerOnCandidate_NeedsNoMovementToBeginPinch()
    {
        var rig = new Rig();
        rig.Down(100, 100);
        rig.PointerDown(2, 150, 100, 5);

        Assert.Equal(new[] { E(Pinch, Began) }, rig.Shape());
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public void Process_OnePointerLifts_PinchEndsThenRemainingFingerPansFromZero(int liftedId)
    {
        var rig = new Rig();
        rig.StartPinch();
        var liftedX = liftedId == 1 ? 400 : 600;
        rig.PointerUp(liftedId, liftedX, 250, 10);

        Assert.Equal(new[] { E(Pinch, Began), E(Pinch, Ended), E(Pan, Began) }, rig.Shape());
        Assert.Equal(0.0, rig.Last.DeltaX, 6);
        Assert.Equal(0.0, rig.Last.DeltaY, 6);
    }

    [Fact]
    public void Process_PanAfterPinch_ChangesOnMoveAndEndsOnUp()
    {
        var rig = new Rig();
        rig.StartPinch();
        rig.PointerUp(2, 600, 250, 10);
        rig.Move(450, 300, 20);

        Assert.Equal(Pan, rig.Last.Kind);
        Assert.Equal(Changed, rig.Last.Phase);
        Assert.Equal(0.05, rig.Last.DeltaX, 6);
        Assert.Equal(0.1, rig.Last.DeltaY, 6);

        rig.Up(450, 300, 30);

        Assert.Equal(Pan, rig.Last.Kind);
        Assert.Equal(Ended, rig.Last.Phase);
    }

    [Fact]
    public void Process_PanAfterPinch_StartsAtTheRemainingFingerPosition()
    {
        var rig = new Rig();
        rig.StartPinch();
        rig.PointerUp(1, 400, 250, 10);
        rig.Move(700, 250, 20, id: 2);

        Assert.Equal(0.1, rig.Last.DeltaX, 6);
    }

    [Fact]
    public void Tick_AfterPinch_ReportsNoTapAndNoDragFollows()
    {
        var rig = new Rig();
        rig.StartPinch();
        rig.PointerUp(2, 600, 250, 10);
        rig.Move(450, 300, 20);
        rig.Move(900, 300, 30);
        rig.Up(900, 300, 40);
        rig.Tick(10_000);

        Assert.Null(rig.Classifier.NextTickMs);
        Assert.All(rig.Events, e => Assert.True(e.Kind is Pinch or Pan, $"unexpected {e.Kind}"));
    }

    [Fact]
    public void Process_PinchEndedByUpOfLastPointer_NoTap()
    {
        var rig = new Rig();
        rig.StartPinch();
        rig.Up(400, 250, 10);
        rig.Tick(10_000);

        Assert.Equal(new[] { E(Pinch, Began), E(Pinch, Ended) }, rig.Shape());
        Assert.Null(rig.Classifier.NextTickMs);
    }

    [Fact]
    public void Process_NewSecondPointerDuringPan_CancelsPanAndBeginsNewPinch()
    {
        var rig = new Rig();
        rig.StartPinch();
        rig.PointerUp(2, 600, 250, 10);
        rig.PointerDown(3, 600, 250, 20);

        Assert.Equal(
            new[] { E(Pinch, Began), E(Pinch, Ended), E(Pan, Began), E(Pan, Cancelled), E(Pinch, Began) },
            rig.Shape());
        Assert.Equal(1.0, rig.Last.Scale, 6);

        rig.Move(800, 250, 30, id: 3);
        Assert.Equal(Changed, rig.Last.Phase);
        Assert.Equal(Pinch, rig.Last.Kind);
        Assert.Equal(2.0, rig.Last.Scale, 6);
    }

    [Fact]
    public void Process_ThirdPointerDuringPinch_IsIgnored()
    {
        var rig = new Rig();
        rig.StartPinch();
        rig.PointerDown(3, 800, 250, 10);
        rig.Move(900, 250, 20, id: 3);
        rig.PointerUp(3, 900, 250, 30);

        Assert.Equal(new[] { E(Pinch, Began) }, rig.Shape());

        rig.Move(800, 250, 40, id: 2);
        Assert.Equal(2.0, rig.Last.Scale, 6);
    }

    [Fact]
    public void Process_CancelDuringPinch_EmitsPinchCancelled()
    {
        var rig = new Rig();
        rig.StartPinch();
        rig.Move(800, 250, 10, id: 2);
        rig.Cancel(20);

        Assert.Equal(new[] { E(Pinch, Began), E(Pinch, Changed), E(Pinch, Cancelled) }, rig.Shape());
        Assert.Null(rig.Classifier.NextTickMs);
    }

    // ---------------------------------------------------------------- Locked

    [Fact]
    public void Process_LockedTap_IsReportedAtOnceOnUpAndNothingIsPending()
    {
        var rig = new Rig();
        rig.Classifier.Locked = true;
        rig.Down(250, 100, 0);
        Assert.Empty(rig.Events);

        rig.Up(250, 100, 50);

        Assert.Equal(new[] { E(Tap, Ended) }, rig.Shape());
        Assert.Equal(0.25, rig.Last.FocusX, 6);
        Assert.Equal(0.2, rig.Last.FocusY, 6);
        Assert.Null(rig.Classifier.NextTickMs);
        rig.Tick(10_000);
        Assert.Single(rig.Events);
    }

    [Fact]
    public void Process_LockedTwoQuickTaps_AreTwoTapsAndNoDoubleTap()
    {
        var rig = new Rig();
        rig.Classifier.Locked = true;
        rig.TapAt(500, 250, 0, 50);
        rig.TapAt(500, 250, 100, 150);

        Assert.Equal(new[] { E(Tap, Ended), E(Tap, Ended) }, rig.Shape());
        Assert.Null(rig.Classifier.NextTickMs);
    }

    [Fact]
    public void Process_LockedMoveBeyondSlop_ReportsNothing()
    {
        var rig = new Rig();
        rig.Classifier.Locked = true;
        rig.Down(100, 100);
        rig.Move(300, 100, 10);
        rig.Move(500, 300, 20);
        rig.Up(500, 300, 30);

        Assert.Empty(rig.Events);
        Assert.Null(rig.Classifier.NextTickMs);
        rig.Tick(10_000);
        Assert.Empty(rig.Events);
    }

    [Fact]
    public void Process_LockedMoveWithinSlop_IsStillATap()
    {
        var rig = new Rig();
        rig.Classifier.Locked = true;
        rig.Down(100, 100);
        rig.Move(110, 100, 10);
        rig.Up(110, 100, 20);

        Assert.Equal(new[] { E(Tap, Ended) }, rig.Shape());
    }

    [Fact]
    public void Process_LockedSecondPointer_ReportsNothing()
    {
        var rig = new Rig();
        rig.Classifier.Locked = true;
        rig.Down(100, 100);
        rig.PointerDown(2, 300, 100, 5);
        rig.Move(500, 100, 10, id: 2);
        rig.PointerUp(2, 500, 100, 15);
        rig.Up(100, 100, 20);

        Assert.Empty(rig.Events);
        Assert.Null(rig.Classifier.NextTickMs);
    }

    [Fact]
    public void Process_PendingTapBeforeLock_IsFlushedAtTheNextDown()
    {
        var rig = new Rig();
        rig.TapAt(500, 250, 0, 50);
        rig.Classifier.Locked = true;
        rig.Down(500, 250, 100);

        Assert.Equal(new[] { E(Tap, Ended) }, rig.Shape());
        Assert.Null(rig.Classifier.NextTickMs);

        rig.Up(500, 250, 120);
        Assert.Equal(new[] { E(Tap, Ended), E(Tap, Ended) }, rig.Shape());
    }

    [Fact]
    public void Process_LockedAfterTouchDown_DoesNotChangeThatTouch()
    {
        var rig = new Rig();
        rig.Down(500, 250, 0);
        rig.Classifier.Locked = true;
        rig.Up(500, 250, 50);

        Assert.Empty(rig.Events);
        Assert.Equal(350, rig.Classifier.NextTickMs);
    }

    [Fact]
    public void Process_UnlockedAfterLockedTouchDown_StillLockedForThatTouch()
    {
        var rig = new Rig();
        rig.Classifier.Locked = true;
        rig.Down(100, 100, 0);
        rig.Classifier.Locked = false;
        rig.Move(300, 100, 10);

        Assert.Empty(rig.Events);
    }

    // ---------------------------------------------------------------- Long press

    private const GestureKind LongPress = GestureKind.LongPress;

    [Theory]
    [InlineData(500)]
    [InlineData(501)]
    [InlineData(5000)]
    public void Tick_AtOrAfterLongPressTime_ReportsLongPressBeganOnce(long now)
    {
        var rig = new Rig();
        rig.Down(250, 100, 0);

        rig.Tick(now);
        rig.Tick(now + 1000);

        Assert.Equal(new[] { E(LongPress, Began) }, rig.Shape());
    }

    [Theory]
    [InlineData(250, 100, ScreenSide.Left, 0.25, 0.2)]
    [InlineData(750, 400, ScreenSide.Right, 0.75, 0.8)]
    public void Tick_LongPressBegan_HasSideAndFocusOfTheTouchDownPoint(
        double x, double y, ScreenSide side, double focusX, double focusY)
    {
        var rig = new Rig();
        rig.Down(x, y, 1000);
        rig.Move(x + 5, y - 5, 1100);

        rig.Tick(1500);

        Assert.Equal(side, rig.Last.Side);
        Assert.Equal(focusX, rig.Last.FocusX, 6);
        Assert.Equal(focusY, rig.Last.FocusY, 6);
        Assert.Equal(1.0, rig.Last.Scale);
        Assert.Equal(0, rig.Last.DeltaX);
        Assert.Equal(0, rig.Last.DeltaY);
    }

    [Fact]
    public void Tick_BeforeLongPressTime_ReportsNothing()
    {
        var rig = new Rig();
        rig.Down(500, 250, 0);

        rig.Tick(499);

        Assert.Empty(rig.Events);
        Assert.Equal(500, rig.Classifier.NextTickMs);
    }

    [Fact]
    public void NextTickMs_AfterLongPressBegan_IsNull()
    {
        var rig = new Rig();
        rig.Down(500, 250, 0);
        rig.Tick(500);

        Assert.Null(rig.Classifier.NextTickMs);
    }

    [Fact]
    public void NextTickMs_AfterLongPressEnded_IsNull()
    {
        var rig = new Rig();
        rig.Down(500, 250, 0);
        rig.Tick(500);
        rig.Up(500, 250, 900);

        Assert.Null(rig.Classifier.NextTickMs);
    }

    [Fact]
    public void Process_UpAfterLongPressBegan_ReportsLongPressEndedAndNoTap()
    {
        var rig = new Rig();
        rig.Down(250, 100, 0);
        rig.Tick(500);
        rig.Up(255, 100, 800);
        rig.Tick(100_000);

        Assert.Equal(new[] { E(LongPress, Began), E(LongPress, Ended) }, rig.Shape());
        Assert.Equal(ScreenSide.Left, rig.Last.Side);
        Assert.Equal(0.25, rig.Last.FocusX, 6);
        Assert.Equal(0.2, rig.Last.FocusY, 6);
        Assert.Equal(1.0, rig.Last.Scale);
        Assert.Equal(0, rig.Last.DeltaX);
        Assert.Equal(0, rig.Last.DeltaY);
    }

    [Fact]
    public void Process_UpAfterLongPressBeganAndAfterTheDoubleTapWindow_ReportsLongPressEndedAndNoTap()
    {
        var rig = new Rig();
        rig.Down(500, 250, 0);
        rig.Tick(500);
        rig.Up(500, 250, 5000);
        rig.Tick(10_000);

        Assert.Equal(new[] { E(LongPress, Began), E(LongPress, Ended) }, rig.Shape());
        Assert.Null(rig.Classifier.NextTickMs);
    }

    [Theory]
    [InlineData(10)]
    [InlineData(16)]
    [InlineData(200)]
    public void Process_MoveAfterLongPressBegan_ReportsNothing(double dx)
    {
        var rig = new Rig();
        rig.Down(500, 250, 0);
        rig.Tick(500);

        rig.Move(500 + dx, 250, 600);

        Assert.Equal(new[] { E(LongPress, Began) }, rig.Shape());
    }

    [Fact]
    public void Process_MoveBeyondSlopAfterLongPressBegan_ThenUp_StillEndsTheLongPress()
    {
        var rig = new Rig();
        rig.Down(500, 250, 0);
        rig.Tick(500);
        rig.Move(700, 250, 600);
        rig.Up(700, 250, 700);

        Assert.Equal(new[] { E(LongPress, Began), E(LongPress, Ended) }, rig.Shape());
    }

    [Fact]
    public void Process_CancelAfterLongPressBegan_ReportsCancelled()
    {
        var rig = new Rig();
        rig.Down(500, 250, 0);
        rig.Tick(500);
        rig.Cancel(600);

        Assert.Equal(new[] { E(LongPress, Began), E(LongPress, Cancelled) }, rig.Shape());
        Assert.Null(rig.Classifier.NextTickMs);
    }

    [Fact]
    public void Process_SecondPointerAfterLongPressBegan_CancelsItAndBeginsPinch()
    {
        var rig = new Rig();
        rig.Down(400, 250, 0);
        rig.Tick(500);
        rig.PointerDown(2, 600, 250, 700);

        Assert.Equal(
            new[] { E(LongPress, Began), E(LongPress, Cancelled), E(Pinch, Began) },
            rig.Shape());
    }

    [Fact]
    public void Process_PinchAfterLongPress_EndsCleanlyAndNextTouchIsNew()
    {
        var rig = new Rig();
        rig.Down(400, 250, 0);
        rig.Tick(500);
        rig.PointerDown(2, 600, 250, 700);
        rig.PointerUp(2, 600, 250, 800);
        rig.Up(400, 250, 900);
        rig.TapAt(500, 250, 2000, 2050);
        rig.Tick(2350);

        Assert.Equal(
            new[]
            {
                E(LongPress, Began), E(LongPress, Cancelled), E(Pinch, Began), E(Pinch, Ended),
                E(Pan, Began), E(Pan, Ended), E(Tap, Ended),
            },
            rig.Shape());
    }

    [Fact]
    public void Process_MoveInsideSlopAtOrAfterDueTimeWithoutTick_ReportsLongPressBeganFirst()
    {
        var rig = new Rig();
        rig.Down(500, 250, 0);

        rig.Move(505, 250, 700);

        Assert.Equal(new[] { E(LongPress, Began) }, rig.Shape());
    }

    [Fact]
    public void Process_UpAtOrAfterDueTimeWithoutTick_ReportsLongPressBeganThenEnded()
    {
        var rig = new Rig();
        rig.Down(500, 250, 0);

        rig.Up(500, 250, 500);

        Assert.Equal(new[] { E(LongPress, Began), E(LongPress, Ended) }, rig.Shape());
        Assert.Null(rig.Classifier.NextTickMs);
    }

    [Fact]
    public void Process_CancelAtOrAfterDueTimeWithoutTick_ReportsLongPressBeganThenCancelled()
    {
        var rig = new Rig();
        rig.Down(500, 250, 0);

        rig.Cancel(700);

        Assert.Equal(new[] { E(LongPress, Began), E(LongPress, Cancelled) }, rig.Shape());
    }

    [Fact]
    public void Process_PointerDownAtOrAfterDueTimeWithoutTick_ReportsLongPressBeganCancelledThenPinchBegan()
    {
        var rig = new Rig();
        rig.Down(400, 250, 0);

        rig.PointerDown(2, 600, 250, 500);

        Assert.Equal(
            new[] { E(LongPress, Began), E(LongPress, Cancelled), E(Pinch, Began) },
            rig.Shape());
    }

    [Fact]
    public void Process_PointerDownBeforeDueTime_IsAPinchWithoutLongPress()
    {
        var rig = new Rig();
        rig.Down(400, 250, 0);

        rig.PointerDown(2, 600, 250, 499);
        rig.Tick(10_000);

        Assert.Equal(new[] { E(Pinch, Began) }, rig.Shape());
    }

    [Fact]
    public void Process_PassedSlopBeforeDueTime_IsADragAndNeverALongPress()
    {
        var rig = new Rig();
        rig.Down(500, 250, 0);
        rig.Move(540, 250, 100);
        rig.Tick(500);
        rig.Tick(10_000);
        rig.Up(540, 250, 2000);

        Assert.Equal(new[] { E(HDrag, Began), E(HDrag, Ended) }, rig.Shape());
    }

    [Fact]
    public void NextTickMs_AfterDragBegan_IsNull()
    {
        var rig = new Rig();
        rig.Down(500, 250, 0);
        rig.Move(540, 250, 100);

        Assert.Null(rig.Classifier.NextTickMs);
    }

    [Fact]
    public void Process_PassedSlopAndReturnedBeforeDueTime_StaysADragAndNeverALongPress()
    {
        var rig = new Rig();
        rig.Down(500, 250, 0);
        rig.Move(540, 250, 100);
        rig.Move(500, 250, 200);
        rig.Tick(600);

        Assert.Equal(new[] { E(HDrag, Began), E(HDrag, Changed) }, rig.Shape());
    }

    [Fact]
    public void Process_ReleasedBeforeDueTime_IsATapAndNeverALongPress()
    {
        var rig = new Rig();
        rig.Down(500, 250, 0);
        rig.Up(500, 250, 499);
        rig.Tick(499 + 300);

        Assert.Equal(new[] { E(Tap, Ended) }, rig.Shape());
    }

    [Fact]
    public void Process_PendingTapBeforeLongSecondTouch_IsReportedBeforeLongPressBegan()
    {
        var rig = new Rig();
        rig.TapAt(500, 250, 0, 50);
        rig.Down(500, 250, 100);

        rig.Tick(600);

        Assert.Equal(new[] { E(Tap, Ended), E(LongPress, Began) }, rig.Shape());
    }

    [Fact]
    public void Process_LongSecondTouchReleased_IsNotADoubleTap()
    {
        var rig = new Rig();
        rig.TapAt(500, 250, 0, 50);
        rig.Down(500, 250, 100);
        rig.Tick(600);
        rig.Up(500, 250, 900);
        rig.Tick(100_000);

        Assert.Equal(
            new[] { E(Tap, Ended), E(LongPress, Began), E(LongPress, Ended) },
            rig.Shape());
    }

    [Fact]
    public void Process_LongPressAfterEarlierTapWithoutSecondTouch_DoesNotRepeatThePendingTap()
    {
        var rig = new Rig();
        rig.TapAt(100, 250, 0, 50);
        rig.Tick(350);
        rig.Down(100, 250, 1000);
        rig.Tick(1500);

        Assert.Equal(new[] { E(Tap, Ended), E(LongPress, Began) }, rig.Shape());
    }

    [Fact]
    public void Process_LockedHeldTouch_IsATapOnUpAndNoLongPress()
    {
        var rig = new Rig();
        rig.Classifier.Locked = true;
        rig.Down(500, 250, 0);

        rig.Tick(500);
        rig.Tick(5000);
        Assert.Empty(rig.Events);

        rig.Up(500, 250, 6000);

        Assert.Equal(new[] { E(Tap, Ended) }, rig.Shape());
    }

    [Fact]
    public void NextTickMs_LockedTouchIsDown_IsNull()
    {
        var rig = new Rig();
        rig.Classifier.Locked = true;
        rig.Down(500, 250, 0);

        Assert.Null(rig.Classifier.NextTickMs);
    }

    [Fact]
    public void Reset_DuringLongPress_EmitsNothingAndNextTouchStartsClean()
    {
        var rig = new Rig();
        rig.Down(500, 250, 0);
        rig.Tick(500);
        rig.Events.Clear();

        rig.Classifier.Reset();
        rig.Up(500, 250, 700);
        rig.Tick(10_000);

        Assert.Empty(rig.Events);
        Assert.Null(rig.Classifier.NextTickMs);

        rig.Down(100, 100, 2000);
        rig.Tick(2500);
        Assert.Equal(new[] { E(LongPress, Began) }, rig.Shape());
    }

    [Fact]
    public void Reset_BeforeLongPressTime_NeverReportsALongPress()
    {
        var rig = new Rig();
        rig.Down(500, 250, 0);

        rig.Classifier.Reset();
        rig.Tick(10_000);

        Assert.Empty(rig.Events);
        Assert.Null(rig.Classifier.NextTickMs);
    }

    [Theory]
    [InlineData(200)]
    [InlineData(1000)]
    public void Options_CustomLongPressMs_IsUsedForTheLongPressTime(long longPressMs)
    {
        var rig = new Rig();
        rig.Classifier.Options = new GestureClassifierOptions(LongPressMs: longPressMs);
        rig.Down(500, 250, 100);

        Assert.Equal(100 + longPressMs, rig.Classifier.NextTickMs);
        rig.Tick(100 + longPressMs - 1);
        Assert.Empty(rig.Events);

        rig.Tick(100 + longPressMs);
        Assert.Equal(new[] { E(LongPress, Began) }, rig.Shape());
    }

    [Fact]
    public void Options_CustomTouchSlop_IsUsedForTheLongPressBoundary()
    {
        var rig = new Rig();
        rig.Classifier.Options = new GestureClassifierOptions(TouchSlop: 5);
        rig.Down(500, 250, 0);
        rig.Move(505, 250, 100);
        rig.Tick(500);

        Assert.Equal(new[] { E(LongPress, Began) }, rig.Shape());
    }

    // ---------------------------------------------------------------- Other

    [Fact]
    public void Process_DownWhileDragIsActive_CancelsTheDragFirst()
    {
        var rig = new Rig();
        rig.Down(100, 100);
        rig.Move(200, 100, 10);
        rig.Down(300, 300, 20, id: 2);

        Assert.Equal(new[] { E(HDrag, Began), E(HDrag, Cancelled) }, rig.Shape());

        rig.Up(300, 300, 30, id: 2);
        Assert.Equal(330, rig.Classifier.NextTickMs);
    }

    [Fact]
    public void Process_DownWhilePinchIsActive_CancelsThePinchFirst()
    {
        var rig = new Rig();
        rig.StartPinch();
        rig.Down(300, 300, 20, id: 5);

        Assert.Equal(new[] { E(Pinch, Began), E(Pinch, Cancelled) }, rig.Shape());
    }

    [Fact]
    public void Process_DownAfterCancel_StartsAFreshTouch()
    {
        var rig = new Rig();
        rig.Down(100, 100);
        rig.Cancel(5);
        rig.TapAt(100, 100, 100, 150);
        rig.Tick(450);

        Assert.Equal(new[] { E(Tap, Ended) }, rig.Shape());
    }

    [Fact]
    public void Process_CancelWithNoTouch_EmitsNothing()
    {
        var rig = new Rig();
        rig.Cancel();

        Assert.Empty(rig.Events);
    }

    // ---------------------------------------------------------------- Reset

    [Fact]
    public void Reset_AfterTap_DropsThePendingTap()
    {
        var rig = new Rig();
        rig.TapAt(500, 250, 0, 50);

        rig.Classifier.Reset();

        Assert.Null(rig.Classifier.NextTickMs);
        rig.Tick(10_000);
        Assert.Empty(rig.Events);
    }

    [Fact]
    public void Reset_AfterTap_NextQuickTouchIsNotADoubleTap()
    {
        var rig = new Rig();
        rig.TapAt(500, 250, 0, 50);
        rig.Classifier.Reset();
        rig.TapAt(500, 250, 100, 150);

        Assert.Empty(rig.Events);
        Assert.Equal(450, rig.Classifier.NextTickMs);
        rig.Tick(450);
        Assert.Equal(new[] { E(Tap, Ended) }, rig.Shape());
    }

    [Fact]
    public void Reset_DuringDrag_EmitsNothingAndNextDownStartsCleanGesture()
    {
        var rig = new Rig();
        rig.Down(100, 100);
        rig.Move(200, 100, 10);
        rig.Events.Clear();

        rig.Classifier.Reset();
        Assert.Empty(rig.Events);

        rig.Move(300, 100, 20);
        rig.Up(300, 100, 30);
        Assert.Empty(rig.Events);
        Assert.Null(rig.Classifier.NextTickMs);

        rig.Down(100, 400, 40);
        rig.Move(100, 300, 50);
        Assert.Equal(new[] { E(VDrag, Began) }, rig.Shape());
        Assert.Equal(-0.2, rig.Last.DeltaY, 6);
    }

    [Fact]
    public void Reset_DuringPinch_EmitsNothingAndNextDownStartsCleanGesture()
    {
        var rig = new Rig();
        rig.StartPinch();
        rig.Move(800, 250, 10, id: 2);
        rig.Events.Clear();

        rig.Classifier.Reset();
        Assert.Empty(rig.Events);

        rig.Move(900, 250, 20, id: 2);
        rig.PointerUp(2, 900, 250, 25);
        rig.Up(400, 250, 30);
        Assert.Empty(rig.Events);

        rig.Down(100, 100, 40);
        rig.Move(200, 100, 50);
        Assert.Equal(new[] { E(HDrag, Began) }, rig.Shape());
    }

    [Fact]
    public void Reset_WithNothingActive_EmitsNothing()
    {
        var rig = new Rig();

        rig.Classifier.Reset();

        Assert.Empty(rig.Events);
        Assert.Null(rig.Classifier.NextTickMs);
    }

    [Fact]
    public void Reset_WhileSecondTouchIsDown_DropsPendingTapAndGivesNoDoubleTap()
    {
        var rig = new Rig();
        rig.TapAt(500, 250, 0, 50);
        rig.Down(500, 250, 100);
        rig.Classifier.Reset();
        rig.Up(500, 250, 120);
        rig.Tick(10_000);

        Assert.Empty(rig.Events);
        Assert.Null(rig.Classifier.NextTickMs);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(0, 500)]
    [InlineData(1000, 0)]
    [InlineData(-5, -5)]
    public void Process_ViewSizeZeroOrNegative_DoesNotThrowAndValuesAreFinite(double width, double height)
    {
        var rig = new Rig(width, height);
        rig.Down(10, 10);
        rig.Move(100, 10, 10);
        rig.Move(100, 80, 20);
        rig.PointerDown(2, 200, 200, 30);
        rig.Move(400, 400, 40, id: 2);
        rig.PointerUp(1, 100, 80, 50);
        rig.Move(300, 300, 60, id: 2);
        rig.Up(300, 300, 70, id: 2);
        rig.TapAt(10, 10, 100, 110);
        rig.Tick(1000);

        Assert.NotEmpty(rig.Events);
        Assert.All(rig.Events, AssertFinite);
    }

    [Fact]
    public void Process_NonPinchKinds_HaveScaleOne()
    {
        var rig = new Rig();
        rig.TapAt(500, 250, 0, 50);
        rig.Tick(350);

        rig.TapAt(500, 250, 1000, 1050);
        rig.TapAt(500, 250, 1100, 1150);

        rig.Down(100, 100, 2000);
        rig.Move(200, 100, 2010);
        rig.Up(200, 100, 2020);

        rig.Down(100, 100, 3000);
        rig.Move(100, 300, 3010);
        rig.Up(100, 300, 3020);

        rig.Classifier.PanMode = true;
        rig.Down(100, 100, 4000);
        rig.Move(200, 100, 4010);
        rig.Up(200, 100, 4020);

        rig.Classifier.PanMode = false;
        rig.StartPinch();
        rig.PointerUp(2, 600, 250, 5000);
        rig.Move(450, 250, 5010);
        rig.Up(450, 250, 5020);

        var nonPinch = rig.Events.Where(e => e.Kind != Pinch).ToList();
        var kinds = nonPinch.Select(e => e.Kind).Distinct().ToHashSet();
        Assert.Equal(new[] { Tap, DoubleTap, HDrag, VDrag, Pan }.ToHashSet(), kinds);
        Assert.All(nonPinch, e => Assert.Equal(1.0, e.Scale));
    }

    [Fact]
    public void Constructor_NullSink_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new GestureClassifier(null!));
    }

    [Fact]
    public void Constructor_DefaultOptions_AreTheDocumentedThresholds()
    {
        var classifier = new GestureClassifier(_ => { });

        Assert.Equal(16, classifier.Options.TouchSlop);
        Assert.Equal(300, classifier.Options.DoubleTapWindowMs);
        Assert.Equal(100, classifier.Options.DoubleTapDistance);
        Assert.Equal(500, classifier.Options.LongPressMs);
    }

    // ---------------------------------------------------------------- Invariant

    [Fact]
    public void Process_RandomEventSequences_EveryBeganIsClosedOnceBeforeTheNextBegan()
    {
        var random = new Random(20260515);
        var runsWithLongPress = 0;
        for (var run = 0; run < 400; run++)
        {
            var rig = new Rig();
            var script = new List<string>();
            long now = 0;
            double x = 500, y = 250;

            for (var step = 0; step < 40; step++)
            {
                now += random.Next(0, 200);
                if (random.Next(2) == 0)
                {
                    x = random.Next(0, 1001);
                    y = random.Next(0, 501);
                }
                else
                {
                    x = Math.Clamp(x + random.Next(-40, 41), 0, 1000);
                    y = Math.Clamp(y + random.Next(-40, 41), 0, 500);
                }

                var id = random.Next(1, 4);
                var pick = random.Next(100);
                script.Add($"{pick}:{id}@{x},{y}t{now}");
                if (pick < 12) rig.Classifier.Process(PointerAction.Down, id, x, y, now);
                else if (pick < 22) rig.Classifier.Process(PointerAction.PointerDown, id, x, y, now);
                else if (pick < 50) rig.Classifier.Process(PointerAction.Move, id, x, y, now);
                else if (pick < 58)
                {
                    var samples = new[] { new PointerSample(1, x, y), new PointerSample(2, y, x), new PointerSample(3, x, x / 2) };
                    rig.Classifier.ProcessMove(samples.AsSpan(0, random.Next(1, 4)), now);
                }
                else if (pick < 68) rig.Classifier.Process(PointerAction.PointerUp, id, x, y, now);
                else if (pick < 78) rig.Classifier.Process(PointerAction.Up, id, x, y, now);
                else if (pick < 82) rig.Classifier.Process(PointerAction.Cancel, id, x, y, now);
                else if (pick < 90) rig.Classifier.Tick(now);
                else if (pick < 95) rig.Classifier.PanMode = !rig.Classifier.PanMode;
                else rig.Classifier.Locked = !rig.Classifier.Locked;
            }

            rig.Cancel(now);
            AssertBalanced(rig.Events, run, script);
            if (rig.Events.Any(e => e.Kind == GestureKind.LongPress))
                runsWithLongPress++;
        }

        Assert.True(runsWithLongPress > 0, "the random sequences never produced a long press");
    }

    private static void AssertBalanced(IReadOnlyList<GestureEvent> events, int run, List<string> script)
    {
        GestureKind? openKind = null;
        foreach (var e in events)
        {
            AssertFinite(e);
            if (e.Kind is Tap or DoubleTap)
            {
                Assert.True(e.Phase == Ended, $"run {run}: {e.Kind} with phase {e.Phase}");
                continue;
            }

            var context = $"run {run}: {e.Kind} {e.Phase}, open {openKind}; script {string.Join(' ', script)}";
            switch (e.Phase)
            {
                case Began:
                    Assert.True(openKind is null, context);
                    openKind = e.Kind;
                    break;
                case Changed:
                    Assert.True(openKind == e.Kind, context);
                    break;
                default:
                    Assert.True(openKind == e.Kind, context);
                    openKind = null;
                    break;
            }
        }

        Assert.True(openKind is null, $"run {run}: {openKind} left open");
    }
}
