using Player.Core.Contracts;
using Player.Core.Models;

namespace Player.App.Features.Explorer;

/// <summary>The texts of the explorer pages. Common texts are in <c>CommonStrings</c>.</summary>
internal sealed class ExplorerStrings : IStringSource
{
    /// <inheritdoc />
    public IReadOnlyDictionary<string, LocalizedText> Texts { get; } = new Dictionary<string, LocalizedText>
    {
        ["Explorer.AllFiles"] = new("All files", "כל הקבצים"),
        ["Explorer.AllMedia"] = new("All media", "כל המדיה"),
        ["Explorer.Items.Many"] = new("{0} items", "{0} פריטים"),
        ["Explorer.Items.One"] = new("1 item", "פריט אחד"),
        ["Explorer.NewCount.Many"] = new("{0} new", "{0} חדשים"),
        ["Explorer.NewCount.One"] = new("1 new", "חדש אחד"),
        ["Explorer.NewTag"] = new("NEW", "חדש"),
        ["Explorer.NoFiles"] = new("No media files", "לא נמצאו קבצי מדיה"),
        ["Explorer.NoFolders"] = new("No media folders found", "לא נמצאו תיקיות מדיה"),
        ["Explorer.OpenAppSettings"] = new("Open app settings", "פתח את הגדרות האפליקציה"),
        ["Explorer.PermissionNeeded"] = new("Ariel Player needs permission to read your video and audio files.", "Ariel Player צריך הרשאה לקרוא את קבצי הווידאו והשמע שלך."),
        ["Explorer.Search"] = new("Search", "חיפוש"),
        ["Explorer.SettingsFailed"] = new("Settings could not be opened.", "לא ניתן לפתוח את ההגדרות."),
        ["Explorer.Sort"] = new("Sort", "מיון"),
        ["Explorer.SortBy"] = new("Sort by", "מיון לפי"),
        ["Explorer.SortDateAdded"] = new("Date added", "תאריך הוספה"),
        ["Explorer.SortDuration"] = new("Duration", "משך"),
        ["Explorer.SortName"] = new("Name", "שם"),
        ["Explorer.SortSize"] = new("Size", "גודל"),
        ["Explorer.Watched"] = new("Watched", "נצפה"),
    };
}
