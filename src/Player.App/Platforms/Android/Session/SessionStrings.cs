using Player.Core.Contracts;
using Player.Core.Models;

namespace Player.App.MediaControls;

/// <summary>The texts of the media session. Keys are "Session.&lt;Name&gt;", sorted by key, one line per text.</summary>
internal sealed class SessionStrings : IStringSource
{
    /// <inheritdoc />
    public IReadOnlyDictionary<string, LocalizedText> Texts { get; } = new Dictionary<string, LocalizedText>
    {
        ["Session.Close"] = new("Close", "סגור"),
    };
}
