using Player.Core.Models;

namespace Player.Core.Logic.Subtitles;

/// <summary>
/// Chooses the audio and subtitle track that matches the preferred language.
/// </summary>
public static class PreferredTrackSelector
{
    /// <summary>The id of the preferred audio track, or null to keep the engine's choice.</summary>
    public static int? SelectAudio(IReadOnlyList<TrackInfo> tracks, string? preferredLanguage) =>
        Find(tracks, preferredLanguage);

    /// <summary>-1 when subtitles are off, else the id of the preferred subtitle track, or null to keep the engine's choice.</summary>
    public static int? SelectSubtitle(IReadOnlyList<TrackInfo> tracks, string? preferredLanguage, bool subtitlesEnabled) =>
        subtitlesEnabled ? Find(tracks, preferredLanguage) : -1;

    private static int? Find(IReadOnlyList<TrackInfo> tracks, string? preferredLanguage)
    {
        if (tracks is null || tracks.Count == 0 || string.IsNullOrWhiteSpace(preferredLanguage))
        {
            return null;
        }

        foreach (var track in tracks)
        {
            if (track is not null && track.Id >= 0 && LanguageCodes.Matches(track.Language, preferredLanguage))
            {
                return track.Id;
            }
        }

        foreach (var track in tracks)
        {
            if (track is not null && track.Id >= 0 && NameHasLanguage(track.Name, preferredLanguage))
            {
                return track.Id;
            }
        }

        return null;
    }

    private static bool NameHasLanguage(string? name, string language)
    {
        if (string.IsNullOrEmpty(name))
        {
            return false;
        }

        var start = -1;
        for (var i = 0; i <= name.Length; i++)
        {
            var letter = i < name.Length && char.IsLetter(name[i]);
            if (letter && start < 0)
            {
                start = i;
            }
            else if (!letter && start >= 0)
            {
                if (LanguageCodes.Matches(name.Substring(start, i - start), language))
                {
                    return true;
                }

                start = -1;
            }
        }

        return false;
    }
}
