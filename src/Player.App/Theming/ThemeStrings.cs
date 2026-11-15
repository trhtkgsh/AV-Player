using Player.Core.Contracts;
using Player.Core.Models;

namespace Player.App.Theming;

/// <summary>The texts of the theme page. One line per text, sorted by key.</summary>
internal sealed class ThemeStrings : IStringSource
{
    public IReadOnlyDictionary<string, LocalizedText> Texts { get; } = new Dictionary<string, LocalizedText>
    {
        ["Theme.Accent"] = new("Accent color", "צבע הדגשה"),
        ["Theme.DarkOled"] = new("Dark OLED", "כהה OLED"),
        ["Theme.DarkSlate"] = new("Dark Slate", "כהה אפור"),
        ["Theme.HighContrast"] = new("High Contrast", "ניגודיות גבוהה"),
        ["Theme.HudOpacity"] = new("Player controls opacity", "שקיפות פקדי הנגן"),
        ["Theme.Light"] = new("Light", "בהירה"),
        ["Theme.Preview"] = new("Preview", "תצוגה מקדימה"),
        ["Theme.PreviewSample"] = new("Sample text", "טקסט לדוגמה"),
        ["Theme.Themes"] = new("Themes", "ערכות נושא"),
        ["Theme.Title"] = new("Theme", "ערכת נושא"),
    };
}
