using Player.Core.Models;

namespace Player.App.Features.PlaybackSettings;

/// <summary>
/// Point A of an A-B repeat that is being set up. A singleton, so that A survives closing the sheet.
/// Not persisted. Only a complete range (A and B) is handed to the coordinator.
/// </summary>
public sealed class AbRepeatDraft
{
    /// <summary>Position of A in milliseconds, or null while A is not set.</summary>
    public long? StartMs { get; set; }

    /// <summary>The file that A was set on.</summary>
    public MediaFileId? FileId { get; set; }

    public void Clear()
    {
        StartMs = null;
        FileId = null;
    }
}
