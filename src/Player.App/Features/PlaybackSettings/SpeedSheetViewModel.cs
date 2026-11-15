using System.Diagnostics;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Controls;
using Player.Core.Contracts;

namespace Player.App.Features.PlaybackSettings;

/// <summary>
/// State and commands of the speed sheet. Observes the engine; never owns or disposes it.
/// Writes the speed only to the current file's state, never to the global defaults.
/// </summary>
public sealed partial class SpeedSheetViewModel : ObservableObject
{
    private const double MinSpeed = 0.25;
    private const double MaxSpeed = 4.0;
    private const double Step = 0.05;

    private readonly IPlayerEngine _engine;
    private readonly IPlaybackStateRecorder _recorder;
    private bool _closing;

    [ObservableProperty] private double _sliderValue = 1.0;
    [ObservableProperty] private string _speedText = "1.00x";
    [ObservableProperty, NotifyPropertyChangedFor(nameof(HasError))] private string _errorMessage = "";

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    public SpeedSheetViewModel(IPlayerEngine engine, IPlaybackStateRecorder recorder)
    {
        _engine = engine;
        _recorder = recorder;
    }

    /// <summary>Called when the sheet appears. Shows the speed in effect.</summary>
    public void Activate()
    {
        float current = _engine.Snapshot.Speed;
        SliderValue = current;
        SpeedText = FormatSpeed(RoundSpeed(current));
    }

    /// <summary>Nothing to unsubscribe in this sheet. Kept for symmetry with the other sheets.</summary>
    public void Deactivate()
    {
    }

    // The slider moves while the user drags: only the label follows, the engine is not called yet.
    partial void OnSliderValueChanged(double value) => SpeedText = FormatSpeed(RoundSpeed(value));

    [RelayCommand]
    private Task ApplySliderAsync() => ApplyAsync(RoundSpeed(SliderValue));

    [RelayCommand]
    private Task SetPresetAsync(string text)
    {
        float speed = float.Parse(text, CultureInfo.InvariantCulture);
        SliderValue = speed;
        return ApplyAsync(RoundSpeed(speed));
    }

    /// <summary>Closes the sheet and returns to the player. Runs once while a close is in progress.</summary>
    [RelayCommand]
    private async Task CloseAsync()
    {
        if (_closing) return;
        _closing = true;

        try
        {
            if (Shell.Current is { } shell) await shell.GoToAsync("..");
        }
        catch (Exception ex)
        {
            _closing = false; // close failed: allow a later tap or Back press to try again
            Debug.WriteLine($"Close failed: {ex}");
        }
    }

    private async Task ApplyAsync(float speed)
    {
        try
        {
            await _engine.SetSpeedAsync(speed, CancellationToken.None);
            _recorder.Update(state => state with { Speed = speed });
            ErrorMessage = "";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            SliderValue = _engine.Snapshot.Speed; // the control returns to the value really in effect
        }
    }

    private static float RoundSpeed(double value)
    {
        double rounded = Math.Round(value / Step) * Step;
        return (float)Math.Clamp(rounded, MinSpeed, MaxSpeed);
    }

    private static string FormatSpeed(float value) =>
        value.ToString("0.00", CultureInfo.InvariantCulture) + "x";
}
