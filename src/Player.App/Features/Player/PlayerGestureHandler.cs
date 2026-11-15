using System.Diagnostics;
using System.Globalization;
using Player.Core.Contracts;
using Player.Core.Models;

namespace Player.App.Features.PlayerHud;

/// <summary>
/// Turns the gestures of the player page into engine commands and on-screen indicators.
/// Created by <see cref="PlayerViewModel"/>. The gesture event arrives on the UI thread.
/// No exception escapes a gesture; engine and coordinator calls are fire-and-forget.
/// </summary>
public sealed class PlayerGestureHandler
{
    private const long SeekMsPerScreenWidth = 90000;
    private const int VolumePercentPerScreenHeight = 200;
    private const int MaxVolumePercent = 200;
    private const double DefaultBrightness = 0.5;
    private const double MinBrightness = 0.01;
    private const long ScrubStepMs = 300;

    private readonly IGestureSource _gestures;
    private readonly IPlayerEngine _engine;
    private readonly IPlaybackCoordinator _coordinator;
    private readonly IVideoSurface _surface;
    private readonly ILocalizer _localizer;
    private readonly OsdViewModel _osd;
    private readonly Func<UiSettings> _settings;
    private readonly Action _toggleHud;

    private int _volumeStart;
    private int _lastVolumeSent;
    private double _brightnessStart;
    private long _seekStart;
    private long _seekDuration;
    private long _seekTarget;
    private bool _seeking;
    private bool _seekWasPlaying; // the drag paused playback at Began; it resumes at the end
    private int _dragId; // a horizontal drag's Began increments it; a finish for an older drag does not resume
    private bool _resumePending; // a drag ended and its resume is still owed; the next drag takes it over
    private long _scrubTarget; // the newest seek target; the running loop sends it
    private Task _scrubTask = Task.CompletedTask;
    private CancellationTokenSource _scrubCts = new(); // ends the wait for a picture; replaced at each drag's Began, never disposed
    private bool _verticalActive; // set on Began; cleared on Ended, Cancelled and Detach
    private bool _horizontalActive;
    private bool _pinchActive;
    private bool _panActive;
    private bool _longPressActive;
    private float? _longPressRestoreSpeed; // set while a long press plays faster; the speed to return to
    private MediaItem? _longPressItem; // the file the long press started on; its speed is restored only while it is open
    private int _lastBrightnessPercent = -1;
    private ViewTransform _zoomStart = new(1, 0, 0);
    private ViewTransform _panStart = new(1, 0, 0);

    public PlayerGestureHandler(
        IGestureSource gestures,
        IPlayerEngine engine,
        IPlaybackCoordinator coordinator,
        IVideoSurface surface,
        ILocalizer localizer,
        OsdViewModel osd,
        Func<UiSettings> settings,
        Action toggleHud)
    {
        _gestures = gestures;
        _engine = engine;
        _coordinator = coordinator;
        _surface = surface;
        _localizer = localizer;
        _osd = osd;
        _settings = settings;
        _toggleHud = toggleHud;
    }

    public void Attach() => _gestures.Gesture += OnGesture;

    public void Detach()
    {
        _gestures.Gesture -= OnGesture;
        if (_horizontalActive) EndSeek(_seeking ? _seekStart : null); // a drag cut off by Detach counts as cancelled
        RestoreSpeed();
        _verticalActive = _horizontalActive = _pinchActive = _panActive = _longPressActive = false; // a gesture cut off by Detach is over
        _osd.Hide();
    }

    private void OnGesture(object? sender, GestureEvent e)
    {
        switch (e.Kind)
        {
            case GestureKind.Tap:
                Guard(nameof(Tap), () => Tap(e));
                break;
            case GestureKind.DoubleTap:
                Guard(nameof(DoubleTap), () => DoubleTap(e));
                break;
            case GestureKind.VerticalDrag:
                if (Admit(ref _verticalActive, e.Phase)) Guard(nameof(VerticalDrag), () => VerticalDrag(e));
                break;
            case GestureKind.HorizontalDrag:
                if (Admit(ref _horizontalActive, e.Phase)) Guard(nameof(HorizontalDrag), () => HorizontalDrag(e));
                break;
            case GestureKind.Pinch:
                if (Admit(ref _pinchActive, e.Phase)) Guard(nameof(Pinch), () => Pinch(e));
                break;
            case GestureKind.Pan:
                if (Admit(ref _panActive, e.Phase)) Guard(nameof(Pan), () => Pan(e));
                break;
            case GestureKind.LongPress:
                if (Admit(ref _longPressActive, e.Phase)) Guard(nameof(LongPress), () => LongPress(e));
                break;
            default:
                break;
        }
    }

    /// <summary>
    /// Began opens a gesture of one kind. Changed, Ended and Cancelled are admitted only while it is open,
    /// so a touch that started before Attach does nothing. Ended and Cancelled close it.
    /// </summary>
    private static bool Admit(ref bool active, GesturePhase phase)
    {
        if (phase == GesturePhase.Began) active = true;
        else if (!active) return false;

        if (phase is GesturePhase.Ended or GesturePhase.Cancelled) active = false;
        return true;
    }

    private void Tap(GestureEvent e)
    {
        if (e.Phase == GesturePhase.Ended) _toggleHud();
    }

    private void DoubleTap(GestureEvent e)
    {
        if (e.Phase != GesturePhase.Ended) return;

        int seconds = _settings().SkipSeconds;
        int signed = e.Side == ScreenSide.Left ? -seconds : seconds;
        _osd.Show(_localizer.Format("Player.Osd.Seconds", signed.ToString("+0;-0;0", CultureInfo.InvariantCulture)));
        Fire(() => _coordinator.SkipAsync(signed * 1000L, CancellationToken.None));
    }

    private void VerticalDrag(GestureEvent e)
    {
        if (e.Side == ScreenSide.Right) Volume(e);
        else Brightness(e);
    }

    private void Volume(GestureEvent e)
    {
        if (e.Phase == GesturePhase.Began)
        {
            _volumeStart = _engine.Snapshot.VolumePercent;
            _lastVolumeSent = _volumeStart;
        }

        if (e.Phase is GesturePhase.Ended or GesturePhase.Cancelled)
        {
            _osd.Release();
            return;
        }

        int volume = (int)Math.Clamp(Math.Round(_volumeStart - e.DeltaY * VolumePercentPerScreenHeight), 0, MaxVolumePercent);
        _osd.Hold(_localizer.Format("Player.Osd.Volume", volume));
        if (volume != _lastVolumeSent)
        {
            _lastVolumeSent = volume;
            Fire(() => _engine.SetVolumeAsync(volume, CancellationToken.None));
        }
    }

    private void Brightness(GestureEvent e)
    {
        if (e.Phase == GesturePhase.Began)
        {
            double current = _surface.WindowBrightness;
            _brightnessStart = current < 0 ? DefaultBrightness : current; // negative: the system default is in effect
            _lastBrightnessPercent = -1; // the first Changed always sends
        }

        if (e.Phase is GesturePhase.Ended or GesturePhase.Cancelled)
        {
            _osd.Release();
            return;
        }

        double brightness = Math.Clamp(_brightnessStart - e.DeltaY, MinBrightness, 1.0);
        int percent = (int)Math.Round(brightness * 100);
        if (percent != _lastBrightnessPercent)
        {
            _lastBrightnessPercent = percent;
            _surface.SetWindowBrightness(brightness);
        }

        _osd.Hold(_localizer.Format("Player.Osd.Brightness", (int)Math.Round(brightness * 100)));
    }

    private void HorizontalDrag(GestureEvent e)
    {
        switch (e.Phase)
        {
            case GesturePhase.Began:
                var snapshot = _engine.Snapshot;
                _seekStart = snapshot.PositionMs;
                _seekDuration = snapshot.DurationMs;
                _seekTarget = _seekStart;
                _seeking = false;
                _dragId++;
                _seekWasPlaying = snapshot.Status == PlayerStatus.Playing || _resumePending; // an earlier drag's resume is still owed
                _resumePending = false;
                if (snapshot.Status == PlayerStatus.Playing) Fire(() => _engine.PauseAsync(CancellationToken.None)); // playback does not run during the drag
                _scrubCts.Cancel();
                _scrubCts = new CancellationTokenSource(); // the picture of an older drag's target is no longer waited for
                PreviewSeek(e);
                break;

            case GesturePhase.Changed:
                PreviewSeek(e);
                break;

            case GesturePhase.Ended:
                _osd.Release();
                EndSeek(_seeking ? _seekTarget : null);
                break;

            case GesturePhase.Cancelled:
                _osd.Hide();
                EndSeek(_seeking ? _seekStart : null); // the picture returns to the start position
                break;
        }
    }

    /// <summary>
    /// Holding still plays at <see cref="UiSettings.LongPressSpeed"/> until the finger lifts. Only while playing.
    /// The file's saved speed is not touched: nothing is recorded, and the speed from before comes back.
    /// </summary>
    private void LongPress(GestureEvent e)
    {
        switch (e.Phase)
        {
            case GesturePhase.Began:
                if (_longPressRestoreSpeed is not null) return; // a repeated Began must not remember the fast speed

                var snapshot = _engine.Snapshot;
                if (snapshot.Status != PlayerStatus.Playing) return;

                float speed = _settings().LongPressSpeed;
                _longPressRestoreSpeed = snapshot.Speed;
                _longPressItem = _coordinator.Current;
                Fire(() => _engine.SetSpeedAsync(speed, CancellationToken.None));
                _osd.Hold(_localizer.Format("Player.Osd.Speed", speed.ToString("0.0#", CultureInfo.InvariantCulture)));
                break;

            case GesturePhase.Ended:
            case GesturePhase.Cancelled:
                RestoreSpeed();
                break;
        }
    }

    /// <summary>
    /// Returns to the speed from before a long press. Does nothing when no long press changed it. When the file
    /// has changed since the press (it ended during the hold), the new file keeps its own speed. Always clears
    /// the remembered state and releases the indicator.
    /// </summary>
    private void RestoreSpeed()
    {
        if (_longPressRestoreSpeed is not float restore) return;

        MediaItem? item = _longPressItem;
        _longPressRestoreSpeed = null;
        _longPressItem = null;
        if (ReferenceEquals(_coordinator.Current, item)) Fire(() => _engine.SetSpeedAsync(restore, CancellationToken.None));
        _osd.Release();
    }

    private void Pinch(GestureEvent e)
    {
        switch (e.Phase)
        {
            case GesturePhase.Began:
                _zoomStart = _surface.Transform;
                Zoom(e);
                break;

            case GesturePhase.Changed:
                Zoom(e);
                break;

            case GesturePhase.Ended:
            case GesturePhase.Cancelled:
                _osd.Release();
                break;
        }
    }

    private void Zoom(GestureEvent e)
    {
        ViewTransform start = _zoomStart;
        _surface.SetTransform(new ViewTransform(start.Scale * e.Scale, start.PanX, start.PanY)); // the surface clamps to 100..400%
        int percent = (int)Math.Round(_surface.Transform.Scale * 100);
        _osd.Hold(_localizer.Format("Player.Osd.Zoom", percent));
    }

    private void Pan(GestureEvent e)
    {
        switch (e.Phase)
        {
            case GesturePhase.Began:
                _panStart = _surface.Transform;
                break;

            case GesturePhase.Changed:
                ViewTransform start = _panStart;
                _surface.SetTransform(new ViewTransform(start.Scale, start.PanX + e.DeltaX, start.PanY + e.DeltaY));
                break;
        }
    }

    private void PreviewSeek(GestureEvent e)
    {
        if (_seekDuration <= 0) return; // nothing to seek in

        bool first = !_seeking; // the first target of this drag is always sent
        _seeking = true;
        _seekTarget = Math.Clamp(_seekStart + (long)Math.Round(e.DeltaX * SeekMsPerScreenWidth), 0, _seekDuration);
        _osd.Hold(SeekText(_seekStart, _seekTarget));

        // _scrubTarget is the last target sent. A finger that rests sends nothing: the same target is not sent twice.
        bool edge = (_seekTarget == 0 || _seekTarget == _seekDuration) && _seekTarget != _scrubTarget;
        if (first || edge || Math.Abs(_seekTarget - _scrubTarget) >= ScrubStepMs) Scrub(_seekTarget);
    }

    /// <summary>
    /// Ends a seek drag: the picture goes to <paramref name="target"/> (the start for a cancelled drag), then
    /// playback resumes if it was playing before the drag. Nothing runs when there is neither.
    /// </summary>
    private void EndSeek(long? target)
    {
        bool play = _seekWasPlaying;
        _seeking = false;
        _seekWasPlaying = false;
        _resumePending = play;
        if (target is null && !play) return;

        int id = _dragId;
        CancellationTokenSource scrubCts = _scrubCts; // the drag's own source: a newer drag may already have replaced the field
        Fire(() => FinishSeekAsync(target, play, id, scrubCts));
    }

    /// <summary>
    /// Sends the final target, then resumes only if no newer drag has begun since (<paramref name="id"/> is still
    /// the current drag). A newer drag takes over the owed resume and resumes at its own end. The final seek is
    /// sent before the resume, and the resume ends its wait for the picture.
    /// </summary>
    private async Task FinishSeekAsync(long? target, bool play, int id, CancellationTokenSource scrubCts)
    {
        scrubCts.Cancel(); // the picture of an older target is no longer waited for
        await _scrubTask; // never throws

        Task seek = Task.CompletedTask;
        if (target is long final)
        {
            _scrubTarget = final;
            seek = _engine.SeekAsync(final, CancellationToken.None);
        }

        if (play && id == _dragId)
        {
            _resumePending = false;
            await _engine.PlayAsync(CancellationToken.None);
        }

        await seek;
    }

    /// <summary>
    /// Latest wins: the newest target is the one sent, and no seeks queue up. A target that arrives while a
    /// loop runs only replaces <see cref="_scrubTarget"/>. Returns the running loop. Never throws.
    /// </summary>
    private Task Scrub(long target)
    {
        _scrubTarget = target;
        if (_scrubTask is { IsCompleted: false }) return _scrubTask;

        _scrubTask = SendScrubTargetsAsync();
        return _scrubTask;
    }

    private async Task SendScrubTargetsAsync()
    {
        CancellationToken token = _scrubCts.Token;
        long sent = -1; // targets are never negative, so the first pass always sends
        try
        {
            while (sent != _scrubTarget)
            {
                sent = _scrubTarget;
                await _engine.SeekAsync(sent, token);
            }
        }
        catch (OperationCanceledException)
        {
            // The wait for the picture was ended by a new drag or by the finish. The seek itself stays sent.
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Seek preview failed: {ex}");
        }
    }

    /// <summary>"[+0:14]  12:30": the signed offset from the start, then the target time.</summary>
    private static string SeekText(long start, long target)
    {
        string sign = target >= start ? "+" : "-";
        return "[" + sign + TimeFormatter.Format(Math.Abs(target - start)) + "]  " + TimeFormatter.Format(target);
    }

    /// <summary>Runs a gesture body; an exception is logged and never escapes.</summary>
    private static void Guard(string gesture, Action body)
    {
        try
        {
            body();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Gesture {gesture} failed: {ex}");
        }
    }

    /// <summary>Starts an engine or coordinator call without waiting for it. Its exception is logged.</summary>
    private static void Fire(Func<Task> call) => _ = RunQuietlyAsync(call);

    private static async Task RunQuietlyAsync(Func<Task> call)
    {
        try
        {
            await call();
        }
        catch (OperationCanceledException)
        {
            // The page is leaving; nothing to report.
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Gesture command failed: {ex}");
        }
    }
}
