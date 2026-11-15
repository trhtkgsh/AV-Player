using Player.Core.Contracts;
using Player.Core.Models;

namespace Player.App.Features.Explorer;

/// <summary>The texts of the explorer pages. Common texts are in <c>CommonStrings</c>.</summary>
internal sealed class ExplorerStrings : IStringSource
{
    /// <inheritdoc />
    public IReadOnlyDictionary<string, LocalizedText> Texts { get; } = new Dictionary<string, LocalizedText>
    {
        ["Explorer.AddFavorite"] = new("Add to favorites", "הוסף למועדפים"),
        ["Explorer.AllMedia"] = new("All media", "כל המדיה"),
        ["Explorer.FilesHere"] = new("Files in this folder ({0})", "קבצים בתיקייה זו ({0})"),
        ["Explorer.FolderOptions"] = new("Folder options", "אפשרויות תיקייה"),
        ["Explorer.FolderSpeed"] = new("Default speed for this folder: {0}", "מהירות ברירת מחדל לתיקייה: {0}"),
        ["Explorer.FolderSpeedGeneral"] = new("general default", "כללית"),
        ["Explorer.FolderSpeedUseGeneral"] = new("Use the general default", "השתמש בברירת המחדל הכללית"),
        ["Explorer.Items.Many"] = new("{0} items", "{0} פריטים"),
        ["Explorer.Items.One"] = new("1 item", "פריט אחד"),
        ["Explorer.LayoutGrid"] = new("Grid", "רשת"),
        ["Explorer.LayoutList"] = new("List", "רשימה"),
        ["Explorer.NewCount.Many"] = new("{0} new", "{0} חדשים"),
        ["Explorer.NewCount.One"] = new("1 new", "חדש אחד"),
        ["Explorer.NewTag"] = new("NEW", "חדש"),
        ["Explorer.NoFavorites"] = new("No favorite folders yet. Use the menu of a folder to add it.", "אין עדיין תיקיות מועדפות. אפשר להוסיף תיקייה דרך התפריט שלה."),
        ["Explorer.NoFiles"] = new("No media files", "לא נמצאו קבצי מדיה"),
        ["Explorer.NoFolders"] = new("No media folders found", "לא נמצאו תיקיות מדיה"),
        ["Explorer.OpenAppSettings"] = new("Open app settings", "פתח את הגדרות האפליקציה"),
        ["Explorer.PermissionNeeded"] = new("Ariel Player needs permission to read your video and audio files.", "Ariel Player צריך הרשאה לקרוא את קבצי הווידאו והשמע שלך."),
        ["Explorer.RemoveFavorite"] = new("Remove from favorites", "הסר מהמועדפים"),
        ["Explorer.SaveFailed"] = new("The change could not be saved.", "לא ניתן לשמור את השינוי."),
        ["Explorer.Search"] = new("Search", "חיפוש"),
        ["Explorer.SettingsFailed"] = new("Settings could not be opened.", "לא ניתן לפתוח את ההגדרות."),
        ["Explorer.Sort"] = new("Sort", "מיון"),
        ["Explorer.SortBy"] = new("Sort by", "מיון לפי"),
        ["Explorer.SortDateAdded"] = new("Date added", "תאריך הוספה"),
        ["Explorer.SortDuration"] = new("Duration", "משך"),
        ["Explorer.SortName"] = new("Name", "שם"),
        ["Explorer.SortSize"] = new("Size", "גודל"),
        ["Explorer.View"] = new("View", "תצוגה"),
        ["Explorer.ViewFavorites"] = new("Favorites", "מועדפים"),
        ["Explorer.ViewFlat"] = new("All files", "כל הקבצים"),
        ["Explorer.ViewFolders"] = new("Media folders", "תיקיות מדיה"),
        ["Explorer.ViewTree"] = new("File browser", "סייר קבצים"),
        ["Explorer.Watched"] = new("Watched", "נצפה"),
    };
}
