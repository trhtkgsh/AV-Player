using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Player.Core.Contracts;
using Player.Core.Models;

namespace Player.App.Features.InterfaceSettings;

/// <summary>
/// The interface settings page. Every save starts from the stored record, so the fields this page
/// does not show (view mode, sort, layout) keep their stored values.
/// </summary>
public sealed partial class UiSettingsViewModel : ObservableObject
{
    private const int HudMinSeconds = 1;
    private const int HudMaxSeconds = 10;
    private const int NewTagMinDays = 1;
    private const int NewTagMaxDays = 30;
    private const double MinLongPressSpeed = 0.25;
    private const double MaxLongPressSpeed = 4.0;
    private const double MinNameSizePercent = 80;
    private const double MaxNameSizePercent = 160;

    // The values of the skip length slider, in seconds, ascending.
    private static readonly int[] SkipOptions = [5, 10, 15, 30, 60];

    private readonly IUiSettingsStore _store;
    private readonly ILocalizer _localizer;

    // Saves are serialized: each one reads the stored record, so two overlapping saves would drop a change.
    private readonly SemaphoreSlim _saveGate = new(1, 1);

    private bool _loading;

    // Saves that have started and not finished yet (see SaveAsync). Changed only through Interlocked.
    private int _pendingSaves;

    // Set by a failed save; the page reloads the stored values once the last queued save has finished.
    private bool _reloadNeeded;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HudAutoHideText))]
    private double _hudAutoHideSeconds = 3;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(NewTagDaysText))]
    private double _newTagDays = 7;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SkipLengthText))]
    private double _skipIndex;

    [ObservableProperty]
    private bool _showClock;

    [ObservableProperty]
    private bool _showBattery;

    [ObservableProperty]
    private bool _isLanguageSystem;

    [ObservableProperty]
    private bool _isLanguageEnglish;

    [ObservableProperty]
    private bool _isLanguageHebrew;

    [ObservableProperty]
    private bool _showDuration;

    [ObservableProperty]
    private bool _showResolution;

    [ObservableProperty]
    private bool _showSize;

    [ObservableProperty]
    private bool _showDate;

    [ObservableProperty]
    private bool _showPath;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LongPressSpeedText))]
    private double _longPressSpeed = 2.0;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(NameSizeText))]
    private double _nameSizePercent = 100;

    [ObservableProperty]
    private bool _includeAudioOnlyFolders = true;

    [ObservableProperty]
    private bool _overlayProgressLine = true;

    [ObservableProperty]
    private bool _overlayClock = true;

    [ObservableProperty]
    private bool _overlayBattery = true;

    // The choices of the quiet display. Each radio button shows one of these through its bool property.
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEdgeBottom), nameof(IsEdgeTop))]
    private OverlayEdge _edgeChoice = OverlayEdge.Bottom;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsTimeOff), nameof(IsTimeElapsed), nameof(IsTimeElapsedTotal),
        nameof(IsTimeRemaining), nameof(IsTimeElapsedRemaining), nameof(IsTimeElapsedTotalRemaining))]
    private OverlayTimeMode _timeChoice = OverlayTimeMode.ElapsedTotal;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsCornerTopLeft), nameof(IsCornerTopRight),
        nameof(IsCornerBottomLeft), nameof(IsCornerBottomRight))]
    private OverlayCorner _cornerChoice = OverlayCorner.TopRight;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsColorWhite), nameof(IsColorLightGray), nameof(IsColorYellow),
        nameof(IsColorGreen), nameof(IsColorCyan), nameof(IsColorOrange), nameof(IsColorRed))]
    private uint _colorChoice = 0xFFFFFFFF;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(OverlayMarginText))]
    private double _overlayMargin;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(OverlaySizeText))]
    private double _overlaySize = 12;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    private string? _errorMessage;

    public UiSettingsViewModel(IUiSettingsStore store, ILocalizer localizer)
    {
        _store = store;
        _localizer = localizer;
    }

    public string HudAutoHideText =>
        _localizer.Format("Interface.SecondsShort", ClampRound(HudAutoHideSeconds, HudMinSeconds, HudMaxSeconds));

    public string SkipLengthText =>
        _localizer.Format("Interface.Seconds", SkipOptions[ClampRound(SkipIndex, 0, SkipOptions.Length - 1)]);

    public string NewTagDaysText =>
        _localizer.Plural("Interface.Days", ClampRound(NewTagDays, NewTagMinDays, NewTagMaxDays));

    // Technical values, not localized: the invariant culture.
    public string LongPressSpeedText =>
        SnapSpeed(LongPressSpeed).ToString("0.0#", CultureInfo.InvariantCulture) + "x";

    public string NameSizeText =>
        SnapPercent(NameSizePercent).ToString("0", CultureInfo.InvariantCulture) + "%";

    public string OverlayMarginText =>
        ClampRound(OverlayMargin, PlaybackOverlaySettings.MinMarginDp, PlaybackOverlaySettings.MaxMarginDp)
            .ToString(CultureInfo.InvariantCulture);

    public string OverlaySizeText =>
        ClampRound(OverlaySize, PlaybackOverlaySettings.MinFontSize, PlaybackOverlaySettings.MaxFontSize)
            .ToString(CultureInfo.InvariantCulture);

    public bool IsEdgeBottom
    {
        get => EdgeChoice == OverlayEdge.Bottom;
        set => ChooseEdge(OverlayEdge.Bottom, value);
    }

    public bool IsEdgeTop
    {
        get => EdgeChoice == OverlayEdge.Top;
        set => ChooseEdge(OverlayEdge.Top, value);
    }

    public bool IsTimeOff
    {
        get => TimeChoice == OverlayTimeMode.None;
        set => ChooseTime(OverlayTimeMode.None, value);
    }

    public bool IsTimeElapsed
    {
        get => TimeChoice == OverlayTimeMode.Elapsed;
        set => ChooseTime(OverlayTimeMode.Elapsed, value);
    }

    public bool IsTimeElapsedTotal
    {
        get => TimeChoice == OverlayTimeMode.ElapsedTotal;
        set => ChooseTime(OverlayTimeMode.ElapsedTotal, value);
    }

    public bool IsTimeRemaining
    {
        get => TimeChoice == OverlayTimeMode.Remaining;
        set => ChooseTime(OverlayTimeMode.Remaining, value);
    }

    public bool IsTimeElapsedRemaining
    {
        get => TimeChoice == OverlayTimeMode.ElapsedRemaining;
        set => ChooseTime(OverlayTimeMode.ElapsedRemaining, value);
    }

    public bool IsTimeElapsedTotalRemaining
    {
        get => TimeChoice == OverlayTimeMode.ElapsedTotalRemaining;
        set => ChooseTime(OverlayTimeMode.ElapsedTotalRemaining, value);
    }

    public bool IsCornerTopLeft
    {
        get => CornerChoice == OverlayCorner.TopLeft;
        set => ChooseCorner(OverlayCorner.TopLeft, value);
    }

    public bool IsCornerTopRight
    {
        get => CornerChoice == OverlayCorner.TopRight;
        set => ChooseCorner(OverlayCorner.TopRight, value);
    }

    public bool IsCornerBottomLeft
    {
        get => CornerChoice == OverlayCorner.BottomLeft;
        set => ChooseCorner(OverlayCorner.BottomLeft, value);
    }

    public bool IsCornerBottomRight
    {
        get => CornerChoice == OverlayCorner.BottomRight;
        set => ChooseCorner(OverlayCorner.BottomRight, value);
    }

    public bool IsColorWhite
    {
        get => ColorChoice == 0xFFFFFFFF;
        set => ChooseColor(0xFFFFFFFF, value);
    }

    public bool IsColorLightGray
    {
        get => ColorChoice == 0xFFCCCCCC;
        set => ChooseColor(0xFFCCCCCC, value);
    }

    public bool IsColorYellow
    {
        get => ColorChoice == 0xFFFFEB3B;
        set => ChooseColor(0xFFFFEB3B, value);
    }

    public bool IsColorGreen
    {
        get => ColorChoice == 0xFF4CAF50;
        set => ChooseColor(0xFF4CAF50, value);
    }

    public bool IsColorCyan
    {
        get => ColorChoice == 0xFF4DD0E1;
        set => ChooseColor(0xFF4DD0E1, value);
    }

    public bool IsColorOrange
    {
        get => ColorChoice == 0xFFFF9800;
        set => ChooseColor(0xFFFF9800, value);
    }

    public bool IsColorRed
    {
        get => ColorChoice == 0xFFF44336;
        set => ChooseColor(0xFFF44336, value);
    }

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    /// <summary>Called when the page appears: follows language changes and refreshes the texts built in code.</summary>
    public void Attach()
    {
        _localizer.Changed -= OnLocalizerChanged;
        _localizer.Changed += OnLocalizerChanged;
        RaiseBuiltTexts();
    }

    /// <summary>Called when the page disappears: stops following language changes.</summary>
    public void Detach() => _localizer.Changed -= OnLocalizerChanged;

    private void OnLocalizerChanged(object? sender, EventArgs e) => RaiseBuiltTexts();

    private void RaiseBuiltTexts()
    {
        OnPropertyChanged(nameof(HudAutoHideText));
        OnPropertyChanged(nameof(SkipLengthText));
        OnPropertyChanged(nameof(NewTagDaysText));
    }

    public async Task LoadAsync()
    {
        await _saveGate.WaitAsync();
        try
        {
            await PopulateAsync();
            ErrorMessage = null;
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            _saveGate.Release();
        }
    }

    // Reads the stored record and shows it. Callers hold the save gate.
    private async Task PopulateAsync()
    {
        _loading = true;
        try
        {
            var settings = await _store.GetAsync(CancellationToken.None);
            HudAutoHideSeconds = settings.HudAutoHideSeconds;
            ShowClock = settings.ShowClock;
            ShowDuration = settings.ExplorerFields.HasFlag(ExplorerInfoFields.Duration);
            ShowResolution = settings.ExplorerFields.HasFlag(ExplorerInfoFields.Resolution);
            ShowSize = settings.ExplorerFields.HasFlag(ExplorerInfoFields.Size);
            ShowDate = settings.ExplorerFields.HasFlag(ExplorerInfoFields.Date);
            ShowPath = settings.ExplorerFields.HasFlag(ExplorerInfoFields.Path);
            NewTagDays = settings.NewTagDays;
            SkipIndex = NearestSkipIndex(settings.SkipSeconds);
            ShowBattery = settings.ShowBattery;
            LongPressSpeed = settings.LongPressSpeed;
            NameSizePercent = Math.Round(settings.ExplorerFontScale * 100, MidpointRounding.AwayFromZero);
            IncludeAudioOnlyFolders = settings.IncludeAudioOnlyFolders;
            IsLanguageSystem = settings.Language == AppLanguage.System;
            IsLanguageEnglish = settings.Language == AppLanguage.English;
            IsLanguageHebrew = settings.Language == AppLanguage.Hebrew;
            OverlayProgressLine = settings.Overlay.ProgressLine;
            EdgeChoice = settings.Overlay.ProgressLineEdge;
            OverlayClock = settings.Overlay.Clock;
            OverlayBattery = settings.Overlay.Battery;
            TimeChoice = settings.Overlay.Time;
            CornerChoice = settings.Overlay.Corner;
            OverlayMargin = settings.Overlay.MarginDp;
            OverlaySize = settings.Overlay.FontSize;
            ColorChoice = settings.Overlay.ColorArgb;
        }
        finally
        {
            _loading = false;
        }
    }

    [RelayCommand(AllowConcurrentExecutions = true)]
    private Task SaveHudAutoHide()
    {
        // Snap the slider to the value that is saved, so the thumb shows the stored number.
        HudAutoHideSeconds = ClampRound(HudAutoHideSeconds, HudMinSeconds, HudMaxSeconds);
        return SaveAsync(current => current with
        {
            HudAutoHideSeconds = ClampRound(HudAutoHideSeconds, HudMinSeconds, HudMaxSeconds),
        });
    }

    [RelayCommand(AllowConcurrentExecutions = true)]
    private Task SaveNewTagDays()
    {
        NewTagDays = ClampRound(NewTagDays, NewTagMinDays, NewTagMaxDays);
        return SaveAsync(current => current with
        {
            NewTagDays = ClampRound(NewTagDays, NewTagMinDays, NewTagMaxDays),
        });
    }

    [RelayCommand(AllowConcurrentExecutions = true)]
    private Task SaveSkipLength()
    {
        // Snap the slider to the value that is saved, so the thumb shows the stored number.
        SkipIndex = ClampRound(SkipIndex, 0, SkipOptions.Length - 1);
        return SaveAsync(current => current with
        {
            SkipSeconds = SkipOptions[ClampRound(SkipIndex, 0, SkipOptions.Length - 1)],
        });
    }

    [RelayCommand(AllowConcurrentExecutions = true)]
    private Task SaveLongPressSpeed()
    {
        // Snap the slider to the value that is saved, so the thumb shows the stored number.
        LongPressSpeed = SnapSpeed(LongPressSpeed);
        return SaveAsync(current => current with { LongPressSpeed = (float)SnapSpeed(LongPressSpeed) });
    }

    [RelayCommand(AllowConcurrentExecutions = true)]
    private Task SaveNameSize()
    {
        // Snap the slider to the value that is saved, so the thumb shows the stored number.
        NameSizePercent = SnapPercent(NameSizePercent);
        return SaveAsync(current => current with { ExplorerFontScale = SnapPercent(NameSizePercent) / 100 });
    }

    [RelayCommand(AllowConcurrentExecutions = true)]
    private Task SaveOverlayMargin()
    {
        // Snap the slider to the value that is saved, so the thumb shows the stored number.
        OverlayMargin = ClampRound(OverlayMargin, PlaybackOverlaySettings.MinMarginDp, PlaybackOverlaySettings.MaxMarginDp);
        return SaveOverlay(overlay => overlay with { MarginDp = (int)OverlayMargin });
    }

    [RelayCommand(AllowConcurrentExecutions = true)]
    private Task SaveOverlaySize()
    {
        // Snap the slider to the value that is saved, so the thumb shows the stored number.
        OverlaySize = ClampRound(OverlaySize, PlaybackOverlaySettings.MinFontSize, PlaybackOverlaySettings.MaxFontSize);
        return SaveOverlay(overlay => overlay with { FontSize = (int)OverlaySize });
    }

    partial void OnOverlayProgressLineChanged(bool value)
    {
        if (!_loading)
        {
            _ = SaveOverlay(overlay => overlay with { ProgressLine = value });
        }
    }

    partial void OnOverlayClockChanged(bool value)
    {
        if (!_loading)
        {
            _ = SaveOverlay(overlay => overlay with { Clock = value });
        }
    }

    partial void OnOverlayBatteryChanged(bool value)
    {
        if (!_loading)
        {
            _ = SaveOverlay(overlay => overlay with { Battery = value });
        }
    }

    // A radio button that is switched off is ignored: the newly checked one saves.
    private void ChooseEdge(OverlayEdge value, bool isChosen)
    {
        if (!isChosen || _loading || EdgeChoice == value)
        {
            return;
        }

        EdgeChoice = value;
        _ = SaveOverlay(overlay => overlay with { ProgressLineEdge = value });
    }

    private void ChooseTime(OverlayTimeMode value, bool isChosen)
    {
        if (!isChosen || _loading || TimeChoice == value)
        {
            return;
        }

        TimeChoice = value;
        _ = SaveOverlay(overlay => overlay with { Time = value });
    }

    private void ChooseCorner(OverlayCorner value, bool isChosen)
    {
        if (!isChosen || _loading || CornerChoice == value)
        {
            return;
        }

        CornerChoice = value;
        _ = SaveOverlay(overlay => overlay with { Corner = value });
    }

    private void ChooseColor(uint value, bool isChosen)
    {
        if (!isChosen || _loading || ColorChoice == value)
        {
            return;
        }

        ColorChoice = value;
        _ = SaveOverlay(overlay => overlay with { ColorArgb = value });
    }

    private Task SaveOverlay(Func<PlaybackOverlaySettings, PlaybackOverlaySettings> change) =>
        SaveAsync(current => current with { Overlay = change(current.Overlay) });

    partial void OnShowClockChanged(bool value)
    {
        if (!_loading)
        {
            _ = SaveAsync(current => current with { ShowClock = value });
        }
    }

    partial void OnShowBatteryChanged(bool value)
    {
        if (!_loading)
        {
            _ = SaveAsync(current => current with { ShowBattery = value });
        }
    }

    partial void OnIncludeAudioOnlyFoldersChanged(bool value)
    {
        if (!_loading)
        {
            _ = SaveAsync(current => current with { IncludeAudioOnlyFolders = value });
        }
    }

    partial void OnIsLanguageSystemChanged(bool value) => SaveLanguage(value, AppLanguage.System);

    partial void OnIsLanguageEnglishChanged(bool value) => SaveLanguage(value, AppLanguage.English);

    partial void OnIsLanguageHebrewChanged(bool value) => SaveLanguage(value, AppLanguage.Hebrew);

    // A radio button that is switched off is ignored: the newly checked one saves.
    private void SaveLanguage(bool isChosen, AppLanguage language)
    {
        if (_loading || !isChosen)
        {
            return;
        }

        _ = SaveAsync(current => current with { Language = language });
    }

    partial void OnShowDurationChanged(bool value) => SaveExplorerField(ExplorerInfoFields.Duration, value);

    partial void OnShowResolutionChanged(bool value) => SaveExplorerField(ExplorerInfoFields.Resolution, value);

    partial void OnShowSizeChanged(bool value) => SaveExplorerField(ExplorerInfoFields.Size, value);

    partial void OnShowDateChanged(bool value) => SaveExplorerField(ExplorerInfoFields.Date, value);

    partial void OnShowPathChanged(bool value) => SaveExplorerField(ExplorerInfoFields.Path, value);

    private void SaveExplorerField(ExplorerInfoFields flag, bool isChecked)
    {
        if (_loading)
        {
            return;
        }

        _ = SaveAsync(current => current with
        {
            ExplorerFields = isChecked ? current.ExplorerFields | flag : current.ExplorerFields & ~flag,
        });
    }

    private async Task SaveAsync(Func<UiSettings, UiSettings> change)
    {
        // Counts the saves that are waiting for the gate or running, so the reload runs once, after the last one.
        Interlocked.Increment(ref _pendingSaves);
        await _saveGate.WaitAsync();
        try
        {
            var current = await _store.GetAsync(CancellationToken.None);
            await _store.SaveAsync(change(current), CancellationToken.None);
            ErrorMessage = null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
            ErrorMessage = _localizer["Interface.SaveFailed"];
            _reloadNeeded = true;
        }
        finally
        {
            // Show the stored values again, so the page does not keep a value that was not saved.
            // The reload waits until no other save is queued, so it cannot show values from before a later save.
            if (Interlocked.Decrement(ref _pendingSaves) == 0 && _reloadNeeded)
            {
                _reloadNeeded = false;
                try
                {
                    await PopulateAsync();
                }
                catch (Exception)
                {
                    // The save error stays visible if the reload fails too.
                }
            }

            _saveGate.Release();
        }
    }

    // The index of the skip option nearest to the stored value; on a tie the shorter one.
    private static double NearestSkipIndex(int seconds)
    {
        var nearest = 0;
        for (var i = 1; i < SkipOptions.Length; i++)
        {
            if (Math.Abs(SkipOptions[i] - seconds) < Math.Abs(SkipOptions[nearest] - seconds))
            {
                nearest = i;
            }
        }

        return nearest;
    }

    private static int ClampRound(double value, int min, int max) =>
        Math.Clamp((int)Math.Round(value, MidpointRounding.AwayFromZero), min, max);

    // Steps of 0.25 from 0.25x to 4.0x.
    private static double SnapSpeed(double value) =>
        Math.Clamp(Math.Round(value * 4, MidpointRounding.AwayFromZero) / 4, MinLongPressSpeed, MaxLongPressSpeed);

    // Steps of 10% from 80% to 160%.
    private static double SnapPercent(double value) =>
        Math.Clamp(Math.Round(value / 10, MidpointRounding.AwayFromZero) * 10, MinNameSizePercent, MaxNameSizePercent);
}
