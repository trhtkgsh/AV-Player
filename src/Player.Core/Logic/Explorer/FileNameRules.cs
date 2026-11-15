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

    /// <summary>The typed base name with surrounding spaces and trailing dots or spaces removed.</summary>
    public static string BuildName(string typedBaseName, string extension)
    {
        ArgumentNullException.ThrowIfNull(typedBaseName);
        ArgumentNullException.ThrowIfNull(extension);

        return Normalize(typedBaseName) + extension;
    }

    /// <summary>
    /// Validates the typed base name. "." and ".." are reserved before the trailing dots are removed,
    /// so they report as reserved. The length and the unchanged test use the full new name.
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

        if (trimmed is "." or "..")
        {
            return FileNameProblem.Reserved;
        }

        if (trimmed.IndexOfAny(InvalidChars) >= 0)
        {
            return FileNameProblem.InvalidCharacter;
        }

        var stripped = Normalize(trimmed);
        if (stripped.Length == 0)
        {
            return FileNameProblem.Empty;
        }

        var full = stripped + extension;
        if (full.Length > MaxLength)
        {
            return FileNameProblem.TooLong;
        }

        return string.Equals(full, currentDisplayName, StringComparison.Ordinal)
            ? FileNameProblem.Unchanged
            : FileNameProblem.None;
    }

    private static string Normalize(string typedBaseName) =>
        typedBaseName.Trim().TrimEnd('.', ' ');
}

