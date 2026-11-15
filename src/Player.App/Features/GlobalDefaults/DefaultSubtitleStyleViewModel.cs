using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Player.Core.Contracts;
using Player.Core.Models;

namespace Player.App.Features.DefaultsSettings;

/// <summary>
/// The default subtitle style page. Every change is saved at once through <see cref="SaveAsync"/>,
/// which starts from the stored record and changes only <see cref="SubtitleStyle"/>.
/// </summary>
public sealed partial class DefaultSubtitleStyleViewModel : ObservableObject
{
    private const double FontScaleStep = 0.1;
    private const double MinFontScale = 0.5;
    private const double MaxFontScale = 3.0;
    private const int MinOutlineThickness = 0;
    private const int MaxOutlineThickness = 4;
    private const double BackgroundOpacityStep = 0.1;
    private const double MinBackgroundOpacity = 0.0;
    private const double MaxBackgroundOpacity = 1.0;
    private const double BottomMarginStep = 0.01;
    private const double MinBottomMarginFraction = 0.0;
    private const double MaxBottomMarginFraction = 0.30;

    private static readonly (string Name, uint Argb)[] TextPalette =
    {
        ("White", 0xFFFFFFFF),
        ("Yellow", 0xFFFFEB3B),
        ("Cyan", 0xFF4DD0E1),
        ("Green", 0xFF8BC34A),
        ("Pink", 0xFFFF80AB),
        ("Light gray", 0xFFCCCCCC),
    };

    private static readonly (string Name, uint Argb)[] OutlinePalette =
    {
        ("Black", 0xFF000000),
        ("Dark gray", 0xFF333333),
        ("Navy", 0xFF0D1B4C),
        ("White", 0xFFFFFFFF),
    };

    private readonly IGlobalDefaultsStore _store;

    // True while the stored style is copied into the properties: no property change saves then.
    private bool _loading;

    // Saves run one at a time: each one reads the stored record, changes the style and writes it back.
    private readonly SemaphoreSlim _saveGate = new(1, 1);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FontScaleText))]
    private double _fontScale = SubtitleStyle.Default.FontScale;

    [ObservableProperty]
    private uint _textColorArgb = SubtitleStyle.Default.TextColorArgb;

    [ObservableProperty]
    private uint _outlineColorArgb = SubtitleStyle.Default.OutlineColorArgb;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(OutlineThicknessText))]
    private int _outlineThickness = SubtitleStyle.Default.OutlineThickness;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(BackgroundOpacityText))]
    private double _backgroundOpacity = SubtitleStyle.Default.BackgroundOpacity;

    [ObservableProperty]
    private bool _bold = SubtitleStyle.Default.Bold;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(BottomMarginText))]
    private double _bottomMarginFraction = SubtitleStyle.Default.BottomMarginFraction;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private bool _hasError;

    public DefaultSubtitleStyleViewModel(IGlobalDefaultsStore store)
    {
        _store = store;
        TextColors = CreateSwatches(TextPalette);
        OutlineColors = CreateSwatches(OutlinePalette);
        MarkSelected(TextColors, TextColorArgb);
        MarkSelected(OutlineColors, OutlineColorArgb);
    }

    public IReadOnlyList<ColorSwatch> TextColors { get; }

    public IReadOnlyList<ColorSwatch> OutlineColors { get; }

    public string FontScaleText => FontScale.ToString("0%", CultureInfo.InvariantCulture);

    public string OutlineThicknessText => OutlineThickness.ToString(CultureInfo.InvariantCulture);

    public string BackgroundOpacityText => BackgroundOpacity.ToString("0%", CultureInfo.InvariantCulture);

    public string BottomMarginText => BottomMarginFraction.ToString("0%", CultureInfo.InvariantCulture);

    /// <summary>Copies the stored subtitle style into the page. Store failures are shown in <see cref="ErrorMessage"/>.</summary>
    public async Task LoadAsync()
    {
        _loading = true;
        try
        {
            var stored = await _store.GetAsync(CancellationToken.None);
            ApplyStyle(stored.SubtitleStyle);
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

    partial void OnTextColorArgbChanged(uint value)
    {
        MarkSelected(TextColors, value);
    }

    partial void OnOutlineColorArgbChanged(uint value)
    {
        MarkSelected(OutlineColors, value);
    }

    partial void OnBoldChanged(bool value)
    {
        if (_loading)
        {
            return;
        }

        _ = SaveAsync(style => style with { Bold = Bold });
    }

    [RelayCommand(AllowConcurrentExecutions = true)]
    private Task FontScaleDown()
    {
        FontScale = Step(FontScale, -FontScaleStep, MinFontScale, MaxFontScale);
        return SaveAsync(style => style with { FontScale = FontScale });
    }

    [RelayCommand(AllowConcurrentExecutions = true)]
    private Task FontScaleUp()
    {
        FontScale = Step(FontScale, FontScaleStep, MinFontScale, MaxFontScale);
        return SaveAsync(style => style with { FontScale = FontScale });
    }

    [RelayCommand(AllowConcurrentExecutions = true)]
    private Task SelectTextColor(ColorSwatch? swatch)
    {
        if (swatch is null)
        {
            return Task.CompletedTask;
        }

        TextColorArgb = swatch.Argb;
        return SaveAsync(style => style with { TextColorArgb = TextColorArgb });
    }

    [RelayCommand(AllowConcurrentExecutions = true)]
    private Task SelectOutlineColor(ColorSwatch? swatch)
    {
        if (swatch is null)
        {
            return Task.CompletedTask;
        }

        OutlineColorArgb = swatch.Argb;
        return SaveAsync(style => style with { OutlineColorArgb = OutlineColorArgb });
    }

    [RelayCommand(AllowConcurrentExecutions = true)]
    private Task OutlineThinner()
    {
        OutlineThickness = Math.Clamp(OutlineThickness - 1, MinOutlineThickness, MaxOutlineThickness);
        return SaveAsync(style => style with { OutlineThickness = OutlineThickness });
    }

    [RelayCommand(AllowConcurrentExecutions = true)]
    private Task OutlineThicker()
    {
        OutlineThickness = Math.Clamp(OutlineThickness + 1, MinOutlineThickness, MaxOutlineThickness);
        return SaveAsync(style => style with { OutlineThickness = OutlineThickness });
    }

    [RelayCommand(AllowConcurrentExecutions = true)]
    private Task BackgroundLighter()
    {
        BackgroundOpacity = Step(BackgroundOpacity, -BackgroundOpacityStep, MinBackgroundOpacity, MaxBackgroundOpacity);
        return SaveAsync(style => style with { BackgroundOpacity = BackgroundOpacity });
    }

    [RelayCommand(AllowConcurrentExecutions = true)]
    private Task BackgroundDarker()
    {
        BackgroundOpacity = Step(BackgroundOpacity, BackgroundOpacityStep, MinBackgroundOpacity, MaxBackgroundOpacity);
        return SaveAsync(style => style with { BackgroundOpacity = BackgroundOpacity });
    }

    [RelayCommand(AllowConcurrentExecutions = true)]
    private Task BottomMarginLower()
    {
        BottomMarginFraction = Step(BottomMarginFraction, -BottomMarginStep, MinBottomMarginFraction, MaxBottomMarginFraction);
        return SaveAsync(style => style with { BottomMarginFraction = BottomMarginFraction });
    }

    [RelayCommand(AllowConcurrentExecutions = true)]
    private Task BottomMarginHigher()
    {
        BottomMarginFraction = Step(BottomMarginFraction, BottomMarginStep, MinBottomMarginFraction, MaxBottomMarginFraction);
        return SaveAsync(style => style with { BottomMarginFraction = BottomMarginFraction });
    }

    [RelayCommand(AllowConcurrentExecutions = true)]
    private Task ResetStyle()
    {
        // The properties show the defaults without saving each one; the single save below stores the default style.
        _loading = true;
        try
        {
            ApplyStyle(SubtitleStyle.Default);
        }
        finally
        {
            _loading = false;
        }

        return SaveAsync(_ => SubtitleStyle.Default);
    }

    private void ApplyStyle(SubtitleStyle style)
    {
        FontScale = style.FontScale;
        TextColorArgb = style.TextColorArgb;
        OutlineColorArgb = style.OutlineColorArgb;
        OutlineThickness = style.OutlineThickness;
        BackgroundOpacity = style.BackgroundOpacity;
        Bold = style.Bold;
        BottomMarginFraction = style.BottomMarginFraction;
    }

    // Every save starts from the stored record, so the other global defaults are preserved.
    // A change that leaves the style equal to the stored one writes nothing.
    private async Task SaveAsync(Func<SubtitleStyle, SubtitleStyle> change)
    {
        await _saveGate.WaitAsync(CancellationToken.None);
        try
        {
            var stored = await _store.GetAsync(CancellationToken.None);
            var style = change(stored.SubtitleStyle);
            if (style != stored.SubtitleStyle)
            {
                await _store.SaveAsync(stored with { SubtitleStyle = style }, CancellationToken.None);
            }

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

    // Rounded after each step so that 0.1 steps do not drift, then clamped to the range.
    private static double Step(double value, double delta, double min, double max) =>
        Math.Clamp(Math.Round(value + delta, 2, MidpointRounding.AwayFromZero), min, max);

    private static IReadOnlyList<ColorSwatch> CreateSwatches((string Name, uint Argb)[] palette) =>
        palette.Select(entry => new ColorSwatch(entry.Argb, entry.Name)).ToList();

    private static void MarkSelected(IReadOnlyList<ColorSwatch> swatches, uint argb)
    {
        foreach (var swatch in swatches)
        {
            swatch.IsSelected = swatch.Argb == argb;
        }
    }
}
