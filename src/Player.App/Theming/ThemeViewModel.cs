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
    private ThemeSettings _desired;
    private bool _loading;
    private CancellationTokenSource? _opacityDebounce;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HudOpacityText))]
    private double _hudOpacityPercent;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    private string? _errorMessage;

    public ThemeViewModel(IThemeService themeService, ILocalizer localizer)
    {
        ArgumentNullException.ThrowIfNull(themeService);
        ArgumentNullException.ThrowIfNull(localizer);

        _themeService = themeService;
        _desired = themeService.Current;

        Themes =
        [
            new ThemeChoice(ThemeId.DarkOled, localizer["Theme.DarkOled"]),
            new ThemeChoice(ThemeId.DarkSlate, localizer["Theme.DarkSlate"]),
            new ThemeChoice(ThemeId.Light, localizer["Theme.Light"]),
            new ThemeChoice(ThemeId.HighContrast, localizer["Theme.HighContrast"]),
        ];
        Accents = [.. AccentPalette.Colors.Select(argb => new AccentSwatch(argb))];

        Refresh();
    }

    public IReadOnlyList<ThemeChoice> Themes { get; }

    public IReadOnlyList<AccentSwatch> Accents { get; }

    public string HudOpacityText => $"{(int)Math.Round(HudOpacityPercent)}%";

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

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
        try
        {
            ErrorMessage = null;
            await _themeService.ApplyAsync(settings, CancellationToken.None);
        }
        catch (OperationCanceledException)
        {
            // Nothing to report.
        }
        catch (Exception ex)
        {
            // The theme is applied even when the save failed; the page says so.
            ErrorMessage = ex.Message;
        }
    }

    private static double ToOpacity(double percent) => Math.Clamp(Math.Round(percent), 0, 100) / 100.0;
}
