using CommunityToolkit.Mvvm.ComponentModel;
using Player.Core.Models;

namespace Player.App.Features.PlaybackSettings;

/// <summary>One track in a track list. Also used by the subtitle sheet.</summary>
public sealed partial class TrackRow : ObservableObject
{
    public TrackRow(int id, string title, bool isSelected)
    {
        Id = id;
        Title = title;
        _isSelected = isSelected;
    }

    public int Id { get; }

    public string Title { get; }

    [ObservableProperty] private bool _isSelected;

    /// <summary>
    /// The name, plus the language in brackets when there is a language that the name does not already contain.
    /// </summary>
    public static string TitleOf(TrackInfo track)
    {
        string? language = track.Language;
        if (string.IsNullOrEmpty(language) || track.Name.Contains(language, StringComparison.OrdinalIgnoreCase))
        {
            return track.Name;
        }

        return track.Name + " (" + language + ")";
    }
}
