using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Player.Core.Contracts;
using Player.Core.Logic.Theming;
using Player.Core.Models;

namespace Player.App.Theming;

/// <summary>
/// The theme page. Every change is applied at once through <see cref="IThemeService"/>. The page keeps its own copy of the
/// wanted settings and updates it on each tap, so two quick taps on different parts never undo each other.
/// </summary>
public sealed partial class ThemeViewModel : ObservableObject
{
    private static readonly TimeSpan OpacityDebounce = TimeSpan.FromMilliseconds(150);

    private readonly IThemeService _themeService;
    private readonly ILocalizer _localizer;
    private ThemeSettings _desired;
    private bool _loading;
    private int _applyVersion;
    private CancellationTokenSource? _opacityDebounce;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HudOpacityText))]
    private double _hudOpacityPercent;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ErrorMessage))]
    private bool _hasError;

    public ThemeViewModel(IThemeService themeService, ILocalizer localizer)
    {
        ArgumentNullException.ThrowIfNull(themeService);
        ArgumentNullException.ThrowIfNull(localizer);

        _themeService = themeService;
        _localizer = localizer;
        _desired = themeService.Current;

        Themes =
        [
            new ThemeChoice(ThemeId.DarkOled, string.Empty),
            new ThemeChoice(ThemeId.DarkSlate, string.Empty),
            new ThemeChoice(ThemeId.Light, string.Empty),
            new ThemeChoice(ThemeId.HighContrast, string.Empty),
        ];
        Accents = [.. AccentPalette.Colors.Select(argb => new AccentSwatch(argb))];

        RefreshTexts();
        Refresh();
    }

    public IReadOnlyList<ThemeChoice> Themes { get; }

    public IReadOnlyList<AccentSwatch> Accents { get; }

    public string HudOpacityText => $"{(int)Math.Round(HudOpacityPercent)}%";

    /// <summary>The localized text of a failed save; null when there is no error.</summary>
    public string? ErrorMessage => HasError ? _localizer["Theme.SaveFailed"] : null;

    /// <summary>Starts following language changes. Call it when the page appears.</summary>
    public void Attach()
    {
        _localizer.Changed -= OnLanguageChanged;
        _localizer.Changed += OnLanguageChanged;
        RefreshTexts();
    }

    /// <summary>Stops following language changes. Call it when the page disappears.</summary>
    public void Detach() => _localizer.Changed -= OnLanguageChanged;

    private void OnLanguageChanged(object? sender, EventArgs e) => RefreshTexts();

    private void RefreshTexts()
    {
        foreach (var theme in Themes)
        {
            theme.Name = _localizer[theme.Id switch
            {
                ThemeId.DarkOled => "Theme.DarkOled",
                ThemeId.DarkSlate => "Theme.DarkSlate",
                ThemeId.Light => "Theme.Light",
                _ => "Theme.HighContrast",
            }];
        }

        OnPropertyChanged(nameof(ErrorMessage));
    }

    /// <summary>Shows the settings in effect. Call it when the page appears.</summary>
    public void Refresh()
    {
        _desired = _themeService.Current;
        _loading = true;
        try
        {
            foreach (var theme in Themes)
            {
                theme.IsSelected = theme.Id == _desired.Theme;
            }

            var accent = _desired.AccentColorArgb | 0xFF000000u;
            foreach (var swatch in Accents)
            {
                swatch.IsSelected = swatch.Argb == accent;
            }

            HudOpacityPercent = Math.Round(Math.Clamp(_desired.HudOpacity, 0, 1) * 100);
        }
        finally
        {
            _loading = false;
        }
    }

    [RelayCommand(AllowConcurrentExecutions = true)]
    private Task SelectTheme(ThemeChoice? choice)
    {
        if (choice is null)
        {
            return Task.CompletedTask;
        }

        foreach (var theme in Themes)
        {
            theme.IsSelected = ReferenceEquals(theme, choice);
        }

        _desired = _desired with { Theme = choice.Id };
        return ApplyDesiredAsync();
    }

    [RelayCommand(AllowConcurrentExecutions = true)]
    private Task SelectAccent(AccentSwatch? swatch)
    {
        if (swatch is null)
        {
            return Task.CompletedTask;
        }

        foreach (var other in Accents)
        {
            other.IsSelected = ReferenceEquals(other, swatch);
        }

        _desired = _desired with { AccentColorArgb = swatch.Argb };
        return ApplyDesiredAsync();
    }

    /// <summary>Applies the opacity at once; the slider calls it when the drag ends.</summary>
    [RelayCommand(AllowConcurrentExecutions = true)]
    private Task ApplyHudOpacity()
    {
        CancelDebounce();
        _desired = _desired with { HudOpacity = ToOpacity(HudOpacityPercent) };
        return ApplyDesiredAsync();
    }

    partial void OnHudOpacityPercentChanged(double value)
    {
        if (_loading)
        {
            return;
        }

        // Live preview while dragging: the value is applied after a short pause in the movement.
        _desired = _desired with { HudOpacity = ToOpacity(value) };
        var cts = new CancellationTokenSource();
        CancelDebounce();
        _opacityDebounce = cts;
        _ = ApplyAfterPauseAsync(cts.Token);
    }

    private async Task ApplyAfterPauseAsync(CancellationToken ct)
    {
        try
        {
            await Task.Delay(OpacityDebounce, ct);
            await ApplyDesiredAsync();
        }
        catch (OperationCanceledException)
        {
            // A newer value replaced this one.
        }
    }

    private void CancelDebounce()
    {
        var previous = Interlocked.Exchange(ref _opacityDebounce, null);
        previous?.Cancel();
    }

    private async Task ApplyDesiredAsync()
    {
        var settings = _desired;
        var version = Interlocked.Increment(ref _applyVersion);
        try
        {
            await _themeService.ApplyAsync(settings, CancellationToken.None);
            if (version == Volatile.Read(ref _applyVersion))
            {
                HasError = false;
            }
        }
        catch (OperationCanceledException)
        {
            // Nothing to report.
        }
        catch (Exception ex)
        {
            // The theme is applied even when the save failed; the page says so. Older applies stay silent.
            System.Diagnostics.Debug.WriteLine($"Theme could not be saved: {ex}");
            if (version == Volatile.Read(ref _applyVersion))
            {
                HasError = true;
            }
        }
    }

    private static double ToOpacity(double percent) => Math.Clamp(Math.Round(percent), 0, 100) / 100.0;
}
