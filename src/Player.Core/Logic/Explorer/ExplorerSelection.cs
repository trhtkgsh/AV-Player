namespace Player.Core.Logic.Explorer;

/// <summary>The actions of the selection mode. Rename is for exactly one selected file.</summary>
public enum SelectionAction
{
    Delete,
    Move,
    Copy,
    Rename,
}

/// <summary>
/// The selected keys of a list: media file ids on the items page, bucket ids on the folders page.
/// The selection mode is on while at least one key is selected, so clearing the last key leaves the mode.
/// </summary>
public sealed class ExplorerSelection<TKey>
    where TKey : notnull
{
    private readonly HashSet<TKey> _keys = new();

    public int Count => _keys.Count;

    public bool IsActive => _keys.Count > 0;

    public IReadOnlyCollection<TKey> Keys => _keys;

    public bool Contains(TKey key) => _keys.Contains(key);

    /// <summary>Selects the key when it is not selected, clears it when it is. Returns true when the key is selected afterwards.</summary>
    public bool Toggle(TKey key)
    {
        if (_keys.Remove(key))
        {
            return false;
        }

        _keys.Add(key);
        return true;
    }

    public void Add(TKey key) => _keys.Add(key);

    public void AddRange(IEnumerable<TKey> keys)
    {
        foreach (var key in keys)
        {
            _keys.Add(key);
        }
    }

    public void Remove(TKey key) => _keys.Remove(key);

    public void Clear() => _keys.Clear();
}
