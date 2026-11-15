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
                Guard(nameof(VerticalDrag), () => VerticalDrag(e));
                break;
            case GestureKind.HorizontalDrag:
                Guard(nameof(HorizontalDrag), () => HorizontalDrag(e));
                break;
            default:
                // Pinch and Pan belong to the zoom feature (part 2 of the task).
                break;
        }
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
        }

        if (e.Phase is GesturePhase.Ended or GesturePhase.Cancelled)
        {
            _osd.Release();
            return;
        }

        double brightness = Math.Clamp(_brightnessStart - e.DeltaY, MinBrightness, 1.0);
        _surface.SetWindowBrightness(brightness);
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
                PreviewSeek(e);
                break;

            case GesturePhase.Changed:
                PreviewSeek(e);
                break;

            case GesturePhase.Ended:
                _osd.Release();
                if (_seeking)
                {
                    long target = _seekTarget;
                    Fire(() => _engine.SeekAsync(target, CancellationToken.None));
                }

                _seeking = false;
                break;

            case GesturePhase.Cancelled:
                _osd.Hide();
                _seeking = false;
                break;
        }
    }

    private void PreviewSeek(GestureEvent e)
    {
        if (_seekDuration <= 0) return; // nothing to seek in

        _seeking = true;
        _seekTarget = Math.Clamp(_seekStart + (long)Math.Round(e.DeltaX * SeekMsPerScreenWidth), 0, _seekDuration);
        _osd.Hold(SeekText(_seekStart, _seekTarget));
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
