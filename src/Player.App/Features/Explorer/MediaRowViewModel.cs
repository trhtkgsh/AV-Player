using CommunityToolkit.Mvvm.ComponentModel;
using Player.Core.Logic.Explorer;
using Player.Core.Logic.Library;
using Player.Core.Models;

namespace Player.App.Features.Explorer;

/// <summary>One explorer row. <see cref="Apply"/> must be called on the UI thread.</summary>
public sealed partial class MediaRowViewModel : ObservableObject
{
    public MediaRowViewModel(MediaItem item)
    {
        Item = item;
        Name = item.DisplayName;
    }

    public MediaItem Item { get; }

    public string Name { get; }

    public MediaMetadata? Metadata { get; private set; }

    public PlaybackState? State { get; private set; }

    [ObservableProperty]
    private string? _thumbnailPath;

    [ObservableProperty]
    private string _infoLine = string.Empty;

    [ObservableProperty]
    private bool _isNew;

    [ObservableProperty]
    private double _progress;

    [ObservableProperty]
    private bool _hasProgress;

    [ObservableProperty]
    private bool _isWatched;

    public void Apply(
        MediaMetadata? metadata,
        PlaybackState? state,
        UiSettings settings,
        long nowUnixSeconds,
        TimeZoneInfo zone)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(zone);

        Metadata = metadata;
        State = state;
        ThumbnailPath = metadata?.ThumbnailPath;
        InfoLine = MediaInfoFormatter.InfoLine(Item, metadata, settings.ExplorerFields, zone);
        IsNew = NewTagRule.IsNew(Item, state, settings.NewTagDays, nowUnixSeconds);
        Progress = ProgressCalculator.Fraction(state);
        HasProgress = Progress > 0;
        IsWatched = ProgressCalculator.IsWatched(state);
    }
}
