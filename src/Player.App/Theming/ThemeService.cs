using Player.Core.Contracts;
using Player.Core.Logic.Theming;
using Player.Core.Models;
using Color = Microsoft.Maui.Graphics.Color;

namespace Player.App.Theming;

public sealed class ThemeService : IThemeService
{
    public ThemeSettings Current { get; private set; } = new();

    public event EventHandler<ThemeSettings>? Changed;

    public Task InitializeAsync(CancellationToken ct) => ApplyAsync(new ThemeSettings(), ct);

    public async Task ApplyAsync(ThemeSettings settings, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ct.ThrowIfCancellationRequested();

        var resolved = ThemeResolver.Resolve(settings);
        await MainThread.InvokeOnMainThreadAsync(() => WriteResources(resolved)).ConfigureAwait(false);

        Current = settings;
        Changed?.Invoke(this, settings);
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
