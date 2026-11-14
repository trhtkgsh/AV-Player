using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Player.Core.Contracts;
using Player.Core.Models;

namespace Player.App.Features.PlayerHud;

/// <summary>
/// State and commands of the player page. Observes the engine and the coordinator; never owns or disposes them.
/// </summary>
public sealed partial class PlayerViewModel : ObservableObject
{
    private const int ClockRefreshSeconds = 15;

    private readonly IPlayerEngine _engine;
    private readonly IPlaybackCoordinator _coordinator;
    private readonly IUiSettingsStore _uiSettings;

    private UiSettings _settings = new();
    private bool _active;
    private bool _dragging;
    private bool _closing;
    private bool _errorHandled;
    private CancellationTokenSource? _hideCts;
    private CancellationTokenSource? _clockCts;

    [ObservableProperty] private string _title = "";
    [ObservableProperty] private double _positionMs;
    [ObservableProperty] private double _durationMs = 1;
    [ObservableProperty] private string _positionText = "0:00";
    [ObservableProperty] private string _durationText = "0:00";
    [ObservableProperty] private bool _isPlaying;
    [ObservableProperty] private bool _isOpening;
    [ObservableProperty] private bool _isHudVisible = true;
    [ObservableProperty] private string _decoderText = "...";
    [ObservableProperty] private bool _showClock;
    [ObservableProperty] private string _clockText = "";
    [ObservableProperty] private string _playPauseText = "Play";

    public PlayerViewModel(IPlayerEngine engine, IPlaybackCoordinator coordinator, IUiSettingsStore uiSettings)
    {
        _engine = engine;
        _coordinator = coordinator;
        _uiSettings = uiSettings;
    }

    /// <summary>Called when the page appears. Reads the settings once, then subscribes and starts the timers.</summary>
    public async Task ActivateAsync()
    {
        if (_active) return;
        _active = true;

        var settings = await _uiSettings.GetAsync(CancellationToken.None);
        if (!_active) return; // deactivated while the settings were loading

        ApplySettings(settings);
        _engine.SnapshotChanged += OnSnapshotChanged;
        _coordinator.Changed += OnCoordinatorChanged;
        _uiSettings.Changed += OnUiSettingsChanged;

        ApplySnapshot(_engine.Snapshot);
        StartClock();
        RestartHideTimer();
    }

    /// <summary>Called when the page disappears. Unsubscribes and stops both timers.</summary>
    public void Deactivate()
    {
        _active = false;
        _engine.SnapshotChanged -= OnSnapshotChanged;
        _coordinator.Changed -= OnCoordinatorChanged;
        _uiSettings.Changed -= OnUiSettingsChanged;
        CancelHideTimer();
        StopClock();
    }

    partial void OnPositionMsChanged(double value)
    {
        PositionText = TimeFormatter.Format((long)value);
    }

    partial void OnIsPlayingChanged(bool value)
    {
        PlayPauseText = value ? "Pause" : "Play";

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
    private Task ToggleDecoderAsync() => RunAsync(() => _engine.SetDecoderAsync(
        _engine.Snapshot.RequestedDecoder == DecoderMode.Hardware ? DecoderMode.Software : DecoderMode.Hardware,
        CancellationToken.None));

    [RelayCommand]
    private void ToggleHud()
    {
        IsHudVisible = !IsHudVisible;
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
        _closing = true;

        try
        {
            await _coordinator.CloseAsync(CancellationToken.None);
        }
        catch (OperationCanceledException)
        {
            // The page is leaving anyway; nothing to show.
        }
        catch (Exception ex)
        {
            await ShowAlertAsync("Error", ex.Message);
        }

        if (Shell.Current is { } shell) await shell.GoToAsync("..");
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
            await ShowAlertAsync("Error", ex.Message);
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
        if (!_dragging)
        {
            PositionMs = snapshot.PositionMs;
        }

        DecoderText = snapshot.ActualDecoder switch
        {
            DecoderMode.Hardware => "HW",
            DecoderMode.Software => "SW",
            _ => "...",
        };

        RefreshTitle();

        if (snapshot.Status == PlayerStatus.Error)
        {
            _ = HandleErrorAsync(snapshot.Error ?? "Unknown error.");
        }
    }

    private void ApplySettings(UiSettings settings)
    {
        _settings = settings;
        ShowClock = settings.ShowClock;
        UpdateClock();
    }

    private void RefreshTitle() => Title = _coordinator.Current?.DisplayName ?? "";

    /// <summary>Shows the error once, then closes the page through the same path as the Back button.</summary>
    private async Task HandleErrorAsync(string message)
    {
        if (_errorHandled) return;
        _errorHandled = true;
        await ShowAlertAsync("Cannot play", message);
        await CloseAsync();
    }

    private void RestartHideTimer()
    {
        CancelHideTimer();
        if (!_active) return;

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
            await Task.Delay(TimeSpan.FromSeconds(Math.Max(0, _settings.HudAutoHideSeconds)), ct);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        // Only while playing. A restart that arrived during the wait cancels this token, so re-check it.
        if (!ct.IsCancellationRequested && IsPlaying && !_dragging) IsHudVisible = false;
    }

    private void StartClock()
    {
        StopClock();
        UpdateClock();
        var cts = new CancellationTokenSource();
        _clockCts = cts;
        _ = RunClockAsync(cts.Token);
    }

    private void StopClock()
    {
        _clockCts?.Cancel();
        _clockCts?.Dispose();
        _clockCts = null;
    }

    private async Task RunClockAsync(CancellationToken ct)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(ClockRefreshSeconds));
        try
        {
            while (await timer.WaitForNextTickAsync(ct))
            {
                UpdateClock();
            }
        }
        catch (OperationCanceledException)
        {
            // Deactivated.
        }
    }

    private void UpdateClock()
    {
        if (ShowClock) ClockText = DateTime.Now.ToString("HH:mm", CultureInfo.InvariantCulture);
    }

    private static async Task ShowAlertAsync(string title, string message)
    {
        if (Shell.Current is { } shell)
        {
            await shell.DisplayAlertAsync(title, message, "OK");
        }
    }
}
