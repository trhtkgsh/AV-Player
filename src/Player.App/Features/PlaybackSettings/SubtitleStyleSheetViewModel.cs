using System.Diagnostics;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Controls;
using Player.Core.Contracts;
using Player.Core.Models;

namespace Player.App.Features.PlaybackSettings;

/// <summary>
/// State and commands of the subtitle style sheet. Observes nothing; never owns or disposes the engine.
/// Each change is applied to the running playback 500 ms after the last change, because every apply restarts
/// the picture. Only "Save as default" writes to the global defaults.
/// </summary>
public sealed partial class SubtitleStyleSheetViewModel : ObservableObject
{
    private const int DebounceMs = 500;
    private const double MinFontScale = 0.5;
    private const double MaxFontScale = 3.0;
    private const double FontScaleStep = 0.1;
    private const int MinOutlineThickness = 0;
    private const int MaxOutlineThickness = 3;
    private const double MinOpacity = 0.0;
    private const double MaxOpacity = 1.0;
    private const double OpacityStep = 0.1;
    private const double MinMargin = 0.0;
    private const double MaxMargin = 0.30;
    private const double MarginStep = 0.01;

    private readonly IPlayerEngine _engine;
    private readonly IGlobalDefaultsStore _defaults;
    private readonly SemaphoreSlim _applyGate = new(1, 1);
    private CancellationTokenSource? _pending;
    private bool _dirty;
    private bool _loading;
    private bool _closing;

    [ObservableProperty, NotifyPropertyChangedFor(nameof(FontScaleText))]
    private double _fontScale = SubtitleStyle.Default.FontScale;

    [ObservableProperty] private uint _textColorArgb = SubtitleStyle.Default.TextColorArgb;
    [ObservableProperty] private uint _outlineColorArgb = SubtitleStyle.Default.OutlineColorArgb;

    [ObservableProperty, NotifyPropertyChangedFor(nameof(OutlineText))]
    private int _outlineThickness = SubtitleStyle.Default.OutlineThickness;

    [ObservableProperty, NotifyPropertyChangedFor(nameof(BackgroundText))]
    private double _backgroundOpacity = SubtitleStyle.Default.BackgroundOpacity;

    [ObservableProperty] private bool _bold = SubtitleStyle.Default.Bold;

    [ObservableProperty, NotifyPropertyChangedFor(nameof(MarginText))]
    private double _bottomMargin = SubtitleStyle.Default.BottomMarginFraction;

    [ObservableProperty] private string _statusMessage = "";
    [ObservableProperty, NotifyPropertyChangedFor(nameof(HasError))] private string _errorMessage = "";

    /// <summary>Text colors of the palette. The selected one has <see cref="StyleSwatch.IsSelected"/> set.</summary>
    public IReadOnlyList<StyleSwatch> TextColors { get; }

    /// <summary>Outline colors of the palette.</summary>
    public IReadOnlyList<StyleSwatch> OutlineColors { get; }

    public string FontScaleText => FormatPercent(FontScale);

    public string OutlineText => OutlineThickness.ToString(CultureInfo.InvariantCulture);

    public string BackgroundText => FormatPercent(BackgroundOpacity);

    public string MarginText => FormatPercent(BottomMargin);

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    public SubtitleStyleSheetViewModel(IPlayerEngine engine, IGlobalDefaultsStore defaults)
    {
        _engine = engine;
        _defaults = defaults;

        TextColors = new List<StyleSwatch>
        {
            new(0xFFFFFFFF, "White"),
            new(0xFFFFEB3B, "Yellow"),
            new(0xFF4DD0E1, "Cyan"),
            new(0xFF8BC34A, "Green"),
            new(0xFFFF80AB, "Pink"),
            new(0xFFCCCCCC, "Light gray"),
        };

        OutlineColors = new List<StyleSwatch>
        {
            new(0xFF000000, "Black"),
            new(0xFF333333, "Dark gray"),
            new(0xFF0D1B4C, "Navy"),
            new(0xFFFFFFFF, "White"),
        };
    }

    /// <summary>Called when the sheet appears. Shows the style in effect. Applies nothing.</summary>
    public void Activate()
    {
        StatusMessage = "";
        ErrorMessage = "";
        ReadFromStyle(_engine.SubtitleStyle);
    }

    /// <summary>Called when the sheet disappears. Applies a pending change at once, so closing does not lose it.</summary>
    public void Deactivate()
    {
        if (!_dirty) return;

        CancelPending();
        _ = ApplyAsync();
    }

    [RelayCommand]
    private void Smaller() => FontScale = Math.Clamp(Round2(FontScale - FontScaleStep), MinFontScale, MaxFontScale);

    [RelayCommand]
    private void Larger() => FontScale = Math.Clamp(Round2(FontScale + FontScaleStep), MinFontScale, MaxFontScale);

    [RelayCommand]
    private void SelectTextColor(StyleSwatch swatch) => TextColorArgb = swatch.Argb;

    [RelayCommand]
    private void SelectOutlineColor(StyleSwatch swatch) => OutlineColorArgb = swatch.Argb;

    [RelayCommand]
    private void Thinner() => OutlineThickness = Math.Clamp(OutlineThickness - 1, MinOutlineThickness, MaxOutlineThickness);

    [RelayCommand]
    private void Thicker() => OutlineThickness = Math.Clamp(OutlineThickness + 1, MinOutlineThickness, MaxOutlineThickness);

    [RelayCommand]
    private void Lighter() => BackgroundOpacity = Math.Clamp(Round2(BackgroundOpacity - OpacityStep), MinOpacity, MaxOpacity);

    [RelayCommand]
    private void Darker() => BackgroundOpacity = Math.Clamp(Round2(BackgroundOpacity + OpacityStep), MinOpacity, MaxOpacity);

    [RelayCommand]
    private void Lower() => BottomMargin = Math.Clamp(Round2(BottomMargin - MarginStep), MinMargin, MaxMargin);

    [RelayCommand]
    private void Higher() => BottomMargin = Math.Clamp(Round2(BottomMargin + MarginStep), MinMargin, MaxMargin);

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

    /// <summary>Saves the current style as the global default. This is the only write to the global defaults.</summary>
    [RelayCommand]
    private async Task SaveAsDefaultAsync()
    {
        try
        {
            await FlushAsync();
            GlobalDefaults current = await _defaults.GetAsync(CancellationToken.None);
            await _defaults.SaveAsync(current with { SubtitleStyle = CurrentStyle() }, CancellationToken.None);
            ErrorMessage = "";
            StatusMessage = "Saved as the default style.";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    /// <summary>Puts the default style into the controls and applies it at once.</summary>
    [RelayCommand]
    private async Task RestoreDefaultAsync()
    {
        try
        {
            GlobalDefaults current = await _defaults.GetAsync(CancellationToken.None);
            ReadFromStyle(current.SubtitleStyle);
            CancelPending();
            await ApplyAsync();
            StatusMessage = "";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    partial void OnFontScaleChanged(double value) => OnStyleChanged();

    partial void OnTextColorArgbChanged(uint value)
    {
        SelectSwatch(TextColors, value);
        OnStyleChanged();
    }

    partial void OnOutlineColorArgbChanged(uint value)
    {
        SelectSwatch(OutlineColors, value);
        OnStyleChanged();
    }

    partial void OnOutlineThicknessChanged(int value) => OnStyleChanged();

    partial void OnBackgroundOpacityChanged(double value) => OnStyleChanged();

    partial void OnBoldChanged(bool value) => OnStyleChanged();

    partial void OnBottomMarginChanged(double value) => OnStyleChanged();

    /// <summary>Restarts the debounce. The style is applied 500 ms after the last change.</summary>
    private void OnStyleChanged()
    {
        if (_loading) return;

        _dirty = true;
        CancelPending();

        var pending = new CancellationTokenSource();
        _pending = pending;
        _ = ApplyAfterDelayAsync(pending.Token);
    }

    private async Task ApplyAfterDelayAsync(CancellationToken token)
    {
        try
        {
            await Task.Delay(DebounceMs, token);
        }
        catch (OperationCanceledException)
        {
            return; // a newer change replaced this one
        }

        await ApplyAsync();
    }

    /// <summary>Applies a pending change now, if there is one.</summary>
    private async Task FlushAsync()
    {
        if (!_dirty) return;

        CancelPending();
        await ApplyAsync();
    }

    /// <summary>
    /// Sends the style to the engine. The gate keeps two applies from overlapping. The style is read after the gate,
    /// so the newest values are sent. On failure the controls go back to the engine's style.
    /// </summary>
    private async Task ApplyAsync()
    {
        await _applyGate.WaitAsync();
        try
        {
            _dirty = false;
            await _engine.SetSubtitleStyleAsync(CurrentStyle(), CancellationToken.None);
            ErrorMessage = "";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            ReadFromStyle(_engine.SubtitleStyle);
        }
        finally
        {
            _applyGate.Release();
        }
    }

    private void CancelPending()
    {
        CancellationTokenSource? previous = _pending;
        _pending = null;
        if (previous is null) return;

        previous.Cancel();
        previous.Dispose();
    }

    /// <summary>Shows a style in the controls. Clamps the values; does not schedule an apply.</summary>
    private void ReadFromStyle(SubtitleStyle style)
    {
        _loading = true;
        try
        {
            FontScale = Math.Clamp(Round2(style.FontScale), MinFontScale, MaxFontScale);
            TextColorArgb = style.TextColorArgb;
            OutlineColorArgb = style.OutlineColorArgb;
            OutlineThickness = Math.Clamp(style.OutlineThickness, MinOutlineThickness, MaxOutlineThickness);
            BackgroundOpacity = Math.Clamp(Round2(style.BackgroundOpacity), MinOpacity, MaxOpacity);
            Bold = style.Bold;
            BottomMargin = Math.Clamp(Round2(style.BottomMarginFraction), MinMargin, MaxMargin);
        }
        finally
        {
            _loading = false;
        }

        // A property that keeps its value raises no change, so the check marks are set here as well.
        SelectSwatch(TextColors, TextColorArgb);
        SelectSwatch(OutlineColors, OutlineColorArgb);
    }

    private SubtitleStyle CurrentStyle() =>
        new(FontScale, TextColorArgb, OutlineColorArgb, OutlineThickness, BackgroundOpacity, Bold, BottomMargin);

    private static void SelectSwatch(IEnumerable<StyleSwatch> swatches, uint argb)
    {
        foreach (StyleSwatch swatch in swatches)
        {
            swatch.IsSelected = swatch.Argb == argb;
        }
    }

    private static double Round2(double value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);

    private static string FormatPercent(double fraction) =>
        Math.Round(fraction * 100, 0, MidpointRounding.AwayFromZero).ToString("0", CultureInfo.InvariantCulture) + "%";
}
