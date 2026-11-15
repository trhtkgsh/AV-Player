# Agent: localization

**Mission.** Provide the localization infrastructure: the text catalog, the localizer, the XAML markup extension and the layout direction.

**Model and why.** Sonnet 5.5. Binding-based live language change that survives trimming, culture rules and right-to-left window handling need framework knowledge. The texts themselves are not this agent's work.

**Phase.** 3 (added on 2026-11-15, owner request for a Hebrew interface).

**Owned paths.** `src/Player.App/Localization/**`, `src/Player.Core/Logic/Localization/**`.

**Read-only contracts.** Implements `ILocalizer`. Uses `IStringSource`, `LocalizedText`, `AppLanguage`, `IUiSettingsStore`, `UiSettings`.

**Inputs.** docs/specs/10-localization.md, docs/CONTRACTS.md.

**Deliverables.** `StringCatalog` (pure, in Core/Logic/Localization), `Localizer` (`ILocalizer`), `TExtension` (XAML markup extension `{loc:T Key}`), `CommonStrings` (`IStringSource` with the texts shared by all modules: OK, Cancel, Reset, Back, On, Off...), `AddLocalization()` DI extension.

**Steps.**
1. `StringCatalog`: built from a list of `IStringSource`. Lookup by key and language code, `Format`, `Plural` (`key.One`, `key.Many`), the System rule (a device language code of `he` or `iw` gives Hebrew, anything else English), duplicate keys reported as a list. No MAUI types.
2. `Localizer`: singleton. Takes `IEnumerable<IStringSource>` and `IUiSettingsStore`. `InitializeAsync` reads the setting and applies it; it subscribes to `IUiSettingsStore.Changed`. Applying sets `LanguageCode`, `IsRightToLeft`, the flow direction of every open window (and of windows created later), and raises `Changed` on the UI thread. It does not change `CultureInfo` of the process and does not touch the Android locale.
3. `TExtension`: returns a one-way binding to an observable text object of the key, so the text changes when the language changes. Use a binding form that works in a trimmed Release build (a typed or compiled binding, no reflection on a string path). Markup extensions cannot use DI: the extension reads a static accessor that `Localizer` sets in its constructor.
4. An unknown key returns `[key]` and is logged once. In a Debug build duplicate keys throw at startup.
5. `CommonStrings`: only texts that at least two modules use.
6. `AddLocalization()` registers the localizer as `ILocalizer` and `CommonStrings` as `IStringSource`.

**Acceptance.**
- Build green in Debug and Release. `StringCatalog` is pure and covered by test-writer.
- A language change updates an open page without navigation, in a Release build.
- No literal user text in the owned paths except inside `CommonStrings`.

**Out of scope.** The texts of other modules. The language picker (ui-settings). Translating track names. More languages.

**Escalation.** Stop and report when a contract is missing or wrong, a file outside the owned paths is needed, an acceptance item cannot be met, or the same step failed twice. Never change a contract.

**Report format.** `STATUS: done | blocked`, `BRANCH`, `FILES`, `BUILD`, `TESTS`, `DEVICE CHECKLIST` (items for the owner), `DEVIATIONS`, `QUESTIONS`.
