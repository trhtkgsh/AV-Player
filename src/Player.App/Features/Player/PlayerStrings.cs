using Player.Core.Contracts;
using Player.Core.Models;

namespace Player.App.Features.PlayerHud;

/// <summary>The texts of the player page. Keys are "Player.&lt;Name&gt;", sorted by key, one line per text.</summary>
internal sealed class PlayerStrings : IStringSource
{
    /// <inheritdoc />
    public IReadOnlyDictionary<string, LocalizedText> Texts { get; } = new Dictionary<string, LocalizedText>
    {
        ["Player.ActionFailed"] = new("The action could not be completed.", "לא ניתן היה להשלים את הפעולה."),
        ["Player.Aspect"] = new("Aspect", "יחס תצוגה"),
        ["Player.Aspect.Crop"] = new("Crop", "חיתוך"),
        ["Player.Aspect.Fit"] = new("Fit", "התאמה למסך"),
        ["Player.Aspect.Original"] = new("100%", "100%"),
        ["Player.Aspect.Stretch"] = new("Stretch", "מתיחה"),
        ["Player.AudioTrack"] = new("Audio track", "רצועת שמע"),
        ["Player.CannotPlay"] = new("Cannot play", "לא ניתן לנגן"),
        ["Player.CannotPlayMessage"] = new("This file cannot be played.", "לא ניתן לנגן את הקובץ הזה."),
        ["Player.Lock"] = new("Lock the screen", "נעל את המסך"),
        ["Player.Locked"] = new("Locked", "המסך נעול"),
        ["Player.Menu"] = new("More", "עוד"),
        ["Player.MenuDecoder"] = new("Decoder: {0}", "מפענח: {0}"),
        ["Player.Next"] = new("Next", "הבא"),
        ["Player.Osd.Brightness"] = new("Brightness {0}%", "בהירות {0}%"),
        ["Player.Osd.Seconds"] = new("{0} s", "{0} שנ'"),
        ["Player.Osd.Speed"] = new("Speed {0}x", "מהירות {0}x"),
        ["Player.Osd.Volume"] = new("Volume {0}%", "עוצמת קול {0}%"),
        ["Player.Osd.Zoom"] = new("Zoom {0}%", "זום {0}%"),
        ["Player.Pause"] = new("Pause", "השהה"),
        ["Player.Play"] = new("Play", "נגן"),
        ["Player.PlaybackSpeed"] = new("Playback speed", "מהירות ניגון"),
        ["Player.Previous"] = new("Previous", "הקודם"),
        ["Player.Repeat"] = new("Repeat", "ניגון חוזר"),
        ["Player.Subtitles"] = new("Subtitles", "כתוביות"),
        ["Player.Unlock"] = new("Unlock the screen", "בטל נעילה"),
    };
}
