namespace Player.Core.Models;

/// <param name="FontScale">1.0 is the engine default size.</param>
/// <param name="BackgroundOpacity">0 .. 1.</param>
/// <param name="BottomMarginFraction">Fraction of the video height, 0 .. 0.3.</param>
public sealed record SubtitleStyle(
    double FontScale,
    uint TextColorArgb,
    uint OutlineColorArgb,
    int OutlineThickness,
    double BackgroundOpacity,
    bool Bold,
    double BottomMarginFraction)
{
    public static SubtitleStyle Default { get; } = new(1.0, 0xFFFFFFFF, 0xFF000000, 2, 0.0, false, 0.05);
}

/// <summary>
/// Per-file state. A null value means "not set for this file": the global default applies.
/// </summary>
public sealed record PlaybackState(MediaFileId Id)
{
    public long PositionMs { get; init; }
    public long DurationMs { get; init; }

    /// <summary>True when the position passed 95% of the duration.</summary>
    public bool Finished { get; init; }

    /// <summary>Unix seconds.</summary>
    public long LastPlayedUtc { get; init; }

    public float? Speed { get; init; }
    public DecoderMode? Decoder { get; init; }
    public AspectMode? Aspect { get; init; }
    public int? AudioTrackId { get; init; }
    public long? AudioDelayMs { get; init; }

    /// <summary>-1 means subtitles are off for this file.</summary>
    public int? SubtitleTrackId { get; init; }

    public long? SubtitleDelayMs { get; init; }
    public string? ExternalSubtitleUri { get; init; }
}

/// <summary>Global playback defaults. Stored separately from <see cref="PlaybackState"/>.</summary>
public sealed record GlobalDefaults
{
    /// <summary>0.25 .. 4.0.</summary>
    public float Speed { get; init; } = 1.0f;

    public DecoderMode Decoder { get; init; } = DecoderMode.Hardware;
    public AspectMode Aspect { get; init; } = AspectMode.Fit;
    public RepeatMode Repeat { get; init; } = RepeatMode.None;
    public bool ResumeFromLastPosition { get; init; } = true;
    public bool SubtitlesEnabled { get; init; } = true;
    public string? PreferredAudioLanguage { get; init; }
    public string? PreferredSubtitleLanguage { get; init; }
    public long AudioDelayMs { get; init; }
    public long SubtitleDelayMs { get; init; }
    public SubtitleStyle SubtitleStyle { get; init; } = SubtitleStyle.Default;

    /// <summary>Keep playing audio-only when the app leaves the foreground. On unless the user turns it off.</summary>
    public bool BackgroundAudio { get; init; } = true;

    /// <summary>A playing video continues in a small window when the player is left. On unless turned off.</summary>
    public bool PictureInPictureOnLeave { get; init; } = true;
}

/// <summary>The result of resolving per-file state against the global defaults.</summary>
public sealed record EffectivePlaybackSettings(
    long StartPositionMs,
    float Speed,
    DecoderMode Decoder,
    AspectMode Aspect,
    int? AudioTrackId,
    long AudioDelayMs,
    int? SubtitleTrackId,
    long SubtitleDelayMs,
    string? ExternalSubtitleUri,
    SubtitleStyle SubtitleStyle);

/// <param name="ActualDecoder">What the engine really uses. Can differ from the request when hardware decoding is unavailable.</param>
/// <param name="VideoEnabled">False in audio-only mode.</param>
/// <param name="VolumePercent">0 .. 200.</param>
public sealed record PlayerSnapshot(
    PlayerStatus Status,
    MediaFileId? Current,
    long PositionMs,
    long DurationMs,
    float Speed,
    DecoderMode RequestedDecoder,
    DecoderMode? ActualDecoder,
    bool HasVideo,
    bool VideoEnabled,
    int VolumePercent,
    string? Error)
{
    public static PlayerSnapshot Idle { get; } =
        new(PlayerStatus.Idle, null, 0, 0, 1.0f, DecoderMode.Hardware, null, false, true, 100, null);
}

public sealed record AbRepeat(long StartMs, long EndMs);
