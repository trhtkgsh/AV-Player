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
    private readonly ILocalizer _localizer;

    // True while LoadAsync copies the stored record into the properties: no property change saves then.
    private bool _loading;

    // Saves run one at a time: each one reads the stored record, changes one field and writes it back.
    private readonly SemaphoreSlim _saveGate = new(1, 1);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SpeedText))]
    private double _speed = 1.0;

    [ObservableProperty]
    private bool _isDecoderHardware;

    [ObservableProperty]
    private bool _isDecoderSoftware;

    [ObservableProperty]
    private bool _isAspectFit;

    [ObservableProperty]
    private bool _isAspectStretch;

    [ObservableProperty]
    private bool _isAspectCrop;

    [ObservableProperty]
    private bool _isAspectOriginal;

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

    public GlobalDefaultsViewModel(IGlobalDefaultsStore store, ILocalizer localizer)
    {
        _store = store;
        _localizer = localizer;
    }

    public string SpeedText => Speed.ToString("0.00", CultureInfo.InvariantCulture) + "x";

    public string AudioDelayText => FormatDelay(AudioDelayMs);

    public string SubtitleDelayText => FormatDelay(SubtitleDelayMs);

    /// <summary>Copies the stored defaults into the page. Store failures are shown in <see cref="ErrorMessage"/>.</summary>
    public async Task LoadAsync()
    {
        _loading = true;
        try
        {
            var stored = await _store.GetAsync(CancellationToken.None);
            Speed = stored.Speed;
            IsDecoderHardware = stored.Decoder == DecoderMode.Hardware;
            IsDecoderSoftware = stored.Decoder == DecoderMode.Software;
            IsAspectFit = stored.Aspect == AspectMode.Fit;
            IsAspectStretch = stored.Aspect == AspectMode.Stretch;
            IsAspectCrop = stored.Aspect == AspectMode.Crop;
            IsAspectOriginal = stored.Aspect == AspectMode.Original;
            IsRepeatNone = stored.Repeat == RepeatMode.None;
            IsRepeatFile = stored.Repeat == RepeatMode.File;
            IsRepeatFolder = stored.Repeat == RepeatMode.Folder;
            ResumeFromLastPosition = stored.ResumeFromLastPosition;
            PreferredAudioLanguage = stored.PreferredAudioLanguage ?? string.Empty;
            AudioDelayMs = stored.AudioDelayMs;
            SubtitlesEnabled = stored.SubtitlesEnabled;
            PreferredSubtitleLanguage = stored.PreferredSubtitleLanguage ?? string.Empty;
            SubtitleDelayMs = stored.SubtitleDelayMs;
            ErrorMessage = null;
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
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

    partial void OnIsAspectFitChanged(bool value)
    {
        if (_loading || !value)
        {
            return;
        }

        _ = SaveAsync(d => d with { Aspect = AspectMode.Fit });
    }

    partial void OnIsAspectStretchChanged(bool value)
    {
        if (_loading || !value)
        {
            return;
        }

        _ = SaveAsync(d => d with { Aspect = AspectMode.Stretch });
    }

    partial void OnIsAspectCropChanged(bool value)
    {
        if (_loading || !value)
        {
            return;
        }

        _ = SaveAsync(d => d with { Aspect = AspectMode.Crop });
    }

    partial void OnIsAspectOriginalChanged(bool value)
    {
        if (_loading || !value)
        {
            return;
        }

        _ = SaveAsync(d => d with { Aspect = AspectMode.Original });
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

    [RelayCommand(AllowConcurrentExecutions = true)]
    private async Task SaveSpeed()
    {
        var speed = RoundSpeed(Speed);
        Speed = speed;
        if (await SaveAsync(d => d with { Speed = (float)speed }))
        {
            // The thumb shows the value that was stored. Speed has no change-triggered save, so this does not save again.
            Speed = (float)speed;
        }
    }

    [RelayCommand(AllowConcurrentExecutions = true)]
    private Task ResetSpeed()
    {
        Speed = 1.0;
        return SaveAsync(d => d with { Speed = 1.0f });
    }

    [RelayCommand(AllowConcurrentExecutions = true)]
    private Task SaveLanguages()
    {
        var audio = NormalizeLanguage(PreferredAudioLanguage);
        var subtitle = NormalizeLanguage(PreferredSubtitleLanguage);

        // Written back before the save starts, so text typed while the save runs is not overwritten.
        PreferredAudioLanguage = audio ?? string.Empty;
        PreferredSubtitleLanguage = subtitle ?? string.Empty;
        return SaveAsync(d => d with { PreferredAudioLanguage = audio, PreferredSubtitleLanguage = subtitle });
    }

    [RelayCommand(AllowConcurrentExecutions = true)]
    private Task AudioDelayDown() => SetAudioDelayAsync(AudioDelayMs - DelayStepMs);

    [RelayCommand(AllowConcurrentExecutions = true)]
    private Task AudioDelayUp() => SetAudioDelayAsync(AudioDelayMs + DelayStepMs);

    [RelayCommand(AllowConcurrentExecutions = true)]
    private Task AudioDelayReset() => SetAudioDelayAsync(0);

    [RelayCommand(AllowConcurrentExecutions = true)]
    private Task SubtitleDelayDown() => SetSubtitleDelayAsync(SubtitleDelayMs - DelayStepMs);

    [RelayCommand(AllowConcurrentExecutions = true)]
    private Task SubtitleDelayUp() => SetSubtitleDelayAsync(SubtitleDelayMs + DelayStepMs);

    [RelayCommand(AllowConcurrentExecutions = true)]
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
                _localizer["Common.Reset"],
                _localizer["Defaults.ResetAllConfirm"],
                _localizer["Common.Reset"],
                _localizer["Common.Cancel"]);
            if (!confirmed)
            {
                return;
            }

            // Reset restores the record defaults. The two fields this page does not show keep their stored values.
            if (await SaveAsync(stored => new GlobalDefaults
            {
                BackgroundAudio = stored.BackgroundAudio,
                PictureInPictureOnLeave = stored.PictureInPictureOnLeave,
            }))
            {
                await LoadAsync();
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    private Task SetAudioDelayAsync(long value)
    {
        AudioDelayMs = ClampDelay(value);

        // Reads the live value when the save runs, so the last write stores the latest delay.
        return SaveAsync(d => d with { AudioDelayMs = AudioDelayMs });
    }

    private Task SetSubtitleDelayAsync(long value)
    {
        SubtitleDelayMs = ClampDelay(value);

        // Reads the live value when the save runs, so the last write stores the latest delay.
        return SaveAsync(d => d with { SubtitleDelayMs = SubtitleDelayMs });
    }

    // Every save starts from the stored record, so fields this page does not show are preserved.
    // A change that leaves the record equal to the stored one writes nothing. Returns false when the save failed.
    private async Task<bool> SaveAsync(Func<GlobalDefaults, GlobalDefaults> change)
    {
        await _saveGate.WaitAsync(CancellationToken.None);
        try
        {
            var current = await _store.GetAsync(CancellationToken.None);
            var updated = change(current);
            if (updated != current)
            {
                await _store.SaveAsync(updated, CancellationToken.None);
            }

            ErrorMessage = null;
            return true;
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            return false;
        }
        finally
        {
            _saveGate.Release();
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
