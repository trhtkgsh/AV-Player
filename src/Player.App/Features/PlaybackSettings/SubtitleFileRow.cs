namespace Player.App.Features.PlaybackSettings;

/// <summary>One subtitle file that sits next to the video.</summary>
public sealed class SubtitleFileRow
{
    public SubtitleFileRow(string contentUri, string title)
    {
        ContentUri = contentUri;
        Title = title;
    }

    public string ContentUri { get; }

    public string Title { get; }
}
