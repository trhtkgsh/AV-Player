using Player.Core.Logic.Resolution;
using Player.Core.Models;

namespace Player.Core.Tests.Resolution;

public sealed class PlaybackSettingsResolverTests
{
    private static SubtitleStyle Style() => new(1.25, 0xFF112233, 0xFF445566, 3, 0.5, true, 0.2);

    private static GlobalDefaults Defaults(bool subtitlesEnabled = true, bool resume = true) => new()
    {
        Speed = 1.5f,
        Decoder = DecoderMode.Software,
        Aspect = AspectMode.Crop,
        AudioDelayMs = 250,
        SubtitleDelayMs = -300,
        SubtitlesEnabled = subtitlesEnabled,
        ResumeFromLastPosition = resume,
        SubtitleStyle = Style(),
    };

    private static PlaybackState State(long positionMs = 0, bool finished = false) =>
        new(TestData.Id()) { PositionMs = positionMs, Finished = finished };

    [Fact]
    public void Resolve_NullState_UsesDefaultsForPlaybackValues()
    {
        var result = PlaybackSettingsResolver.Resolve(null, Defaults());

        Assert.Equal(1.5f, result.Speed);
        Assert.Equal(DecoderMode.Software, result.Decoder);
        Assert.Equal(AspectMode.Crop, result.Aspect);
        Assert.Equal(250, result.AudioDelayMs);
        Assert.Equal(-300, result.SubtitleDelayMs);
        Assert.Null(result.SubtitleTrackId);
    }

    [Fact]
    public void Resolve_NullState_StartsAtZeroWithNoTrackOrExternalSubtitle()
    {
        var result = PlaybackSettingsResolver.Resolve(null, Defaults());

        Assert.Equal(0, result.StartPositionMs);
        Assert.Null(result.AudioTrackId);
        Assert.Null(result.ExternalSubtitleUri);
    }

    [Fact]
    public void Resolve_PerFileSpeedSet_WinsOverDefault()
    {
        var state = State() with { Speed = 2.0f };

        Assert.Equal(2.0f, PlaybackSettingsResolver.Resolve(state, Defaults()).Speed);
    }

    [Fact]
    public void Resolve_PerFileDecoderSet_WinsOverDefault()
    {
        var state = State() with { Decoder = DecoderMode.Hardware };

        Assert.Equal(DecoderMode.Hardware, PlaybackSettingsResolver.Resolve(state, Defaults()).Decoder);
    }

    [Fact]
    public void Resolve_PerFileAspectSet_WinsOverDefault()
    {
        var state = State() with { Aspect = AspectMode.Fit };

        Assert.Equal(AspectMode.Fit, PlaybackSettingsResolver.Resolve(state, Defaults()).Aspect);
    }

    [Fact]
    public void Resolve_PerFileAudioTrackSet_IsUsed()
    {
        var state = State() with { AudioTrackId = 4 };

        Assert.Equal(4, PlaybackSettingsResolver.Resolve(state, Defaults()).AudioTrackId);
    }

    [Fact]
    public void Resolve_PerFileAudioDelaySet_WinsOverDefault()
    {
        var state = State() with { AudioDelayMs = 100 };

        Assert.Equal(100, PlaybackSettingsResolver.Resolve(state, Defaults()).AudioDelayMs);
    }

    [Fact]
    public void Resolve_PerFileSubtitleTrackSet_IsUsed()
    {
        var state = State() with { SubtitleTrackId = 3 };

        Assert.Equal(3, PlaybackSettingsResolver.Resolve(state, Defaults()).SubtitleTrackId);
    }

    [Fact]
    public void Resolve_PerFileSubtitleTrackSetAndSubtitlesDisabled_PerFileTrackWins()
    {
        var state = State() with { SubtitleTrackId = 3 };

        var result = PlaybackSettingsResolver.Resolve(state, Defaults(subtitlesEnabled: false));

        Assert.Equal(3, result.SubtitleTrackId);
    }

    [Fact]
    public void Resolve_ResumeAndNegativeStoredPosition_StartsAtZero()
    {
        var result = PlaybackSettingsResolver.Resolve(State(positionMs: -500), Defaults(resume: true));

        Assert.Equal(0, result.StartPositionMs);
    }

    [Fact]
    public void Resolve_PerFileSubtitleDelaySet_WinsOverDefault()
    {
        var state = State() with { SubtitleDelayMs = -50 };

        Assert.Equal(-50, PlaybackSettingsResolver.Resolve(state, Defaults()).SubtitleDelayMs);
    }

    [Fact]
    public void Resolve_PerFileExternalSubtitleSet_IsUsed()
    {
        var state = State() with { ExternalSubtitleUri = "content://subs/1.srt" };

        Assert.Equal("content://subs/1.srt", PlaybackSettingsResolver.Resolve(state, Defaults()).ExternalSubtitleUri);
    }

    [Fact]
    public void Resolve_PerFileValuesNull_FallBackToDefaults()
    {
        var state = State();

        var result = PlaybackSettingsResolver.Resolve(state, Defaults());

        Assert.Equal(1.5f, result.Speed);
        Assert.Equal(DecoderMode.Software, result.Decoder);
        Assert.Equal(AspectMode.Crop, result.Aspect);
        Assert.Equal(250, result.AudioDelayMs);
        Assert.Equal(-300, result.SubtitleDelayMs);
    }

    [Fact]
    public void Resolve_ResumeAndNotFinished_StartsAtStatePosition()
    {
        var result = PlaybackSettingsResolver.Resolve(State(positionMs: 12_345), Defaults(resume: true));

        Assert.Equal(12_345, result.StartPositionMs);
    }

    [Fact]
    public void Resolve_StateFinished_StartsAtZero()
    {
        var result = PlaybackSettingsResolver.Resolve(State(positionMs: 99_000, finished: true), Defaults());

        Assert.Equal(0, result.StartPositionMs);
    }

    [Fact]
    public void Resolve_ResumeOff_StartsAtZero()
    {
        var result = PlaybackSettingsResolver.Resolve(State(positionMs: 12_345), Defaults(resume: false));

        Assert.Equal(0, result.StartPositionMs);
    }

    [Fact]
    public void Resolve_NoPerFileSubtitleTrackAndSubtitlesEnabled_ReturnsNull()
    {
        var result = PlaybackSettingsResolver.Resolve(State(), Defaults(subtitlesEnabled: true));

        Assert.Null(result.SubtitleTrackId);
    }

    [Fact]
    public void Resolve_NoPerFileSubtitleTrackAndSubtitlesDisabled_ReturnsMinusOne()
    {
        var result = PlaybackSettingsResolver.Resolve(State(), Defaults(subtitlesEnabled: false));

        Assert.Equal(-1, result.SubtitleTrackId);
    }

    [Fact]
    public void Resolve_AnyState_SubtitleStyleComesFromDefaults()
    {
        var state = State(positionMs: 500) with { SubtitleTrackId = 2, SubtitleDelayMs = 10 };

        var result = PlaybackSettingsResolver.Resolve(state, Defaults());

        Assert.Equal(Style(), result.SubtitleStyle);
    }

    [Fact]
    public void Resolve_NullDefaults_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => PlaybackSettingsResolver.Resolve(State(), null!));
    }
}
