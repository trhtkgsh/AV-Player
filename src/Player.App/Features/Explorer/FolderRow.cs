using Player.Core.Contracts;
using Player.Core.Models;

namespace Player.App.Features.Explorer;

/// <summary>What a folders-page row is: a media folder, a file-browser node that holds more nodes, or the files of the current file-browser folder.</summary>
public enum FolderRowKind { Folder, Container, FilesHere }

/// <summary>
/// One folders-page row. Display data over a media folder or a file-browser node.
/// <see cref="TreePath"/> is set for the file-browser nodes. <see cref="Favorite"/> is the folder's favorite mark.
/// The texts follow the language of <see cref="Localizer"/>.
/// </summary>
public sealed record FolderRow(FolderRowKind Kind, string Name, MediaFolder? Folder, int ItemCount, int NewCount, ILocalizer Localizer, string? TreePath = null, bool Favorite = false, bool Selected = false)
{
    public string DisplayName => Kind == FolderRowKind.FilesHere ? Localizer.Format("Explorer.FilesHere", ItemCount) : Name;

    public bool ShowCount => Kind != FolderRowKind.FilesHere;

    // The "more" button opens the folder options. The files-here row is the current folder, so it has none.
    public bool HasOptions => Folder is not null && Kind != FolderRowKind.FilesHere;

    public string CountText => Localizer.Plural("Explorer.Items", ItemCount);

    public bool HasNew => NewCount > 0;

    public string NewText => Localizer.Plural("Explorer.NewCount", NewCount);

    // The screen reader text of a selected row: its name and "selected". Null when it is not selected, so the row reads as before.
    public string? AccessibleName => Selected ? Localizer.Format("Explorer.SelectedItem", DisplayName) : null;
}
