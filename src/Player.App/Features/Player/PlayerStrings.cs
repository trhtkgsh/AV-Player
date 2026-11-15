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
        ["Player.Audio"] = new("Audio", "שמע"),
        ["Player.CannotPlay"] = new("Cannot play", "לא ניתן לנגן"),
        ["Player.CannotPlayMessage"] = new("This file cannot be played.", "לא ניתן לנגן את הקובץ הזה."),
        ["Player.Decoder"] = new("Decoder", "מפענח"),
        ["Player.Next"] = new("Next", "הבא"),
        ["Player.Pause"] = new("Pause", "השהה"),
        ["Player.Play"] = new("Play", "נגן"),
        ["Player.PlaybackSpeed"] = new("Playback speed", "מהירות ניגון"),
        ["Player.Previous"] = new("Previous", "הקודם"),
        ["Player.Repeat"] = new("Repeat", "חזרה"),
        ["Player.Subtitles"] = new("Subtitles", "כתוביות"),
    };
}
