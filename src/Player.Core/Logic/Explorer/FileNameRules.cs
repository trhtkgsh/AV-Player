namespace Player.Core.Logic.Explorer;

/// <summary>Why a typed file name cannot be used. <see cref="None"/> means it can.</summary>
public enum FileNameProblem
{
    None,
    Empty,
    InvalidCharacter,
    Reserved,
    TooLong,

    /// <summary>The name is the current one: the rename closes quietly.</summary>
    Unchanged,
}

/// <summary>Rules for renaming a file: the typed base name, the kept extension and the validation.</summary>
public static class FileNameRules
{
    public const int MaxLength = 200;

    private static readonly char[] InvalidChars = { '/', '\\', ':', '*', '?', '"', '<', '>', '|' };

    /// <summary>Splits a display name at its last dot. A leading dot is part of the name, not an extension.</summary>
    /// <returns>The base name and the extension with its dot (empty when there is none).</returns>
    public static (string BaseName, string Extension) Split(string displayName)
    {
        ArgumentNullException.ThrowIfNull(displayName);

        var dot = displayName.LastIndexOf('.');
        return dot <= 0
            ? (displayName, string.Empty)
            : (displayName[..dot], displayName[dot..]);
    }

    /// <summary>The trimmed typed base name followed by the extension.</summary>
    public static string BuildName(string typedBaseName, string extension)
    {
        ArgumentNullException.ThrowIfNull(typedBaseName);
        ArgumentNullException.ThrowIfNull(extension);

        return typedBaseName.Trim() + extension;
    }

    /// <summary>
    /// Validates the typed base name. The checks run on the trimmed text and on the full new name
    /// (base name plus extension) for the length and the unchanged test.
    /// </summary>
    public static FileNameProblem Check(string typedBaseName, string extension, string currentDisplayName)
    {
        ArgumentNullException.ThrowIfNull(typedBaseName);
        ArgumentNullException.ThrowIfNull(extension);
        ArgumentNullException.ThrowIfNull(currentDisplayName);

        var trimmed = typedBaseName.Trim();
        if (trimmed.Length == 0)
        {
            return FileNameProblem.Empty;
        }

        if (trimmed.IndexOfAny(InvalidChars) >= 0)
        {
            return FileNameProblem.InvalidCharacter;
        }

        if (trimmed is "." or "..")
        {
            return FileNameProblem.Reserved;
        }

        var full = trimmed + extension;
        if (full.Length > MaxLength)
        {
            return FileNameProblem.TooLong;
        }

        return string.Equals(full, currentDisplayName, StringComparison.Ordinal)
            ? FileNameProblem.Unchanged
            : FileNameProblem.None;
    }
}
