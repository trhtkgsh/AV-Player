using Player.Core.Models;

namespace Player.Core.Logic.Explorer;

/// <summary>The rules of the selection mode. No MAUI types: the App reads them.</summary>
public static class SelectionRules
{
    /// <summary>
    /// Delete, move and copy need at least one selected row (a file or a folder). Rename needs exactly one
    /// selected file and no folder.
    /// </summary>
    public static bool Allows(SelectionAction action, int selectedFiles, int selectedFolders) => action switch
    {
        SelectionAction.Rename => selectedFiles == 1 && selectedFolders == 0,
        _ => selectedFiles + selectedFolders > 0,
    };

    /// <summary>The items with distinct keys, in their first-seen order.</summary>
    public static IReadOnlyList<T> Distinct<T, TKey>(IEnumerable<T> items, Func<T, TKey> key)
        where TKey : notnull
    {
        var seen = new HashSet<TKey>();
        var result = new List<T>();
        foreach (var item in items)
        {
            if (seen.Add(key(item)))
            {
                result.Add(item);
            }
        }

        return result;
    }

    /// <summary>The one bucket all the given buckets share, or null when they differ or there are none.</summary>
    public static long? SingleBucket(IEnumerable<long> buckets)
    {
        long? first = null;
        foreach (var bucket in buckets)
        {
            if (first is null)
            {
                first = bucket;
            }
            else if (first != bucket)
            {
                return null;
            }
        }

        return first;
    }

    /// <summary>
    /// The folders a move or copy can target: all of them in their list order, without the source folder.
    /// Pass null as the source when the selected files are not all in one folder.
    /// </summary>
    public static IReadOnlyList<MediaFolder> TargetChoices(IEnumerable<MediaFolder> folders, long? sourceBucket) =>
        folders.Where(folder => folder.BucketId != sourceBucket).ToList();
}
