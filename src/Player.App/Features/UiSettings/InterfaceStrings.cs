using Player.Core.Contracts;
using Player.Core.Models;

namespace Player.App.Features.InterfaceSettings;

/// <summary>The texts of the interface settings pages and the settings home. Keys: "Interface.*" and "SettingsHome.*".</summary>
internal sealed class InterfaceStrings : IStringSource
{
    /// <inheritdoc />
    public IReadOnlyDictionary<string, LocalizedText> Texts { get; } = new Dictionary<string, LocalizedText>
    {
        ["Interface.Date"] = new("Date", "תאריך"),
        ["Interface.Days.Many"] = new("{0} days", "{0} ימים"),
        ["Interface.Days.One"] = new("1 day", "יום אחד"),
        ["Interface.DetailsShown"] = new("Details shown for each file", "פרטים להצגה בכל קובץ"),
        ["Interface.Duration"] = new("Duration", "משך"),
        ["Interface.FileList"] = new("File list", "רשימת הקבצים"),
        ["Interface.FolderPath"] = new("Folder path", "נתיב התיקייה"),
        ["Interface.HideControlsAfter"] = new("Hide the controls after", "הסתרת הפקדים אחרי"),
        ["Interface.Language"] = new("Language", "שפה"),
        ["Interface.LongPressSpeed"] = new("Speed while holding the picture", "מהירות בלחיצה ארוכה על התמונה"),
        ["Interface.MarkNewFor"] = new("Mark files as new for", "סמן קבצים כחדשים למשך"),
        ["Interface.NameSize"] = new("Size of names", "גודל גופן השמות"),
        ["Interface.Player"] = new("Player", "נגן"),
        ["Interface.Resolution"] = new("Resolution", "רזולוציה"),
        ["Interface.SaveFailed"] = new("The setting could not be saved.", "לא ניתן לשמור את ההגדרה."),
        ["Interface.Seconds"] = new("{0} seconds", "{0} שניות"),
        ["Interface.SecondsShort"] = new("{0} s", "{0} שנ׳"),
        ["Interface.ShowAudioOnlyFolders"] = new("Show folders with audio only", "הצג תיקיות עם שמע בלבד"),
        ["Interface.ShowBattery"] = new("Show the battery", "הצג סוללה"),
        ["Interface.ShowClock"] = new("Show the clock", "הצג שעון"),
        ["Interface.Size"] = new("Size", "גודל"),
        ["Interface.SkipLength"] = new("Skip length", "משך הדילוג"),
        ["Interface.Title"] = new("Interface", "ממשק"),
        ["SettingsHome.Interface"] = new("Interface", "ממשק"),
        ["SettingsHome.InterfaceHint"] = new("Player controls and file list", "פקדי הנגן ורשימת הקבצים"),
        ["SettingsHome.PlaybackDefaults"] = new("Playback defaults", "ברירות מחדל לניגון"),
        ["SettingsHome.PlaybackDefaultsHint"] = new("Speed, decoder, repeat, audio and subtitles for new files", "מהירות, מפענח, חזרה, שמע וכתוביות לקבצים חדשים"),
        ["SettingsHome.Theme"] = new("Theme", "ערכת נושא"),
        ["SettingsHome.ThemeHint"] = new("Colors, accent and player controls opacity", "צבעים, צבע הדגשה ושקיפות פקדי הנגן"),
    };
}
