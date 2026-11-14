using Player.Core.Models;

namespace Player.App.Features.Explorer;

/// <summary>One folders-page row. Display data over a <see cref="MediaFolder"/>.</summary>
public sealed record FolderRow(MediaFolder Folder)
{
    public string Name => Folder.Name;

    public string CountText => Folder.ItemCount == 1 ? "1 item" : $"{Folder.ItemCount} items";

    public bool HasNew => Folder.NewCount > 0;

    public string NewText => $"{Folder.NewCount} new";
}
