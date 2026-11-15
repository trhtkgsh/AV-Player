using System.ComponentModel;

namespace Player.App.Localization;

/// <summary>
/// The text of one key in the language in effect. XAML binds to <see cref="Value"/>; the localizer refreshes it
/// when the language changes.
/// </summary>
public sealed class LocalizedString : INotifyPropertyChanged
{
    private string _value;

    internal LocalizedString(string key)
    {
        Key = key;
        _value = $"[{key}]";
    }

    /// <summary>The key of the text.</summary>
    public string Key { get; }

    /// <summary>The current text.</summary>
    public string Value => _value;

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    internal void Refresh(string value)
    {
        if (string.Equals(_value, value, StringComparison.Ordinal))
        {
            return;
        }

        _value = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
    }
}
