using Player.Core.Models;

namespace Player.Core.Logic.Resolution;

/// <summary>
/// The single place that decides the resolution order: the per-file value when it is set,
/// otherwise the global default. The speed alone has a middle step: the default of the file's folder.
/// </summary>
public static class PlaybackSettingsResolver
{
    public static EffectivePlaybackSettings Resolve(
        PlaybackState? state, GlobalDefaults defaults, FolderSettings? folder = null)
    {
        ArgumentNullException.ThrowIfNull(defaults);

        var resume = defaults.ResumeFromLastPosition && state is { Finished: false };

        return new EffectivePlaybackSettings(
            StartPositionMs: resume ? Math.Max(0, state!.PositionMs) : 0,
            Speed: state?.Speed ?? folder?.Speed ?? defaults.Speed,
            Decoder: state?.Decoder ?? defaults.Decoder,
            Aspect: state?.Aspect ?? defaults.Aspect,
            AudioTrackId: state?.AudioTrackId,
            AudioDelayMs: state?.AudioDelayMs ?? defaults.AudioDelayMs,
            SubtitleTrackId: state?.SubtitleTrackId ?? (defaults.SubtitlesEnabled ? null : -1),
            SubtitleDelayMs: state?.SubtitleDelayMs ?? defaults.SubtitleDelayMs,
            ExternalSubtitleUri: state?.ExternalSubtitleUri,
            SubtitleStyle: defaults.SubtitleStyle);
    }
}
