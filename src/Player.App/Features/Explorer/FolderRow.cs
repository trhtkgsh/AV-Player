using Player.Core.Contracts;
using Player.Core.Models;

namespace Player.App.Features.Explorer;

/// <summary>What a folders-page row is: a media folder, a file-browser node that holds more nodes, or the files of the current file-browser folder.</summary>
public enum FolderRowKind { Folder, Container, FilesHere }

/// <summary>
/// One folders-page row. Display data over a media folder or a file-browser node.
/// <see cref="TreePath"/> is set for the file-browser nodes. The texts follow the language of <see cref="Localizer"/>.
/// </summary>
public sealed record FolderRow(FolderRowKind Kind, string Name, MediaFolder? Folder, int ItemCount, int NewCount, ILocalizer Localizer, string? TreePath = null)
{
    public string DisplayName => Kind == FolderRowKind.FilesHere ? Localizer.Format("Explorer.FilesHere", ItemCount) : Name;

    public bool ShowCount => Kind != FolderRowKind.FilesHere;

    public string CountText => Localizer.Plural("Explorer.Items", ItemCount);

    public bool HasNew => NewCount > 0;

    public string NewText => Localizer.Plural("Explorer.NewCount", NewCount);
}
