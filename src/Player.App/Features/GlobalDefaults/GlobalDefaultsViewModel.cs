using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Controls;
using Player.Core.Contracts;
using Player.Core.Models;

namespace Player.App.Features.DefaultsSettings;

/// <summary>
/// The playback defaults page. Every change is saved at once through <see cref="SaveAsync"/>,
/// which starts from the stored record so the fields this page does not show are kept.
/// </summary>
public sealed partial class GlobalDefaultsViewModel : ObservableObject
{
    private const double SpeedStep = 0.05;
    private const double MinSpeed = 0.25;
    private const double MaxSpeed = 4.0;
    private const long DelayStepMs = 50;
    private const long MinDelayMs = -5000;
    private const long MaxDelayMs = 5000;

    private readonly IGlobalDefaultsStore _store;

    // True while LoadAsync copies the stored record into the properties: no property change saves then.
    private bool _loading;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SpeedText))]
    private double _speed = 1.0;

    [ObservableProperty]
    private bool _isDecoderHardware;

    [ObservableProperty]
    private bool _isDecoderSoftware;

    [ObservableProperty]
    private bool _isRepeatNone;

    [ObservableProperty]
    private bool _isRepeatFile;

    [ObservableProperty]
    private bool _isRepeatFolder;

    [ObservableProperty]
    private bool _resumeFromLastPosition;

    [ObservableProperty]
    private string _preferredAudioLanguage = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AudioDelayText))]
    private long _audioDelayMs;

    [ObservableProperty]
    private bool _subtitlesEnabled;

    [ObservableProperty]
    private string _preferredSubtitleLanguage = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SubtitleDelayText))]
    private long _subtitleDelayMs;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private bool _hasError;

    public GlobalDefaultsViewModel(IGlobalDefaultsStore store)
    {
        _store = store;
    }

    public string SpeedText => Speed.ToString("0.00", CultureInfo.InvariantCulture) + "x";

    public string AudioDelayText => FormatDelay(AudioDelayMs);

    public string SubtitleDelayText => FormatDelay(SubtitleDelayMs);

    /// <summary>Copies the stored defaults into the page. Throws when the store fails.</summary>
    public async Task LoadAsync()
    {
        _loading = true;
        try
        {
            var stored = await _store.GetAsync(CancellationToken.None);
            Speed = stored.Speed;
            IsDecoderHardware = stored.Decoder == DecoderMode.Hardware;
            IsDecoderSoftware = stored.Decoder == DecoderMode.Software;
            IsRepeatNone = stored.Repeat == RepeatMode.None;
            IsRepeatFile = stored.Repeat == RepeatMode.File;
            IsRepeatFolder = stored.Repeat == RepeatMode.Folder;
            ResumeFromLastPosition = stored.ResumeFromLastPosition;
            PreferredAudioLanguage = stored.PreferredAudioLanguage ?? string.Empty;
            AudioDelayMs = stored.AudioDelayMs;
            SubtitlesEnabled = stored.SubtitlesEnabled;
            PreferredSubtitleLanguage = stored.PreferredSubtitleLanguage ?? string.Empty;
            SubtitleDelayMs = stored.SubtitleDelayMs;
        }
        finally
        {
            _loading = false;
        }
    }

    partial void OnErrorMessageChanged(string? value)
    {
        HasError = !string.IsNullOrEmpty(value);
    }

    partial void OnResumeFromLastPositionChanged(bool value)
    {
        if (_loading)
        {
            return;
        }

        _ = SaveAsync(d => d with { ResumeFromLastPosition = value });
    }

    partial void OnSubtitlesEnabledChanged(bool value)
    {
        if (_loading)
        {
            return;
        }

        _ = SaveAsync(d => d with { SubtitlesEnabled = value });
    }

    // A radio button saves only when it becomes selected; the one that is deselected is ignored.
    partial void OnIsDecoderHardwareChanged(bool value)
    {
        if (_loading || !value)
        {
            return;
        }

        _ = SaveAsync(d => d with { Decoder = DecoderMode.Hardware });
    }

    partial void OnIsDecoderSoftwareChanged(bool value)
    {
        if (_loading || !value)
        {
            return;
        }

        _ = SaveAsync(d => d with { Decoder = DecoderMode.Software });
    }

    partial void OnIsRepeatNoneChanged(bool value)
    {
        if (_loading || !value)
        {
            return;
        }

        _ = SaveAsync(d => d with { Repeat = RepeatMode.None });
    }

    partial void OnIsRepeatFileChanged(bool value)
    {
        if (_loading || !value)
        {
            return;
        }

        _ = SaveAsync(d => d with { Repeat = RepeatMode.File });
    }

    partial void OnIsRepeatFolderChanged(bool value)
    {
        if (_loading || !value)
        {
            return;
        }

        _ = SaveAsync(d => d with { Repeat = RepeatMode.Folder });
    }

    [RelayCommand]
    private Task SaveSpeed()
    {
        var speed = RoundSpeed(Speed);
        Speed = speed;
        return SaveAsync(d => d with { Speed = (float)speed });
    }

    [RelayCommand]
    private Task ResetSpeed()
    {
        Speed = 1.0;
        return SaveAsync(d => d with { Speed = 1.0f });
    }

    [RelayCommand]
    private Task SaveLanguages()
    {
        var audio = NormalizeLanguage(PreferredAudioLanguage);
        var subtitle = NormalizeLanguage(PreferredSubtitleLanguage);
        return SaveAsync(d => d with { PreferredAudioLanguage = audio, PreferredSubtitleLanguage = subtitle });
    }

    [RelayCommand]
    private Task AudioDelayDown() => SetAudioDelayAsync(AudioDelayMs - DelayStepMs);

    [RelayCommand]
    private Task AudioDelayUp() => SetAudioDelayAsync(AudioDelayMs + DelayStepMs);

    [RelayCommand]
    private Task AudioDelayReset() => SetAudioDelayAsync(0);

    [RelayCommand]
    private Task SubtitleDelayDown() => SetSubtitleDelayAsync(SubtitleDelayMs - DelayStepMs);

    [RelayCommand]
    private Task SubtitleDelayUp() => SetSubtitleDelayAsync(SubtitleDelayMs + DelayStepMs);

    [RelayCommand]
    private Task SubtitleDelayReset() => SetSubtitleDelayAsync(0);

    [RelayCommand]
    private async Task OpenSubtitleStyle()
    {
        try
        {
            if (Shell.Current is { } shell)
            {
                await shell.GoToAsync("settings-subtitle-style");
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    [RelayCommand]
    private async Task ResetAll()
    {
        try
        {
            if (Shell.Current is not { } shell)
            {
                return;
            }

            var confirmed = await shell.DisplayAlertAsync(
                "Reset", "Reset all playback defaults?", "Reset", "Cancel");
            if (!confirmed)
            {
                return;
            }

            // The three fields this page does not show keep their stored values.
            await SaveAsync(stored => new GlobalDefaults
            {
                Aspect = stored.Aspect,
                BackgroundAudio = stored.BackgroundAudio,
                PictureInPictureOnLeave = stored.PictureInPictureOnLeave,
            });
            await LoadAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    private Task SetAudioDelayAsync(long value)
    {
        var delay = ClampDelay(value);
        AudioDelayMs = delay;
        return SaveAsync(d => d with { AudioDelayMs = delay });
    }

    private Task SetSubtitleDelayAsync(long value)
    {
        var delay = ClampDelay(value);
        SubtitleDelayMs = delay;
        return SaveAsync(d => d with { SubtitleDelayMs = delay });
    }

    // Every save starts from the stored record, so fields this page does not show are preserved.
    private async Task SaveAsync(Func<GlobalDefaults, GlobalDefaults> change)
    {
        try
        {
            var current = await _store.GetAsync(CancellationToken.None);
            await _store.SaveAsync(change(current), CancellationToken.None);
            ErrorMessage = null;
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    private static double RoundSpeed(double value) =>
        Math.Clamp(Math.Round(value / SpeedStep) * SpeedStep, MinSpeed, MaxSpeed);

    private static long ClampDelay(long value) => Math.Clamp(value, MinDelayMs, MaxDelayMs);

    private static string FormatDelay(long ms) => ms switch
    {
        0 => "0 ms",
        > 0 => "+" + ms.ToString(CultureInfo.InvariantCulture) + " ms",
        _ => ms.ToString(CultureInfo.InvariantCulture) + " ms",
    };

    private static string? NormalizeLanguage(string text)
    {
        var trimmed = text.Trim();
        return trimmed.Length == 0 ? null : trimmed;
    }
}
