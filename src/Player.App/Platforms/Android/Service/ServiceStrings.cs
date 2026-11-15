using Player.Core.Contracts;
using Player.Core.Models;

namespace Player.App.Background;

/// <summary>The texts of the playback notification. Keys are "Service.&lt;Name&gt;", sorted by key, one line per text.</summary>
internal sealed class ServiceStrings : IStringSource
{
    /// <inheritdoc />
    public IReadOnlyDictionary<string, LocalizedText> Texts { get; } = new Dictionary<string, LocalizedText>
    {
        ["Service.Channel"] = new("Playback", "ניגון"),
        ["Service.Close"] = new("Close", "סגור"),
        ["Service.Next"] = new("Next", "הבא"),
        ["Service.Pause"] = new("Pause", "השהה"),
        ["Service.Play"] = new("Play", "נגן"),
        ["Service.Previous"] = new("Previous", "הקודם"),
    };
}
