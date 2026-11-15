using Player.Core.Models;

namespace Player.Core.Logic.Explorer;

/// <summary>
/// Builds one level of a folder tree from the media folders' relative paths.
/// Pure logic: no platform or storage types.
/// </summary>
public static class FolderTree
{
    /// <summary>
    /// Returns the distinct next path segments under <paramref name="parentPath"/>, sorted by name.
    /// A media folder whose path equals <paramref name="parentPath"/> is not a child.
    /// Use an empty string as the parent for the storage root.
    /// </summary>
    public static IReadOnlyList<FolderTreeNode> GetChildren(IReadOnlyList<MediaFolder> folders, string parentPath)
    {
        ArgumentNullException.ThrowIfNull(folders);
        ArgumentNullException.ThrowIfNull(parentPath);

        var parent = Normalize(parentPath);
        var groups = new Dictionary<string, Group>(StringComparer.Ordinal);

        foreach (var folder in folders)
        {
            if (folder is null)
            {
                continue;
            }

            var path = PathOf(folder);
            if (path.Length == 0 || path == parent)
            {
                continue;
            }

            string rest;
            if (parent.Length == 0)
            {
                rest = path;
            }
            else if (path.StartsWith(parent + "/", StringComparison.Ordinal))
            {
                rest = path[(parent.Length + 1)..];
            }
            else
            {
                continue;
            }

            var slash = rest.IndexOf('/', StringComparison.Ordinal);
            var segment = slash < 0 ? rest : rest[..slash];
            var childPath = parent.Length == 0 ? segment : parent + "/" + segment;

            if (!groups.TryGetValue(childPath, out var group))
            {
                group = new Group(segment, childPath);
                groups.Add(childPath, group);
            }

            group.Add(folder, path == childPath);
        }

        return groups.Values
            .OrderBy(g => g.Name, NaturalNameComparer.Instance)
            .ThenBy(g => g.Path, StringComparer.Ordinal)
            .Select(g => new FolderTreeNode(g.Name, g.Path, g.Folder, g.FolderCount, g.ItemCount, g.NewCount))
            .ToList();
    }

    /// <summary>The normalized path of a media folder: its relative path, or its name when the relative path is empty.</summary>
    private static string PathOf(MediaFolder folder)
    {
        var path = Normalize(folder.RelativePath);
        return path.Length > 0 ? path : Normalize(folder.Name);
    }

    /// <summary>No leading or trailing slash and no empty segments; the comparison stays ordinal and case-sensitive.</summary>
    private static string Normalize(string path) =>
        string.Join('/', path.Split('/', StringSplitOptions.RemoveEmptyEntries));

    private sealed class Group(string name, string path)
    {
        public string Name { get; } = name;

        public string Path { get; } = path;

        public MediaFolder? Folder { get; private set; }

        public int FolderCount { get; private set; }

        public int ItemCount { get; private set; }

        public int NewCount { get; private set; }

        public void Add(MediaFolder folder, bool isOwnPath)
        {
            FolderCount++;
            ItemCount += folder.ItemCount;
            NewCount += folder.NewCount;
            if (isOwnPath && Folder is null)
            {
                Folder = folder;
            }
        }
    }
}
