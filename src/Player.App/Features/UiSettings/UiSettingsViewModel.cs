using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Player.Core.Contracts;
using Player.Core.Models;

namespace Player.App.Features.InterfaceSettings;

/// <summary>
/// The interface settings page. Every save starts from the stored record, so the fields this page
/// does not show (view mode, sort, skip length, battery) keep their stored values.
/// </summary>
public sealed partial class UiSettingsViewModel : ObservableObject
{
    private const int HudMinSeconds = 1;
    private const int HudMaxSeconds = 10;
    private const int NewTagMinDays = 1;
    private const int NewTagMaxDays = 30;

    private readonly IUiSettingsStore _store;

    // Saves are serialized: each one reads the stored record, so two overlapping saves would drop a change.
    private readonly SemaphoreSlim _saveGate = new(1, 1);

    private bool _loading;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HudAutoHideText))]
    private double _hudAutoHideSeconds = 3;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(NewTagDaysText))]
    private double _newTagDays = 7;

    [ObservableProperty]
    private bool _showClock;

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
    [NotifyPropertyChangedFor(nameof(HasError))]
    private string? _errorMessage;

    public UiSettingsViewModel(IUiSettingsStore store)
    {
        _store = store;
    }

    public string HudAutoHideText => $"{ClampRound(HudAutoHideSeconds, HudMinSeconds, HudMaxSeconds)} s";

    public string NewTagDaysText
    {
        get
        {
            var days = ClampRound(NewTagDays, NewTagMinDays, NewTagMaxDays);
            return days == 1 ? "1 day" : $"{days} days";
        }
    }

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

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

    partial void OnShowClockChanged(bool value)
    {
        if (!_loading)
        {
            _ = SaveAsync(current => current with { ShowClock = value });
        }
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
        await _saveGate.WaitAsync();
        try
        {
            var current = await _store.GetAsync(CancellationToken.None);
            await _store.SaveAsync(change(current), CancellationToken.None);
            ErrorMessage = null;
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;

            // Show the stored values again, so the page does not keep a value that was not saved.
            // The save error stays visible if the reload fails too.
            try
            {
                await PopulateAsync();
            }
            catch (Exception)
            {
                // The save error above stays visible.
            }
        }
        finally
        {
            _saveGate.Release();
        }
    }

    private static int ClampRound(double value, int min, int max) =>
        Math.Clamp((int)Math.Round(value, MidpointRounding.AwayFromZero), min, max);
}
