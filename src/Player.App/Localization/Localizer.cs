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
    private const int NoChoice = -1;

    // Text objects created by XAML before the localizer exists; the first localizer adopts them.
    private static readonly Dictionary<string, LocalizedString> Pending = new(StringComparer.Ordinal);
    private static readonly object StaticGate = new();

    private readonly StringCatalog _catalog;
    private readonly IUiSettingsStore _settings;
    private readonly string _deviceLanguage;
    private readonly Dictionary<string, LocalizedString> _texts = new(StringComparer.Ordinal);
    private readonly HashSet<string> _logged = new(StringComparer.Ordinal);
    private readonly ConditionalWeakTable<Window, object> _hookedWindows = new();
    private readonly object _gate = new();

    // The latest AppLanguage asked for, or NoChoice. Every apply reads it, so the last request always wins.
    private int _requested = NoChoice;
    private volatile LanguageState _state;
    private volatile bool _initialized;
    private Application? _hookedApp;

    /// <summary>
    /// Merges the string sources and starts following the UI settings. Before <see cref="InitializeAsync"/> has run
    /// the System rule applies.
    /// </summary>
    public Localizer(IEnumerable<IStringSource> sources, IUiSettingsStore settings)
    {
        ArgumentNullException.ThrowIfNull(sources);
        ArgumentNullException.ThrowIfNull(settings);

        _catalog = new StringCatalog(sources);
        if (_catalog.DuplicateKeys.Count > 0)
        {
            var message = "Duplicate string keys: " + string.Join(", ", _catalog.DuplicateKeys);
#if DEBUG
            throw new InvalidOperationException(message);
#else
            LogOnce("duplicates", message);
#endif
        }

        _settings = settings;
        _deviceLanguage = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        _state = CreateState(AppLanguage.System);

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

        RefreshTexts(_state.Code);
        _settings.Changed += OnSettingsChanged;
    }

    /// <summary>The localizer of the running app, for markup extensions that cannot use dependency injection.</summary>
    public static Localizer? Current { get; private set; }

    /// <inheritdoc />
    public AppLanguage Language => _state.Language;

    /// <inheritdoc />
    public string LanguageCode => _state.Code;

    /// <inheritdoc />
    public bool IsRightToLeft => _state.IsRightToLeft;

    /// <summary>True once <see cref="InitializeAsync"/> has finished, also when the store could not be read.</summary>
    public bool IsInitialized => _initialized;

    /// <inheritdoc />
    public event EventHandler? Changed;

    /// <inheritdoc />
    public string this[string key] => Resolve(key, _state.Code, static (c, k, l) => c.Get(k, l));

    /// <inheritdoc />
    public string Format(string key, params object?[] args) =>
        Resolve(key, _state.Code, (c, k, l) => c.Format(k, l, args));

    /// <inheritdoc />
    public string Plural(string key, int count) =>
        Resolve(count == 1 ? key + ".One" : key + ".Many", _state.Code, (c, k, l) => c.Format(k, l, count));

    /// <inheritdoc />
    public async Task InitializeAsync(CancellationToken ct)
    {
        try
        {
            AppLanguage? stored = null;
            try
            {
                stored = (await _settings.GetAsync(ct).ConfigureAwait(false)).Language;
                ct.ThrowIfCancellationRequested();
            }
            catch (Exception ex) when (!ct.IsCancellationRequested)
            {
                LogOnce("init:" + ex.GetType().Name, $"Reading the language failed, the System rule stays: {ex.Message}");
            }

            // A change that arrived while the store was read is newer than the stored value.
            if (stored is { } language)
            {
                Interlocked.CompareExchange(ref _requested, (int)language, NoChoice);
            }

            await ApplyAsync(force: stored is not null).ConfigureAwait(false);
        }
        finally
        {
            _initialized = true;
        }
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
    /// Windows that show a page after the localizer was initialized are also found by themselves; calling this
    /// for every window the app creates (for example in <c>App.CreateWindow</c>) covers the time before that.
    /// Call it on the UI thread.
    /// </summary>
    public void ApplyFlowDirection(Window window)
    {
        ArgumentNullException.ThrowIfNull(window);

        EnsureApplicationHooked();

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
        _state.IsRightToLeft ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;

    private LanguageState CreateState(AppLanguage choice)
    {
        var code = StringCatalog.ResolveLanguageCode(choice, _deviceLanguage);
        return new LanguageState(choice, code, StringCatalog.IsRightToLeft(code));
    }

    private LocalizedString GetOrCreateText(string key)
    {
        lock (_gate)
        {
            if (!_texts.TryGetValue(key, out var text))
            {
                text = new LocalizedString(key);
                text.Refresh(Resolve(key, _state.Code, static (c, k, l) => c.Get(k, l)));
                _texts[key] = text;
            }

            return text;
        }
    }

    private string Resolve(string key, string languageCode, Func<StringCatalog, string, string, string> read)
    {
        if (!_catalog.Contains(key))
        {
            LogOnce("unknown:" + key, $"Unknown string key: {key}");
        }

        return read(_catalog, key, languageCode);
    }

    private void LogOnce(string id, string message)
    {
        lock (_gate)
        {
            if (!_logged.Add(id))
            {
                return;
            }
        }

        System.Diagnostics.Debug.WriteLine($"{LogTag}: {message}");
#if ANDROID
        Android.Util.Log.Warn(LogTag, message);
#endif
    }

    private void RefreshTexts(string code)
    {
        LocalizedString[] texts;
        lock (_gate)
        {
            texts = [.. _texts.Values];
        }

        foreach (var text in texts)
        {
            text.Refresh(Resolve(text.Key, code, static (c, k, l) => c.Get(k, l)));
        }
    }

    private void OnSettingsChanged(object? sender, UiSettings settings)
    {
        Interlocked.Exchange(ref _requested, (int)settings.Language);
        _ = ApplySafeAsync();
    }

    private async Task ApplySafeAsync()
    {
        try
        {
            await ApplyAsync(force: false).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            LogOnce("apply:" + ex.GetType().Name, $"Applying the language failed: {ex.Message}");
        }
    }

    private Task ApplyAsync(bool force)
    {
        if (MainThread.IsMainThread)
        {
            ApplyOnMainThread(force);
            return Task.CompletedTask;
        }

        return MainThread.InvokeOnMainThreadAsync(() => ApplyOnMainThread(force));
    }

    // Reads the latest requested choice, never an argument, so dispatches that run late or out of order end right.
    private void ApplyOnMainThread(bool force)
    {
        EnsureApplicationHooked();

        var requested = Volatile.Read(ref _requested);
        var choice = requested == NoChoice ? AppLanguage.System : (AppLanguage)requested;
        var next = CreateState(choice);
        var codeChanged = !string.Equals(next.Code, _state.Code, StringComparison.Ordinal);

        _state = next;
        if (!codeChanged && !force)
        {
            return;
        }

        RefreshTexts(next.Code);

        if (Application.Current is { } app)
        {
            foreach (var window in app.Windows.ToArray())
            {
                ApplyFlowDirection(window);
            }
        }

        RaiseChanged();
    }

    // One failing subscriber must not stop the others.
    private void RaiseChanged()
    {
        if (Changed is not { } handlers)
        {
            return;
        }

        foreach (var handler in handlers.GetInvocationList())
        {
            try
            {
                ((EventHandler)handler)(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                LogOnce("changed:" + handler.Method.DeclaringType?.FullName + "." + handler.Method.Name,
                    $"A Changed subscriber failed: {ex}");
            }
        }
    }

    // Application.PageAppearing is the only app-wide signal from inside these paths that a window shows a page.
    private void EnsureApplicationHooked()
    {
        var app = Application.Current;
        if (app is null || ReferenceEquals(app, _hookedApp))
        {
            return;
        }

        _hookedApp = app;
        app.PageAppearing += OnPageAppearing;
    }

    private void OnPageAppearing(object? sender, Page page)
    {
        if (page.Window is { } window && !_hookedWindows.TryGetValue(window, out _))
        {
            ApplyFlowDirection(window);
        }
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

    private sealed record LanguageState(AppLanguage Language, string Code, bool IsRightToLeft);
}
