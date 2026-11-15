using Player.Core.Contracts;
using Player.Core.Models;

namespace Player.App.Features.PlaybackSettings;

/// <summary>Texts of the in-player settings sheets. Common texts (Common.*) are in the common source.</summary>
internal sealed class SheetStrings : IStringSource
{
    /// <inheritdoc />
    public IReadOnlyDictionary<string, LocalizedText> Texts { get; } = new Dictionary<string, LocalizedText>
    {
        ["Sheets.Audio.Delay"] = new("Audio delay", "השהיית שמע"),
        ["Sheets.Audio.NoTracks"] = new("No audio tracks", "אין רצועות שמע"),
        ["Sheets.Audio.Title"] = new("Audio track", "רצועת שמע"),
        ["Sheets.Repeat.AbRange"] = new("A {0}  -  B {1}", "מ-A {0} עד B {1}"),
        ["Sheets.Repeat.AbStartOnly"] = new("A {0}  -  B not set", "מ-A {0} עד B לא נקבע"),
        ["Sheets.Repeat.AbTitle"] = new("A-B repeat", "חזרה על קטע A-B"),
        ["Sheets.Repeat.BTooShort"] = new("B must be at least 1 second after A.", "B חייב להיות לפחות שנייה אחת אחרי A."),
        ["Sheets.Repeat.Clear"] = new("Clear", "נקה"),
        ["Sheets.Repeat.Folder"] = new("Folder", "תיקייה"),
        ["Sheets.Repeat.NotSet"] = new("Not set", "לא נקבע"),
        ["Sheets.Repeat.RangeInvalid"] = new("This A-B range is not valid for this file.", "טווח A-B הזה אינו תקף לקובץ הזה."),
        ["Sheets.Repeat.SetA"] = new("Set A", "קבע A"),
        ["Sheets.Repeat.SetB"] = new("Set B", "קבע B"),
        ["Sheets.Repeat.ThisFile"] = new("This file", "הקובץ הזה"),
        ["Sheets.Repeat.Title"] = new("Repeat", "חזרה"),
        ["Sheets.Speed.Title"] = new("Speed", "מהירות"),
        ["Sheets.Style.Background"] = new("Background", "רקע"),
        ["Sheets.Style.Bold"] = new("Bold", "מודגש"),
        ["Sheets.Style.ColorBlack"] = new("Black", "שחור"),
        ["Sheets.Style.ColorCyan"] = new("Cyan", "תכלת"),
        ["Sheets.Style.ColorDarkGray"] = new("Dark gray", "אפור כהה"),
        ["Sheets.Style.ColorGreen"] = new("Green", "ירוק"),
        ["Sheets.Style.ColorLightGray"] = new("Light gray", "אפור בהיר"),
        ["Sheets.Style.ColorNavy"] = new("Navy", "כחול כהה"),
        ["Sheets.Style.ColorPink"] = new("Pink", "ורוד"),
        ["Sheets.Style.ColorWhite"] = new("White", "לבן"),
        ["Sheets.Style.ColorYellow"] = new("Yellow", "צהוב"),
        ["Sheets.Style.Darker"] = new("Darker", "כהה יותר"),
        ["Sheets.Style.Higher"] = new("Higher", "גבוה יותר"),
        ["Sheets.Style.Larger"] = new("Larger", "גדול יותר"),
        ["Sheets.Style.Lighter"] = new("Lighter", "בהיר יותר"),
        ["Sheets.Style.Lower"] = new("Lower", "נמוך יותר"),
        ["Sheets.Style.Margin"] = new("Distance from the bottom", "מרחק מהתחתית"),
        ["Sheets.Style.Note"] = new("For text subtitles. The picture restarts briefly after a change.", "לכתוביות טקסט בלבד. התמונה נטענת מחדש לרגע לאחר כל שינוי."),
        ["Sheets.Style.Outline"] = new("Outline", "קו מתאר"),
        ["Sheets.Style.OutlineColor"] = new("Outline color", "צבע קו המתאר"),
        ["Sheets.Style.RestoreDefault"] = new("Back to default", "חזרה לברירת מחדל"),
        ["Sheets.Style.SaveDefault"] = new("Save as default", "שמור כברירת מחדל"),
        ["Sheets.Style.Saved"] = new("Saved as the default style.", "הסגנון נשמר כברירת מחדל."),
        ["Sheets.Style.Size"] = new("Size", "גודל"),
        ["Sheets.Style.Smaller"] = new("Smaller", "קטן יותר"),
        ["Sheets.Style.TextColor"] = new("Text color", "צבע הטקסט"),
        ["Sheets.Style.Thicker"] = new("Thicker", "עבה יותר"),
        ["Sheets.Style.Thinner"] = new("Thinner", "דק יותר"),
        ["Sheets.Style.Title"] = new("Subtitle style", "עיצוב כתוביות"),
        ["Sheets.Subtitles.Delay"] = new("Subtitle delay", "השהיית כתוביות"),
        ["Sheets.Subtitles.FilesNearby"] = new("Files next to the video", "קבצים ליד הסרטון"),
        ["Sheets.Subtitles.LoadFailed"] = new("The subtitle file could not be loaded.", "לא ניתן לטעון את קובץ הכתוביות."),
        ["Sheets.Subtitles.OpenFile"] = new("Open a subtitle file...", "פתח קובץ כתוביות..."),
        ["Sheets.Subtitles.Style"] = new("Style...", "עיצוב..."),
        ["Sheets.Subtitles.Title"] = new("Subtitles", "כתוביות"),
    };
}
