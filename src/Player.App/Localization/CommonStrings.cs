using Player.Core.Contracts;
using Player.Core.Models;

namespace Player.App.Localization;

/// <summary>Texts used by two or more modules.</summary>
public sealed class CommonStrings : IStringSource
{
    /// <inheritdoc />
    public IReadOnlyDictionary<string, LocalizedText> Texts { get; } = new Dictionary<string, LocalizedText>
    {
        ["Common.Back"] = new("Back", "חזרה"),
        ["Common.Cancel"] = new("Cancel", "ביטול"),
        ["Common.Close"] = new("Close", "סגור"),
        ["Common.Default"] = new("Default", "ברירת מחדל"),
        ["Common.Delete"] = new("Delete", "מחק"),
        ["Common.Error"] = new("Error", "שגיאה"),
        ["Common.Language.English"] = new("English", "English"),
        ["Common.Language.Hebrew"] = new("עברית", "עברית"),
        ["Common.Language.System"] = new("System default", "ברירת המחדל של המכשיר"),
        ["Common.No"] = new("No", "לא"),
        ["Common.Off"] = new("Off", "כבוי"),
        ["Common.Ok"] = new("OK", "אישור"),
        ["Common.On"] = new("On", "פעיל"),
        ["Common.Reset"] = new("Reset", "אפס"),
        ["Common.Save"] = new("Save", "שמור"),
        ["Common.Settings"] = new("Settings", "הגדרות"),
        ["Common.TryAgain"] = new("Try again", "נסה שוב"),
        ["Common.Yes"] = new("Yes", "כן"),
    };
}
