# Spec 10: Localization (English and Hebrew)

Phase: 3. Added on 2026-11-15 at the owner's request ("add a Hebrew interface as an option, an addition and not a replacement").
Agent: localization (infrastructure). Every UI agent owns the texts of its own module.
Contracts: `ILocalizer`, `IStringSource`, `LocalizedText`, `AppLanguage`, `UiSettings.Language`.

## Requirements

1. **Languages.** English and Hebrew. English stays complete: Hebrew is added next to it.
2. **Choice.** `UiSettings.Language`: System (default), English, Hebrew. System means Hebrew when the device language is Hebrew, otherwise English. The choice is on the Interface settings page and is stored with the other UI settings.
3. **Live change.** A new choice applies at once, without an app restart: texts bound in XAML change by themselves, and the layout direction changes with them.
4. **Direction.** Hebrew lays the app out right to left, English left to right, whatever the device language is. The direction is set on the window, so every page and every modal sheet follows it.
5. **Never mirrored.** The transport bar of the player (seek bar, times, previous, play/pause, next, speed, decoder) stays left to right in every language. Symbols are not mirrored, except the back arrow.
6. **Texts live with their module.** Each module has one `IStringSource` class inside its own owned path, with one line per text: key, English, Hebrew. Keys are `<Module>.<Name>`, unique in the app. No text of the user interface is a literal in XAML or in a ViewModel.
7. **XAML.** `Text="{loc:T Explorer.Sort}"` (markup extension of the localization module). ViewModels take `ILocalizer` and read `localizer["Key"]`, `Format` or `Plural`. A ViewModel whose page can be open while the language changes (the settings pages and the pages under them in the navigation stack) refreshes its computed texts on `ILocalizer.Changed`.
8. **Not translated.** File names, folder names, track names that come from the file or from LibVLC, technical units (`MB`, `ms`, `x`), the decoder labels `HW` and `SW`, the app name.
9. **Unknown key.** Shows the key in square brackets and is logged. A duplicate key in two sources fails at startup in a Debug build.
10. **No platform locale switch.** The app does not change the Android per-app locale. System dialogs (the permission dialog, the file picker) follow the device.
11. **Hebrew style.** Short, plain, modern Hebrew without niqqud, in the masculine imperative or the infinitive for commands ("שמור כברירת מחדל", "אפס"). Technical terms that Hebrew users know in English stay in English only where a Hebrew term would confuse.

## Acceptance

- Unit tests (pure part in `Player.Core.Logic.Localization`): lookup, fallback for an unknown key, `Format`, `Plural`, the System rule, duplicate detection.
- A test that reads the source files: every key used in XAML (`{loc:T Key}`) exists in a string source, every text has a non-empty English and Hebrew value, the Hebrew value contains Hebrew letters (or is on a short allow list), and both have the same placeholders.
- Device: with the device in Hebrew and the choice on System the app is in Hebrew and right to left. Switching to English on the Interface page changes the open page at once. The player transport bar is identical in both languages.

## Out of scope

More languages, per-app locale of Android, translated track names, right-to-left changes inside the video picture.
