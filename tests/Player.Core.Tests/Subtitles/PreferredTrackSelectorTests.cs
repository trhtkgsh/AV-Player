using Player.Core.Logic.Subtitles;
using Player.Core.Models;

namespace Player.Core.Tests.Subtitles;

public sealed class PreferredTrackSelectorTests
{
    private static TrackInfo Track(int id, string name, string? language) => new(id, name, language);

    // --- audio ---

    [Fact]
    public void SelectAudio_LanguageMatches_ReturnsThatTrackId()
    {
        var tracks = new[] { Track(1, "Track 1", "en"), Track(2, "Track 2", "he") };

        Assert.Equal(2, PreferredTrackSelector.SelectAudio(tracks, "he"));
    }

    [Fact]
    public void SelectAudio_SeveralLanguageMatches_ReturnsFirst()
    {
        var tracks = new[] { Track(1, "A", "en"), Track(2, "B", "heb"), Track(3, "C", "he") };

        Assert.Equal(2, PreferredTrackSelector.SelectAudio(tracks, "he"));
    }

    [Theory]
    [InlineData("heb")]
    [InlineData("iw")]
    [InlineData("Hebrew")]
    [InlineData("HE")]
    public void SelectAudio_PreferredLanguageInOtherForm_Matches(string preferred)
    {
        var tracks = new[] { Track(1, "A", "en"), Track(2, "B", "he") };

        Assert.Equal(2, PreferredTrackSelector.SelectAudio(tracks, preferred));
    }

    [Fact]
    public void SelectAudio_NoLanguageMatchButNameHasLanguageWord_ReturnsThatTrackId()
    {
        var tracks = new[] { Track(1, "Track 1", "en"), Track(2, "Track 2 - [Hebrew]", null) };

        Assert.Equal(2, PreferredTrackSelector.SelectAudio(tracks, "he"));
    }

    [Fact]
    public void SelectAudio_LanguageMatchOnLaterTrack_WinsOverNameMatchOnEarlierTrack()
    {
        var tracks = new[] { Track(1, "Track 1 - Hebrew", null), Track(2, "Track 2", "he") };

        Assert.Equal(2, PreferredTrackSelector.SelectAudio(tracks, "he"));
    }

    [Fact]
    public void SelectAudio_SeveralNameMatches_ReturnsFirst()
    {
        var tracks = new[] { Track(1, "English", null), Track(2, "Hebrew 5.1", null), Track(3, "Hebrew", null) };

        Assert.Equal(2, PreferredTrackSelector.SelectAudio(tracks, "he"));
    }

    [Fact]
    public void SelectAudio_NameContainsLanguageOnlyInsideAnotherWord_DoesNotMatch()
    {
        var tracks = new[] { Track(1, "Hebrews", null), Track(2, "Cheetah", null) };

        Assert.Null(PreferredTrackSelector.SelectAudio(tracks, "he"));
    }

    [Fact]
    public void SelectAudio_NothingMatches_ReturnsNull()
    {
        var tracks = new[] { Track(1, "Track 1", "en"), Track(2, "Track 2", "fr") };

        Assert.Null(PreferredTrackSelector.SelectAudio(tracks, "he"));
    }

    [Fact]
    public void SelectAudio_EmptyList_ReturnsNull()
    {
        Assert.Null(PreferredTrackSelector.SelectAudio(Array.Empty<TrackInfo>(), "he"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void SelectAudio_NoPreferredLanguage_ReturnsNull(string? preferred)
    {
        var tracks = new[] { Track(1, "Track 1", "he") };

        Assert.Null(PreferredTrackSelector.SelectAudio(tracks, preferred));
    }

    [Fact]
    public void SelectAudio_TrackWithNegativeIdMatchesByLanguage_IsIgnored()
    {
        var tracks = new[] { Track(-1, "Disabled", "he"), Track(2, "Track 2", "he") };

        Assert.Equal(2, PreferredTrackSelector.SelectAudio(tracks, "he"));
    }

    [Fact]
    public void SelectAudio_OnlyNegativeIdMatchesByLanguage_ReturnsNull()
    {
        var tracks = new[] { Track(-1, "Disabled", "he") };

        Assert.Null(PreferredTrackSelector.SelectAudio(tracks, "he"));
    }

    [Fact]
    public void SelectAudio_OnlyNegativeIdMatchesByName_ReturnsNull()
    {
        var tracks = new[] { Track(-1, "Hebrew", null) };

        Assert.Null(PreferredTrackSelector.SelectAudio(tracks, "he"));
    }

    [Fact]
    public void SelectAudio_TrackWithIdZero_CanBeSelected()
    {
        var tracks = new[] { Track(0, "Track 0", "he") };

        Assert.Equal(0, PreferredTrackSelector.SelectAudio(tracks, "he"));
    }

    // --- subtitle ---

    [Fact]
    public void SelectSubtitle_SubtitlesDisabled_ReturnsMinusOne()
    {
        var tracks = new[] { Track(1, "Track 1", "he") };

        Assert.Equal(-1, PreferredTrackSelector.SelectSubtitle(tracks, "he", subtitlesEnabled: false));
    }

    [Fact]
    public void SelectSubtitle_SubtitlesDisabledAndNoTracks_ReturnsMinusOne()
    {
        Assert.Equal(-1, PreferredTrackSelector.SelectSubtitle(Array.Empty<TrackInfo>(), null, subtitlesEnabled: false));
    }

    [Fact]
    public void SelectSubtitle_SubtitlesDisabledAndNothingMatches_ReturnsMinusOne()
    {
        var tracks = new[] { Track(1, "Track 1", "en") };

        Assert.Equal(-1, PreferredTrackSelector.SelectSubtitle(tracks, "he", subtitlesEnabled: false));
    }

    [Fact]
    public void SelectSubtitle_EnabledAndLanguageMatches_ReturnsThatTrackId()
    {
        var tracks = new[] { Track(1, "Track 1", "en"), Track(2, "Track 2", "he") };

        Assert.Equal(2, PreferredTrackSelector.SelectSubtitle(tracks, "he", subtitlesEnabled: true));
    }

    [Fact]
    public void SelectSubtitle_EnabledAndOnlyNameMatches_ReturnsThatTrackId()
    {
        var tracks = new[] { Track(1, "Track 1", "en"), Track(2, "Track 2 - [Hebrew]", null) };

        Assert.Equal(2, PreferredTrackSelector.SelectSubtitle(tracks, "he", subtitlesEnabled: true));
    }

    [Fact]
    public void SelectSubtitle_LanguageMatchOnLaterTrack_WinsOverNameMatchOnEarlierTrack()
    {
        var tracks = new[] { Track(1, "Hebrew", null), Track(2, "Track 2", "heb") };

        Assert.Equal(2, PreferredTrackSelector.SelectSubtitle(tracks, "he", subtitlesEnabled: true));
    }

    [Fact]
    public void SelectSubtitle_EnabledAndNothingMatches_ReturnsNull()
    {
        var tracks = new[] { Track(1, "Track 1", "en") };

        Assert.Null(PreferredTrackSelector.SelectSubtitle(tracks, "he", subtitlesEnabled: true));
    }

    [Fact]
    public void SelectSubtitle_EnabledAndEmptyList_ReturnsNull()
    {
        Assert.Null(PreferredTrackSelector.SelectSubtitle(Array.Empty<TrackInfo>(), "he", subtitlesEnabled: true));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void SelectSubtitle_EnabledAndNoPreferredLanguage_ReturnsNull(string? preferred)
    {
        var tracks = new[] { Track(1, "Track 1", "he") };

        Assert.Null(PreferredTrackSelector.SelectSubtitle(tracks, preferred, subtitlesEnabled: true));
    }

    [Fact]
    public void SelectSubtitle_TrackWithNegativeId_IsIgnored()
    {
        var tracks = new[] { Track(-1, "Off", "he"), Track(3, "Track 3", "he") };

        Assert.Equal(3, PreferredTrackSelector.SelectSubtitle(tracks, "he", subtitlesEnabled: true));
    }
}
