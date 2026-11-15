namespace Player.Data.GlobalDefaultsStorage;

/// <summary>What happens when the player is left while a video plays. The choice is stored in two fields.</summary>
public enum LeaveChoice
{
    /// <summary>A small window (Picture-in-Picture): <c>PictureInPictureOnLeave</c> true, <c>BackgroundAudio</c> true.</summary>
    Window,

    /// <summary>The sound keeps playing: <c>PictureInPictureOnLeave</c> false, <c>BackgroundAudio</c> true.</summary>
    Sound,

    /// <summary>Playback pauses: both fields false.</summary>
    Pause,
}

/// <summary>
/// Maps the two stored fields to the one choice of the defaults page and back. Pure, no state.
/// A stored pair with Picture-in-Picture on always reads as <see cref="LeaveChoice.Window"/>, whatever the sound field says.
/// </summary>
public static class LeaveChoiceMap
{
    /// <summary>Reads the choice from the stored fields.</summary>
    /// <param name="pictureInPictureOnLeave">The stored <c>PictureInPictureOnLeave</c>.</param>
    /// <param name="backgroundAudio">The stored <c>BackgroundAudio</c>.</param>
    public static LeaveChoice FromFields(bool pictureInPictureOnLeave, bool backgroundAudio)
    {
        if (pictureInPictureOnLeave)
        {
            return LeaveChoice.Window;
        }

        return backgroundAudio ? LeaveChoice.Sound : LeaveChoice.Pause;
    }

    /// <summary>Gives the two field values for a choice. Both are written in one save.</summary>
    /// <param name="choice">The chosen value.</param>
    /// <returns>The <c>PictureInPictureOnLeave</c> and <c>BackgroundAudio</c> values.</returns>
    public static (bool PictureInPictureOnLeave, bool BackgroundAudio) ToFields(LeaveChoice choice) => choice switch
    {
        LeaveChoice.Window => (true, true),
        LeaveChoice.Sound => (false, true),
        _ => (false, false),
    };
}
