using Player.Core.Contracts;
using Player.Core.Models;

namespace Player.App.Features.Explorer;

/// <summary>One folders-page row. Display data over a <see cref="MediaFolder"/>. The texts follow the language of <see cref="Localizer"/>.</summary>
public sealed record FolderRow(MediaFolder Folder, ILocalizer Localizer)
{
    public string Name => Folder.Name;

    public string CountText => Localizer.Plural("Explorer.Items", Folder.ItemCount);

    public bool HasNew => Folder.NewCount > 0;

    public string NewText => Localizer.Plural("Explorer.NewCount", Folder.NewCount);
}
