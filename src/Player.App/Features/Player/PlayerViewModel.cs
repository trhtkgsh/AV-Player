using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Devices;
using Player.Core.Contracts;
using Player.Core.Models;

namespace Player.App.Features.PlayerHud;

/// <summary>
/// State and commands of the player page. Observes the engine and the coordinator; never owns or disposes them.
/// </summary>
public sealed partial class PlayerViewModel : ObservableObject
{
    private const int StatusRefreshSeconds = 15;
    private const int UnlockVisibleSeconds = 3;

    private readonly IPlayerEngine _engine;
    private readonly IPlaybackCoordinator _coordinator;
    private readonly IUiSettingsStore _uiSettings;
    private readonly IPlaybackStateRecorder _recorder;
    private readonly ILocalizer _localizer;
    private readonly IVideoSurface _surface;
    private readonly IGestureSource _gestures;
    private readonly PlayerGestureHandler _gestureHandler;

    private UiSettings _settings = new();
    private bool _active;
    private bool _sheetOpening;
    private bool _menuOpen;
    private bool _returnFromSheet;
    private int _activation;
    private bool _dragging;
    private bool _closing;
    private bool _errorHandled;
    private double _enginePositionMs;
    private CancellationTokenSource? _hideCts;
    private CancellationTokenSource? _statusCts;
    private CancellationTokenSource? _unlockCts;

    [ObservableProperty] private string _title = "";
    [ObservableProperty] private double _positionMs;
    [ObservableProperty] private double _durationMs = 1;
    [ObservableProperty] private string _positionText = "0:00";
    [ObservableProperty] private string _durationText = "0:00";
    [ObservableProperty, NotifyPropertyChangedFor(nameof(ShowPlayIcon))] private bool _isPlaying;
    [ObservableProperty] private bool _isOpening;
    [ObservableProperty] private bool _isHudVisible = true;
    [ObservableProperty] private string _decoderText = "...";
    [ObservableProperty] private string _speedText = "1.0x";
    [ObservableProperty] private bool _showStatus;
    [ObservableProperty] private string _statusText = "";
    [ObservableProperty] private bool _isLocked;
    [ObservableProperty] private bool _isUnlockVisible;

    /// <summary>The on-screen indicator of gesture values.</summary>
    public OsdViewModel Osd { get; }

    /// <summary>True while the play icon is shown; the pause icon is shown while playing.</summary>
    public bool ShowPlayIcon => !IsPlaying;

    public PlayerViewModel(
        IPlayerEngine engine,
        IPlaybackCoordinator coordinator,
        IUiSettingsStore uiSettings,
        IPlaybackStateRecorder recorder,
        ILocalizer localizer,
        IGestureSource gestures,
        IVideoSurface surface)
    {
        _engine = engine;
        _coordinator = coordinator;
        _uiSettings = uiSettings;
        _recorder = recorder;
        _localizer = localizer;
        _surface = surface;
        _gestures = gestures;
        Osd = new OsdViewModel();
        _gestureHandler = new PlayerGestureHandler(
            gestures, engine, coordinator, surface, localizer, Osd, () => _settings, ToggleHud);
    }

    /// <summary>Called when the page appears. Reads the settings once, then subscribes and starts the timers.</summary>
    public async Task ActivateAsync()
    {
        if (_active) return;
        int activation = ++_activation;

        var settings = await _uiSettings.GetAsync(CancellationToken.None);
        if (activation != _activation || _active) return; // deactivated, or another activation took over

        _active = true;
        _gestures.Locked = IsLocked; // a locked page stays locked when it is activated again
        if (IsLocked) IsHudVisible = false;
        ApplySettings(settings);
        _engine.SnapshotChanged += OnSnapshotChanged;
        _coordinator.Changed += OnCoordinatorChanged;
        _uiSettings.Changed += OnUiSettingsChanged;
        _gestureHandler.Attach();
        _surface.SetImmersive(true);

        ApplySnapshot(_engine.Snapshot);
        if (_returnFromSheet)
        {
            // Back from a settings sheet: the HUD comes back, the countdown starts again below.
            _returnFromSheet = false;
            IsHudVisible = true;
        }

        RestartHideTimer();
    }

    /// <summary>Called when the page disappears. Unsubscribes and stops both timers. Safe to call more than once.</summary>
    public void Deactivate()
    {
        _activation++;
        _active = false;
        _engine.SnapshotChanged -= OnSnapshotChanged;
        _coordinator.Changed -= OnCoordinatorChanged;
        _uiSettings.Changed -= OnUiSettingsChanged;
        _gestureHandler.Detach(); // the window stays immersive: a settings sheet may open on top
        CancelUnlockTimer(); // the lock itself stays: a locked screen stays locked in the background
        IsUnlockVisible = false;
        CancelHideTimer();
        StopStatus();
    }

    partial void OnPositionMsChanged(double value)
    {
        // Only a drag shows the slider value. Otherwise show the engine position, so a clamped slider write-back is ignored.
        PositionText = TimeFormatter.Format((long)(_dragging ? value : _enginePositionMs));
    }

    partial void OnIsPlayingChanged(bool value)
    {
        // Playback started (for example after opening): the HUD must start its countdown now.
        if (value)
        {
            RestartHideTimer();
        }
    }

    [RelayCommand]
    private Task PlayPauseAsync() => RunAsync(() => _engine.Snapshot.Status == PlayerStatus.Playing
        ? _engine.PauseAsync(CancellationToken.None)
        : _engine.PlayAsync(CancellationToken.None));

    [RelayCommand]
    private Task NextAsync() => RunAsync(() => _coordinator.NextAsync(CancellationToken.None));

    [RelayCommand]
    private Task PreviousAsync() => RunAsync(() => _coordinator.PreviousAsync(CancellationToken.None));

    [RelayCommand]
    private Task ToggleDecoderAsync() => RunAsync(async () =>
    {
        var mode = _engine.Snapshot.RequestedDecoder == DecoderMode.Hardware ? DecoderMode.Software : DecoderMode.Hardware;
        await _engine.SetDecoderAsync(mode, CancellationToken.None);
        _recorder.Update(s => s with { Decoder = mode }); // remembered for this file only
    });

    [RelayCommand]
    private Task OpenSpeedAsync() => OpenSheetAsync("player-speed");

    [RelayCommand]
    private Task OpenRepeatAsync() => OpenSheetAsync("player-repeat");

    [RelayCommand]
    private Task OpenAudioAsync() => OpenSheetAsync("player-audio");

    [RelayCommand]
    private Task OpenSubtitlesAsync() => OpenSheetAsync("player-subtitles");

    /// <summary>
    /// The menu of the controls: Subtitles, Audio track, Repeat and the decoder. The controls stay visible while
    /// the sheet is open, and a second tap while it is open is ignored. Never throws.
    /// </summary>
    [RelayCommand]
    private async Task OpenMenuAsync()
    {
        if (_menuOpen) return;
        _menuOpen = true;
        CancelHideTimer();

        string subtitles = _localizer["Player.Subtitles"];
        string audio = _localizer["Player.AudioTrack"];
        string repeat = _localizer["Player.Repeat"];
        string decoder = _localizer.Format("Player.MenuDecoder", DecoderText);
        string? choice = null;
        try
        {
            if (Shell.Current is { } shell)
            {
                choice = await shell.DisplayActionSheetAsync(
                    _localizer["Player.Menu"],
                    _localizer["Common.Cancel"],
                    null,
                    subtitles,
                    audio,
                    repeat,
                    decoder);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Menu failed: {ex}");
        }
        finally
        {
            _menuOpen = false;
        }

        // A cancelled or dismissed sheet gives no matching text: the controls simply count down again.
        if (choice == subtitles) await OpenSubtitlesAsync();
        else if (choice == audio) await OpenAudioAsync();
        else if (choice == repeat) await OpenRepeatAsync();
        else if (choice == decoder) await ToggleDecoderAsync();
        else RestartHideTimer();
    }

    [RelayCommand]
    private void ToggleHud()
    {
        if (IsLocked)
        {
            ShowUnlockButton(); // while locked the HUD never shows; a tap only reveals the unlock button
            return;
        }

        IsHudVisible = !IsHudVisible;
        RestartHideTimer();
    }

    /// <summary>While locked: shows the unlock button for 3 seconds. A new call restarts the 3 seconds.</summary>
    public void ShowUnlockButton()
    {
        IsUnlockVisible = true;
        CancelUnlockTimer();
        var cts = new CancellationTokenSource();
        _unlockCts = cts;
        _ = HideUnlockAfterDelayAsync(cts.Token);
    }

    [RelayCommand]
    private void Lock()
    {
        _gestures.Locked = true;
        IsLocked = true;
        CancelHideTimer();
        IsHudVisible = false;
        Osd.Show(_localizer["Player.Locked"]);
    }

    [RelayCommand]
    private void Unlock()
    {
        ReleaseLock();
        IsHudVisible = true;
        RestartHideTimer();
    }

    /// <summary>Clears the lock in the gesture source and on the page. Safe to call when not locked.</summary>
    private void ReleaseLock()
    {
        _gestures.Locked = false;
        IsLocked = false;
        CancelUnlockTimer();
        IsUnlockVisible = false;
    }

    [RelayCommand]
    private void CycleAspect()
    {
        var next = _surface.Aspect switch
        {
            AspectMode.Fit => AspectMode.Stretch,
            AspectMode.Stretch => AspectMode.Crop,
            AspectMode.Crop => AspectMode.Original,
            _ => AspectMode.Fit,
        };
        _surface.SetAspect(next);
        _recorder.Update(s => s with { Aspect = next }); // remembered for this file only

        string key = next switch
        {
            AspectMode.Stretch => "Player.Aspect.Stretch",
            AspectMode.Crop => "Player.Aspect.Crop",
            AspectMode.Original => "Player.Aspect.Original",
            _ => "Player.Aspect.Fit",
        };
        Osd.Show(_localizer[key]);
        RestartHideTimer();
    }

    [RelayCommand]
    private void SeekStarted()
    {
        _dragging = true;
        RestartHideTimer();
    }

    [RelayCommand]
    private Task SeekCompletedAsync() => RunAsync(async () =>
    {
        try
        {
            await _engine.SeekAsync((long)PositionMs, CancellationToken.None);
        }
        finally
        {
            _dragging = false;
        }
    });

    /// <summary>Closes the queue and leaves the page. Runs once, however many times it is requested.</summary>
    [RelayCommand]
    private async Task CloseAsync()
    {
        if (_closing) return;
        if (IsLocked)
        {
            ShowUnlockButton(); // Back while locked: do not close, show the unlock button like a tap
            return;
        }
        _closing = true;

        try
        {
            await _coordinator.CloseAsync(CancellationToken.None);
            ReleaseLock();
            _surface.SetImmersive(false);
            _surface.SetWindowBrightness(null);
            if (Shell.Current is { } shell) await shell.GoToAsync("..");
        }
        catch (OperationCanceledException)
        {
            _closing = false; // the page is leaving anyway; allow a later Back press to try again
        }
        catch (Exception ex)
        {
            _closing = false; // close failed: allow a later Back press to try again
            System.Diagnostics.Debug.WriteLine($"Close failed: {ex}");
        }
    }

    private async Task RunAsync(Func<Task> action)
    {
        RestartHideTimer();
        try
        {
            await action();
        }
        catch (OperationCanceledException)
        {
            // Cancelled by the page going away; nothing to show.
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Command failed: {ex}");
            await ShowAlertAsync(_localizer["Common.Error"], _localizer["Player.ActionFailed"]);
        }
    }

    /// <summary>Opens a settings sheet route. A second tap while the navigation runs is ignored. Never throws.</summary>
    private async Task OpenSheetAsync(string route)
    {
        if (_sheetOpening) return;
        _sheetOpening = true;
        try
        {
            // The sheet covers the HUD: stop the countdown and hide it. ActivateAsync shows it again.
            CancelHideTimer();
            IsHudVisible = false;
            _returnFromSheet = true;
            if (Shell.Current is { } shell) await shell.GoToAsync(route);
        }
        catch (Exception ex)
        {
            // The route may not exist yet; the sheet is then simply not opened. Give the user the controls back.
            _returnFromSheet = false;
            IsHudVisible = true;
            RestartHideTimer();
            System.Diagnostics.Debug.WriteLine($"Open {route} failed: {ex}");
        }
        finally
        {
            _sheetOpening = false;
        }
    }

    // Engine and store events arrive on background threads: marshal before touching properties.
    private void OnSnapshotChanged(object? sender, PlayerSnapshot snapshot) =>
        MainThread.BeginInvokeOnMainThread(() => { if (_active) ApplySnapshot(snapshot); });

    private void OnCoordinatorChanged(object? sender, EventArgs e) =>
        MainThread.BeginInvokeOnMainThread(() => { if (_active) RefreshTitle(); });

    private void OnUiSettingsChanged(object? sender, UiSettings settings) =>
        MainThread.BeginInvokeOnMainThread(() => { if (_active) ApplySettings(settings); });

    private void ApplySnapshot(PlayerSnapshot snapshot)
    {
        IsPlaying = snapshot.Status == PlayerStatus.Playing;
        IsOpening = snapshot.Status == PlayerStatus.Opening;
        DurationMs = Math.Max(1d, snapshot.DurationMs);
        DurationText = TimeFormatter.Format(snapshot.DurationMs);
        _enginePositionMs = snapshot.PositionMs;
        if (!_dragging)
        {
            PositionMs = _enginePositionMs;
            PositionText = TimeFormatter.Format((long)_enginePositionMs);
        }

        DecoderText = snapshot.ActualDecoder switch
        {
            DecoderMode.Hardware => "HW",
            DecoderMode.Software => "SW",
            _ => "...",
        };

        SpeedText = snapshot.Speed.ToString("0.0#", CultureInfo.InvariantCulture) + "x";

        RefreshTitle();

        if (snapshot.Status == PlayerStatus.Error)
        {
            _ = HandleErrorAsync(snapshot.Error ?? "no message");
        }
    }

    private void ApplySettings(UiSettings settings)
    {
        _settings = settings;
        ShowStatus = settings.ShowClock || settings.ShowBattery;
        UpdateStatusState();
    }

    private void RefreshTitle() => Title = _coordinator.Current?.DisplayName ?? "";

    /// <summary>Shows the error once, then closes the page through the same path as the Back button. Never throws.</summary>
    private async Task HandleErrorAsync(string message)
    {
        if (_errorHandled) return;
        _errorHandled = true;
        try
        {
            System.Diagnostics.Debug.WriteLine($"Playback error: {message}");
            await ShowAlertAsync(_localizer["Player.CannotPlay"], _localizer["Player.CannotPlayMessage"]);
            ReleaseLock(); // an error close must not be blocked by the lock
            await CloseAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error handling failed: {ex}");
        }
    }

    private void RestartHideTimer()
    {
        CancelHideTimer();
        if (!_active || IsLocked) return;

        var cts = new CancellationTokenSource();
        _hideCts = cts;
        _ = HideAfterDelayAsync(cts.Token);
    }

    private void CancelHideTimer()
    {
        _hideCts?.Cancel();
        _hideCts?.Dispose();
        _hideCts = null;
    }

    private async Task HideAfterDelayAsync(CancellationToken ct)
    {
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(Math.Max(1, _settings.HudAutoHideSeconds)), ct);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        // Only while playing. A restart that arrived during the wait cancels this token, so re-check it.
        if (!ct.IsCancellationRequested && IsPlaying && !_dragging) IsHudVisible = false;
    }

    private void CancelUnlockTimer()
    {
        _unlockCts?.Cancel();
        _unlockCts?.Dispose();
        _unlockCts = null;
    }

    private async Task HideUnlockAfterDelayAsync(CancellationToken ct)
    {
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(UnlockVisibleSeconds), ct);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        if (!ct.IsCancellationRequested) IsUnlockVisible = false;
    }

    /// <summary>The status line runs only while active and ShowStatus is on.</summary>
    private void UpdateStatusState()
    {
        if (!_active || !ShowStatus)
        {
            StopStatus();
            return;
        }

        if (_statusCts is not null)
        {
            UpdateStatus(); // already running: show the new settings at once
            return;
        }

        UpdateStatus();
        var cts = new CancellationTokenSource();
        _statusCts = cts;
        _ = RunStatusAsync(cts.Token);
    }

    private void StopStatus()
    {
        _statusCts?.Cancel();
        _statusCts?.Dispose();
        _statusCts = null;
    }

    private async Task RunStatusAsync(CancellationToken ct)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(StatusRefreshSeconds));
        try
        {
            while (await timer.WaitForNextTickAsync(ct))
            {
                UpdateStatus();
            }
        }
        catch (OperationCanceledException)
        {
            // Deactivated.
        }
    }

    /// <summary>Builds the status line: the clock and the battery percentage, joined by two spaces.</summary>
    private void UpdateStatus()
    {
        var parts = new List<string>(2);
        if (_settings.ShowClock) parts.Add(DateTime.Now.ToString("HH:mm", CultureInfo.InvariantCulture));
        if (_settings.ShowBattery && ReadBatteryPercent() is { } percent)
        {
            parts.Add(percent.ToString(CultureInfo.InvariantCulture) + "%");
        }

        StatusText = "\u200E" + string.Join("  ", parts); // LTR mark: the clock and the percentage keep their order in RTL
    }

    /// <summary>The battery level in percent, or null when it is unknown. Never throws.</summary>
    private static int? ReadBatteryPercent()
    {
        try
        {
            double level = Battery.Default.ChargeLevel;
            if (double.IsNaN(level) || level < 0) return null;
            return (int)Math.Round(Math.Clamp(level, 0d, 1d) * 100d);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Battery level unavailable: {ex}");
            return null;
        }
    }

    /// <summary>Shows an alert with the localized OK button. Never throws, so it is safe to await inside a catch block.</summary>
    private async Task ShowAlertAsync(string title, string message)
    {
        try
        {
            if (Shell.Current is { } shell)
            {
                await shell.DisplayAlertAsync(title, message, _localizer["Common.Ok"]);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Alert failed: {ex}");
        }
    }
}
