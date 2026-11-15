using Player.Core.Contracts;
using Player.Core.Logic.Theming;
using Player.Core.Models;
using Player.Data.ThemeStorage;
using System.Diagnostics;
using Color = Microsoft.Maui.Graphics.Color;

namespace Player.App.Theming;

/// <summary>
/// Applies a theme at runtime: tokens into the application resources, the app theme (light or dark) and the
/// Android system bars. Settings come from and go to <see cref="ThemeSettingsRepository"/>.
/// </summary>
public sealed partial class ThemeService : IThemeService
{
    private readonly ThemeSettingsRepository _repository;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly Lock _stateLock = new();
    private ThemeSettings _current = new();
    private uint _barSurface;
    private bool _barDark = true;
    private bool _hasTheme;

    public ThemeService(ThemeSettingsRepository repository)
    {
        ArgumentNullException.ThrowIfNull(repository);
        _repository = repository;
    }

    public ThemeSettings Current
    {
        get
        {
            lock (_stateLock)
            {
                return _current;
            }
        }
    }

    public event EventHandler<ThemeSettings>? Changed;

    public async Task InitializeAsync(CancellationToken ct)
    {
        await _gate.WaitAsync(ct);
        try
        {
            // The defaults go in first and synchronously when called on the UI thread, so the tokens exist
            // before the first page is created. The saved theme follows as soon as it is read.
            await ApplyCoreAsync(Current, raiseChanged: false);

            ThemeSettings saved;
            try
            {
                saved = await _repository.LoadAsync(ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                Debug.WriteLine($"Theme settings could not be read: {ex}");
                return;
            }

            if (saved != Current)
            {
                await ApplyCoreAsync(saved, raiseChanged: true);
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task ApplyAsync(ThemeSettings settings, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ct.ThrowIfCancellationRequested();

        await _gate.WaitAsync(ct);
        try
        {
            await ApplyCoreAsync(settings, raiseChanged: true);

            // A failing save reaches the caller; the theme stays applied.
            await _repository.SaveAsync(settings, ct);
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>
    /// Applies the system bar colors and icon brightness of the current theme to the current activity, if there is one.
    /// The activity calls this when it is created or resumed. Without an activity it does nothing.
    /// </summary>
    public void ApplySystemBars()
    {
        uint surface;
        bool dark;
        lock (_stateLock)
        {
            if (!_hasTheme)
            {
                return;
            }

            surface = _barSurface;
            dark = _barDark;
        }

        if (MainThread.IsMainThread)
        {
            ApplyBarsCore(surface, dark);
        }
        else
        {
            MainThread.BeginInvokeOnMainThread(() => ApplyBarsCore(surface, dark));
        }
    }

    partial void ApplyBarsCore(uint surface, bool dark);

    private Task ApplyCoreAsync(ThemeSettings settings, bool raiseChanged)
    {
        var resolved = ThemeResolver.Resolve(settings);
        var dark = ThemeBrightness.IsDark(settings.Theme);

        return MainThread.InvokeOnMainThreadAsync(() =>
        {
            WriteResources(resolved);

            // On Android MAUI maps this to the AppCompat night mode: native widgets, popups and dialogs follow.
            Application.Current!.UserAppTheme = dark ? AppTheme.Dark : AppTheme.Light;

            lock (_stateLock)
            {
                _current = settings;
                _barSurface = resolved[ThemeTokens.ColorSurface];
                _barDark = dark;
                _hasTheme = true;
            }

            ApplySystemBars();

            if (raiseChanged)
            {
                RaiseChanged(settings);
            }
        });
    }

    private void RaiseChanged(ThemeSettings settings)
    {
        var handler = Changed;
        if (handler is null)
        {
            return;
        }

        foreach (var subscriber in handler.GetInvocationList())
        {
            try
            {
                ((EventHandler<ThemeSettings>)subscriber).Invoke(this, settings);
            }
            catch (Exception ex)
            {
                // A failing subscriber must not undo the theme.
                Debug.WriteLine($"Theme change subscriber failed: {ex}");
            }
        }
    }

    private static void WriteResources(IReadOnlyDictionary<string, uint> resolved)
    {
        var resources = Application.Current?.Resources
            ?? throw new InvalidOperationException("Application resources are not available yet.");

        foreach (var (token, argb) in resolved)
        {
            resources[token] = Color.FromUint(argb);
        }
    }
}
