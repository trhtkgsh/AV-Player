using Player.Core.Models;

namespace Player.Core.Contracts;

/// <summary>
/// The texts of the user interface in the language in effect. The language comes from
/// <see cref="UiSettings.Language"/>; the localizer follows <see cref="IUiSettingsStore.Changed"/> by itself.
/// </summary>
public interface ILocalizer
{
    /// <summary>The stored choice.</summary>
    AppLanguage Language { get; }

    /// <summary>The language in effect: "en" or "he".</summary>
    string LanguageCode { get; }

    bool IsRightToLeft { get; }

    /// <summary>Raised on the UI thread after the language in effect changed.</summary>
    event EventHandler? Changed;

    /// <summary>The text of a key. An unknown key returns the key in square brackets.</summary>
    string this[string key] { get; }

    /// <summary>The text of a key with its {0}, {1}... placeholders filled in.</summary>
    string Format(string key, params object?[] args);

    /// <summary>
    /// A counted text: the key with the suffix ".One" when the count is 1, otherwise the key with the suffix
    /// ".Many" formatted with the count as {0}.
    /// </summary>
    string Plural(string key, int count);

    /// <summary>Loads the stored language and applies it. Called once, before the first page is shown.</summary>
    Task InitializeAsync(CancellationToken ct);
}

/// <summary>
/// The texts of one module. Every module registers its own source in DI; the localizer merges them.
/// Keys are unique in the whole app and start with the module name, for example "Explorer.Sort".
/// </summary>
public interface IStringSource
{
    IReadOnlyDictionary<string, LocalizedText> Texts { get; }
}
