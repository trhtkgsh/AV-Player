using System.Globalization;
using System.Runtime.CompilerServices;
using Player.Core.Contracts;
using Player.Core.Logic.Localization;
using Player.Core.Models;

namespace Player.App.Localization;

/// <summary>
/// The localizer: the texts in the language in effect and the layout direction of the app windows. It never changes
/// the process culture or the Android locale.
/// </summary>
public sealed class Localizer : ILocalizer
{
    private const string LogTag = "ArielL10n";

    // Text objects created by XAML before the localizer exists; the first localizer adopts them.
    private static readonly Dictionary<string, LocalizedString> Pending = new(StringComparer.Ordinal);
    private static readonly object StaticGate = new();

    private readonly StringCatalog _catalog;
    private readonly IUiSettingsStore _settings;
    private readonly string _deviceLanguage;
    private readonly Dictionary<string, LocalizedString> _texts = new(StringComparer.Ordinal);
    private readonly HashSet<string> _loggedUnknown = new(StringComparer.Ordinal);
    private readonly ConditionalWeakTable<Window, object> _hookedWindows = new();
    private readonly object _gate = new();

    private volatile string _languageCode;
    private volatile bool _isRightToLeft;
    private volatile int _language = (int)AppLanguage.System;
    private volatile bool _initialized;

    /// <summary>
    /// Merges the string sources and starts following the UI settings. Before <see cref="InitializeAsync"/> has run
    /// the System rule applies.
    /// </summary>
    public Localizer(IEnumerable<IStringSource> sources, IUiSettingsStore settings)
    {
        ArgumentNullException.ThrowIfNull(sources);
        ArgumentNullException.ThrowIfNull(settings);

        _catalog = new StringCatalog(sources);
#if DEBUG
        if (_catalog.DuplicateKeys.Count > 0)
        {
            throw new InvalidOperationException(
                "Duplicate string keys: " + string.Join(", ", _catalog.DuplicateKeys));
        }
#endif

        _settings = settings;
        _deviceLanguage = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        _languageCode = StringCatalog.ResolveLanguageCode(AppLanguage.System, _deviceLanguage);
        _isRightToLeft = StringCatalog.IsRightToLeft(_languageCode);

        lock (StaticGate)
        {
            lock (_gate)
            {
                foreach (var (key, text) in Pending)
                {
                    _texts[key] = text;
                }
            }

            Pending.Clear();
            Current = this;
        }

        RefreshTexts();
        _settings.Changed += OnSettingsChanged;
    }

    /// <summary>The localizer of the running app, for markup extensions that cannot use dependency injection.</summary>
    public static Localizer? Current { get; private set; }

    /// <inheritdoc />
    public AppLanguage Language => (AppLanguage)_language;

    /// <inheritdoc />
    public string LanguageCode => _languageCode;

    /// <inheritdoc />
    public bool IsRightToLeft => _isRightToLeft;

    /// <inheritdoc />
    public event EventHandler? Changed;

    /// <inheritdoc />
    public string this[string key] => Resolve(key, _languageCode, static (c, k, l) => c.Get(k, l));

    /// <inheritdoc />
    public string Format(string key, params object?[] args) =>
        Resolve(key, _languageCode, (c, k, l) => c.Format(k, l, args));

    /// <inheritdoc />
    public string Plural(string key, int count) =>
        Resolve(count == 1 ? key + ".One" : key + ".Many", _languageCode, (c, k, l) => c.Format(k, l, count));

    /// <inheritdoc />
    public async Task InitializeAsync(CancellationToken ct)
    {
        var stored = await _settings.GetAsync(ct).ConfigureAwait(false);
        ct.ThrowIfCancellationRequested();
        await ApplyAsync(stored.Language, force: true).ConfigureAwait(false);
        _initialized = true;
    }

    /// <summary>
    /// The observable text of a key, created on demand. Works before a localizer exists (the first one adopts the object).
    /// </summary>
    public static LocalizedString GetText(string key)
    {
        ArgumentNullException.ThrowIfNull(key);

        lock (StaticGate)
        {
            if (Current is { } localizer)
            {
                return localizer.GetOrCreateText(key);
            }

            if (!Pending.TryGetValue(key, out var text))
            {
                text = new LocalizedString(key);
                Pending[key] = text;
            }

            return text;
        }
    }

    /// <summary>
    /// Sets the layout direction of a window and its page, and keeps it for pages and modal pages shown later.
    /// Call it for every window the app creates, for example in <c>App.CreateWindow</c>.
    /// </summary>
    public void ApplyFlowDirection(Window window)
    {
        ArgumentNullException.ThrowIfNull(window);

        // Modal pages and pages that arrive later do not reliably inherit the direction, so they are set explicitly.
        if (!_hookedWindows.TryGetValue(window, out _))
        {
            _hookedWindows.Add(window, new object());
            window.ModalPushing += OnModalPushing;
            window.ModalPushed += OnModalPushed;
            window.PropertyChanged += OnWindowPropertyChanged;
        }

        var direction = CurrentFlowDirection;
        window.FlowDirection = direction;
        if (window.Page is { } page)
        {
            page.FlowDirection = direction;
        }

        foreach (var modal in window.Navigation.ModalStack)
        {
            modal.FlowDirection = direction;
        }
    }

    private FlowDirection CurrentFlowDirection =>
        _isRightToLeft ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;

    private LocalizedString GetOrCreateText(string key)
    {
        lock (_gate)
        {
            if (!_texts.TryGetValue(key, out var text))
            {
                text = new LocalizedString(key);
                text.Refresh(Resolve(key, _languageCode, static (c, k, l) => c.Get(k, l)));
                _texts[key] = text;
            }

            return text;
        }
    }

    private string Resolve(string key, string languageCode, Func<StringCatalog, string, string, string> read)
    {
        if (!_catalog.Contains(key))
        {
            LogUnknown(key);
        }

        return read(_catalog, key, languageCode);
    }

    private void LogUnknown(string key)
    {
        lock (_gate)
        {
            if (!_loggedUnknown.Add(key))
            {
                return;
            }
        }

        var message = $"Unknown string key: {key}";
        System.Diagnostics.Debug.WriteLine($"{LogTag}: {message}");
#if ANDROID
        Android.Util.Log.Warn(LogTag, message);
#endif
    }

    private void RefreshTexts()
    {
        LocalizedString[] texts;
        lock (_gate)
        {
            texts = [.. _texts.Values];
        }

        var code = _languageCode;
        foreach (var text in texts)
        {
            text.Refresh(Resolve(text.Key, code, static (c, k, l) => c.Get(k, l)));
        }
    }

    private void OnSettingsChanged(object? sender, UiSettings settings)
    {
        if ((int)settings.Language == _language && _initialized)
        {
            return;
        }

        _ = ApplySafeAsync(settings.Language);
    }

    private async Task ApplySafeAsync(AppLanguage choice)
    {
        try
        {
            await ApplyAsync(choice, force: false).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"{LogTag}: applying the language failed: {ex}");
#if ANDROID
            Android.Util.Log.Warn(LogTag, $"Applying the language failed: {ex.Message}");
#endif
        }
    }

    private Task ApplyAsync(AppLanguage choice, bool force)
    {
        if (MainThread.IsMainThread)
        {
            ApplyOnMainThread(choice, force);
            return Task.CompletedTask;
        }

        return MainThread.InvokeOnMainThreadAsync(() => ApplyOnMainThread(choice, force));
    }

    private void ApplyOnMainThread(AppLanguage choice, bool force)
    {
        var code = StringCatalog.ResolveLanguageCode(choice, _deviceLanguage);
        var changed = force || !string.Equals(code, _languageCode, StringComparison.Ordinal);

        _language = (int)choice;
        _languageCode = code;
        _isRightToLeft = StringCatalog.IsRightToLeft(code);

        if (!changed)
        {
            return;
        }

        RefreshTexts();

        if (Application.Current is { } app)
        {
            foreach (var window in app.Windows.ToArray())
            {
                ApplyFlowDirection(window);
            }
        }

        Changed?.Invoke(this, EventArgs.Empty);
    }

    private void OnModalPushing(object? sender, ModalPushingEventArgs e) =>
        e.Modal.FlowDirection = CurrentFlowDirection;

    private void OnModalPushed(object? sender, ModalPushedEventArgs e) =>
        e.Modal.FlowDirection = CurrentFlowDirection;

    private void OnWindowPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Window.Page) && sender is Window { Page: { } page })
        {
            page.FlowDirection = CurrentFlowDirection;
        }
    }
}
