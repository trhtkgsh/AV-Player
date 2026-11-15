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

	public const string TargetMissingError = "The target folder was not found in the media library.";
	public const string OtherVolumeError = "Moving to another storage volume is not supported.";
	public const string TargetNameTakenError = "A file with this name already exists in the target folder.";
	public const string RefusedKindError = "Android does not allow this kind of file in that folder.";
	public const string AccessDeniedError = "Access to the file was denied.";
	public const string CopyFailedError = "The file could not be copied.";

	/// <summary>The first free name in a folder: the name itself, then "name (1).ext", "name (2).ext" and so on.</summary>
	public static string FreeName(string name, Func<string, bool> exists)
	{
		ArgumentNullException.ThrowIfNull(exists);
		if (!exists(name))
		{
			return name;
		}

		var stem = Path.GetFileNameWithoutExtension(name);
		var extension = Path.GetExtension(name);
		for (var n = 1; ; n++)
		{
			var candidate = $"{stem} ({n}){extension}";
			if (!exists(candidate))
			{
				return candidate;
			}
		}
	}

	/// <summary>The name of a copied subtitle when the video copy got another name: the subtitle keeps matching it.</summary>
	public static string SidecarName(string oldVideoName, string newVideoName, string sidecarName)
	{
		var oldStem = Path.GetFileNameWithoutExtension(oldVideoName);
		var newStem = Path.GetFileNameWithoutExtension(newVideoName);
		return oldStem.Length > 0 && sidecarName.StartsWith(oldStem, StringComparison.OrdinalIgnoreCase)
			? newStem + sidecarName[oldStem.Length..]
			: sidecarName;
	}

	/// <summary>
	/// The result of a batch: Success when nothing failed, otherwise Failed with a short text naming the first
	/// cause. <see cref="FileOperationResult.CompletedCount"/> is the number of files that were done.
	/// </summary>
	public static FileOperationResult BatchResult(int completed, int total, string? firstError, string verb)
	{
		if (firstError is null)
		{
			return new FileOperationResult(FileOperationStatus.Success, null, null) { CompletedCount = completed };
		}

		var text = $"{total - completed} of {total} files were not {verb}. {firstError}";
		return new FileOperationResult(FileOperationStatus.Failed, null, text) { CompletedCount = completed };
	}

	public static FileOperationResult WithCount(FileOperationResult result, int completed) =>
		result with { CompletedCount = completed };

	/// <summary>True when the state of the old identity must be moved to the new one.</summary>
	public static bool IdentityChanged(MediaFileId before, MediaFileId after) => before != after;
}
