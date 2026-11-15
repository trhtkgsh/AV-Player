using Player.Core.Models;

namespace Player.Core.Logic.Explorer;

/// <summary>
/// One level of the folder tree: a path segment under the parent path.
/// <see cref="Folder"/> is the media folder at exactly this path, or null when the node is only a container.
/// The counts are totals over all media folders at or below this node.
/// </summary>
public sealed record FolderTreeNode(string Name, string Path, MediaFolder? Folder, int FolderCount, int ItemCount, int NewCount);
