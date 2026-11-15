using Player.Core.Models;

namespace Player.App.FileOps;

/// <summary>What happened to a consent dialog.</summary>
public enum ConsentOutcome
{
	Granted,

	/// <summary>The user said no, the token was cancelled or the activity went away.</summary>
	Declined,

	/// <summary>No activity was available to show the dialog.</summary>
	Unavailable,
}

/// <summary>The platform-independent decisions of the file operations. No Android types, so they are unit-testable.</summary>
public static class FileOperationRules
{
	public const string EmptyNameError = "The name is empty.";
	public const string SeparatorError = "The name must not contain / or \\.";
	public const string NameTakenError = "A file with this name already exists in the folder.";
	public const string NoConsentWindowError = "No window is available to ask for permission.";

	/// <summary>Returns an error text when the name cannot be used for a file, otherwise null.</summary>
	public static string? ValidateName(string? newDisplayName)
	{
		if (string.IsNullOrWhiteSpace(newDisplayName))
		{
			return EmptyNameError;
		}

		if (newDisplayName.Contains('/') || newDisplayName.Contains('\\'))
		{
			return SeparatorError;
		}

		return newDisplayName is "." or ".." ? EmptyNameError : null;
	}

	/// <summary>True when the new name is the current one: nothing to do.</summary>
	public static bool IsSameName(string currentName, string newName) =>
		string.Equals(currentName, newName, StringComparison.Ordinal);

	public static FileOperationResult Failed(string message) => new(FileOperationStatus.Failed, null, message);

	public static FileOperationResult Denied(string message) => new(FileOperationStatus.Denied, null, message);

	public static FileOperationResult Cancelled() => new(FileOperationStatus.Cancelled, null, null);

	/// <summary>The result for a consent that did not end in a grant.</summary>
	public static FileOperationResult FromConsent(ConsentOutcome outcome) => outcome switch
	{
		ConsentOutcome.Declined => Cancelled(),
		_ => Failed(NoConsentWindowError),
	};

	/// <summary>True when the state of the old identity must be moved to the new one.</summary>
	public static bool IdentityChanged(MediaFileId before, MediaFileId after) => before != after;
}
