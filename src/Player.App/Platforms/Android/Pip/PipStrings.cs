using Player.Core.Contracts;
using Player.Core.Models;

namespace Player.App.Pip;

/// <summary>The texts of the Picture-in-Picture actions. Keys are "Pip.&lt;Name&gt;", sorted by key, one line per text.</summary>
internal sealed class PipStrings : IStringSource
{
    /// <inheritdoc />
    public IReadOnlyDictionary<string, LocalizedText> Texts { get; } = new Dictionary<string, LocalizedText>
    {
        ["Pip.Next"] = new("Next", "הבא"),
        ["Pip.Pause"] = new("Pause", "השהה"),
        ["Pip.Play"] = new("Play", "נגן"),
        ["Pip.Previous"] = new("Previous", "הקודם"),
    };
}
