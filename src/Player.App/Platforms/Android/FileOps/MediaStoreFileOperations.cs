using System.Runtime.Versioning;
using Android.App;
using Android.Content;
using Android.Provider;
using Android.Util;
using Player.Core.Contracts;
using Player.Core.Models;
using AndroidUri = Android.Net.Uri;
using JavaFile = Java.IO.File;
using JavaSecurityException = Java.Lang.SecurityException;

namespace Player.App.FileOps;

/// <summary>Rename and trash through MediaStore. The write rules differ per API level, see docs/agents/file-ops.md.</summary>
public sealed partial class MediaStoreFileOperations : IFileOperations
{
	private const string Tag = "FileOps";

	private readonly IPlaybackStateStore _stateStore;
	private readonly ConsentLauncher _consent;
	private readonly ISubtitleService _subtitles;
	private readonly Context _context;

	public MediaStoreFileOperations(IPlaybackStateStore stateStore, ConsentLauncher consent, ISubtitleService subtitles)
	{
		_stateStore = stateStore ?? throw new ArgumentNullException(nameof(stateStore));
		_consent = consent ?? throw new ArgumentNullException(nameof(consent));
		_subtitles = subtitles ?? throw new ArgumentNullException(nameof(subtitles));
		_context = global::Android.App.Application.Context;
	}

	private ContentResolver Resolver => _context.ContentResolver!;

	public async Task<FileOperationResult> RenameAsync(MediaItem item, string newDisplayName, CancellationToken ct)
	{
		ArgumentNullException.ThrowIfNull(item);
		var invalid = FileOperationRules.ValidateName(newDisplayName);
		if (invalid is not null)
		{
			return FileOperationRules.Failed(invalid);
		}

		if (FileOperationRules.IsSameName(item.DisplayName, newDisplayName))
		{
			return new FileOperationResult(FileOperationStatus.Success, item, null);
		}

		var committed = false;
		try
		{
			var uri = AndroidUri.Parse(item.ContentUri)!;
			if (await Task.Run(() => NameExists(item, newDisplayName), ct).ConfigureAwait(false))
			{
				return FileOperationRules.Failed(FileOperationRules.NameTakenError);
			}

			var failure = OperatingSystem.IsAndroidVersionAtLeast(29)
				? await RunWithConsentAsync(() => UpdateName(uri, newDisplayName), [uri], ct).ConfigureAwait(false)
				: await RunLegacyAsync(() => RenameLegacy(uri, newDisplayName), ct).ConfigureAwait(false);
			if (failure is not null)
			{
				return failure;
			}

			committed = true;
			return await FinishRenameAsync(item, uri, newDisplayName).ConfigureAwait(false);
		}
		catch (OperationCanceledException) when (!committed)
		{
			return FileOperationRules.Cancelled();
		}
		catch (Exception ex) when (!committed)
		{
			Log.Warn(Tag, $"Rename failed: {ex}");
			return FileOperationRules.Failed(ex.Message);
		}
	}

	// The write is done: nothing here may change the outcome, so no caller token and no exception escapes.
	private async Task<FileOperationResult> FinishRenameAsync(MediaItem item, AndroidUri uri, string newName)
	{
		var updated = item with { DisplayName = newName };
		try
		{
			var readBack = await Task.Run(() => ReadUpdated(item, uri)).ConfigureAwait(false);
			if (readBack is null)
			{
				Log.Warn(Tag, "The renamed row could not be read back; reporting the requested name.");
			}
			else
			{
				updated = readBack;
				await FollowIdentityAsync(item.Id, updated.Id).ConfigureAwait(false);
			}
		}
		catch (Exception ex)
		{
			Log.Warn(Tag, $"Reading back the renamed file failed: {ex}");
		}

		return new FileOperationResult(FileOperationStatus.Success, updated, null);
	}

	public async Task<FileOperationResult> TrashAsync(IReadOnlyList<MediaItem> items, CancellationToken ct)
	{
		ArgumentNullException.ThrowIfNull(items);
		if (items.Count == 0)
		{
			return new FileOperationResult(FileOperationStatus.Success, null, null);
		}

		var removed = new List<MediaItem>();
		try
		{
			var uris = items.Select(i => AndroidUri.Parse(i.ContentUri)!).ToList();
			var subtitles = await CollectSubtitlesAsync(items, ct).ConfigureAwait(false);
			FileOperationResult? failure;
			if (OperatingSystem.IsAndroidVersionAtLeast(30))
			{
				var withSubtitles = uris.Concat(subtitles.Values.SelectMany(s => s).Select(s => AndroidUri.Parse(s.ContentUri)!)).ToList();
				IntentSender sender;
				try
				{
					sender = await Task.Run(() => OperatingSystem.IsAndroidVersionAtLeast(30) ? TrashRequest(withSubtitles) : null!, ct).ConfigureAwait(false);
				}
				catch (Exception ex) when (ex is not OperationCanceledException && withSubtitles.Count > uris.Count)
				{
					Log.Warn(Tag, $"The trash request with subtitles was refused, trying without them: {ex.Message}");
					sender = await Task.Run(() => OperatingSystem.IsAndroidVersionAtLeast(30) ? TrashRequest(uris) : null!, ct).ConfigureAwait(false);
				}

				failure = await AskAsync(sender, ct).ConfigureAwait(false);
				if (failure is null)
				{
					removed.AddRange(items);
				}
			}
			else if (OperatingSystem.IsAndroidVersionAtLeast(29))
			{
				failure = null;
				for (var i = 0; i < uris.Count; i++)
				{
					var uri = uris[i];
					failure = await RunWithConsentAsync(() => DeleteRow(uri), [uri], ct).ConfigureAwait(false);
					if (failure is not null)
					{
						break;
					}

					removed.Add(items[i]);
				}
			}
			else
			{
				failure = await RunLegacyAsync(() => DeleteLegacy(items, uris, removed), ct).ConfigureAwait(false);
			}

			await DeleteStatesAsync(removed).ConfigureAwait(false);
			if (!OperatingSystem.IsAndroidVersionAtLeast(30))
			{
				await DeleteSidecarsAsync(removed, subtitles).ConfigureAwait(false);
			}

			return failure ?? new FileOperationResult(FileOperationStatus.Success, null, null);
		}
		catch (Exception ex)
		{
			// A throw after a partial removal still cleans the state of what is gone.
			await DeleteStatesAsync(removed).ConfigureAwait(false);
			if (ex is OperationCanceledException)
			{
				return FileOperationRules.Cancelled();
			}

			Log.Warn(Tag, $"Trash failed: {ex}");
			return FileOperationRules.Failed(ex.Message);
		}
	}

	private async Task DeleteStatesAsync(List<MediaItem> removed)
	{
		foreach (var item in removed)
		{
			await DeleteStateAsync(item.Id).ConfigureAwait(false);
		}
	}

	[SupportedOSPlatform("android30.0")]
	private IntentSender TrashRequest(List<AndroidUri> uris) =>
		MediaStore.CreateTrashRequest(Resolver, uris, true).IntentSender;

	[SupportedOSPlatform("android30.0")]
	private IntentSender WriteRequest(IReadOnlyList<AndroidUri> uris) =>
		MediaStore.CreateWriteRequest(Resolver, uris.ToList()).IntentSender;

	[SupportedOSPlatform("android29.0")]
	private static IntentSender? RecoverableSender(Exception ex) =>
		ex is RecoverableSecurityException recoverable ? recoverable.UserAction?.ActionIntent?.IntentSender : null;

	private async Task<FileOperationResult?> AskAsync(IntentSender sender, CancellationToken ct)
	{
		var outcome = await _consent.AskAsync(sender, ct).ConfigureAwait(false);
		return outcome == ConsentOutcome.Granted ? null : FileOperationRules.FromConsent(outcome);
	}

	/// <summary>API 29+. Runs the write; when the system refuses, asks for consent once and retries once. Null means success.</summary>
	private async Task<FileOperationResult?> RunWithConsentAsync(Action write, IReadOnlyList<AndroidUri> uris, CancellationToken ct)
	{
		IntentSender? sender;
		try
		{
			await Task.Run(write, ct).ConfigureAwait(false);
			return null;
		}
		catch (JavaSecurityException ex)
		{
			sender = OperatingSystem.IsAndroidVersionAtLeast(30) ? WriteRequest(uris)
				: OperatingSystem.IsAndroidVersionAtLeast(29) ? RecoverableSender(ex)
				: null;
			if (sender is null)
			{
				return FileOperationRules.Denied(ex.Message ?? "Access denied.");
			}
		}

		var refused = await AskAsync(sender, ct).ConfigureAwait(false);
		if (refused is not null)
		{
			return refused;
		}

		try
		{
			await Task.Run(write, ct).ConfigureAwait(false);
			return null;
		}
		catch (JavaSecurityException ex)
		{
			return FileOperationRules.Denied(ex.Message ?? "Access denied.");
		}
	}

	/// <summary>API 26-28. Needs the legacy write permission, asked at run time.</summary>
	private async Task<FileOperationResult?> RunLegacyAsync(Action write, CancellationToken ct)
	{
		var permission = new StorageWritePermission();
		var status = await MainThread.InvokeOnMainThreadAsync(permission.CheckStatusAsync).ConfigureAwait(false);
		if (status != PermissionStatus.Granted)
		{
			status = await MainThread.InvokeOnMainThreadAsync(permission.RequestAsync).ConfigureAwait(false);
		}

		if (status != PermissionStatus.Granted)
		{
			return FileOperationRules.Denied("The storage permission was not granted.");
		}

		try
		{
			await Task.Run(write, ct).ConfigureAwait(false);
			return null;
		}
		catch (JavaSecurityException ex)
		{
			return FileOperationRules.Denied(ex.Message ?? "Access denied.");
		}
	}

	private void UpdateName(AndroidUri uri, string name)
	{
		using var values = new ContentValues();
		values.Put("_display_name", name);
		if (Resolver.Update(uri, values, null, null) < 1)
		{
			throw new InvalidOperationException("The file is no longer in the media library.");
		}
	}

	private void DeleteRow(AndroidUri uri)
	{
		if (Resolver.Delete(uri, null, null) < 1)
		{
			throw new InvalidOperationException("The file is no longer in the media library.");
		}
	}

	// API 26-28: updating the row does not rename the file, so rename it and then fix the row.
	private void RenameLegacy(AndroidUri uri, string name)
	{
		var path = ReadString(uri, "_data") ?? throw new InvalidOperationException("The file path is unknown.");
		var source = new JavaFile(path);
		var target = new JavaFile(source.ParentFile, name);
		if (target.Exists())
		{
			throw new InvalidOperationException(FileOperationRules.NameTakenError);
		}

		if (!source.RenameTo(target))
		{
			throw new InvalidOperationException("The file could not be renamed.");
		}

		using var values = new ContentValues();
		values.Put("_display_name", name);
		values.Put("_data", target.AbsolutePath);
		Resolver.Update(uri, values, null, null);
	}

	private void DeleteLegacy(IReadOnlyList<MediaItem> items, List<AndroidUri> uris, List<MediaItem> removed)
	{
		for (var i = 0; i < uris.Count; i++)
		{
			var uri = uris[i];
			var path = ReadString(uri, "_data");
			if (path is not null)
			{
				var file = new JavaFile(path);
				if (file.Exists() && !file.Delete())
				{
					throw new InvalidOperationException("The file could not be deleted.");
				}
			}

			Resolver.Delete(uri, null, null);
			removed.Add(items[i]);
		}
	}

	private bool NameExists(MediaItem item, string name)
	{
		var uri = AndroidUri.Parse(item.ContentUri)!;
		if (!OperatingSystem.IsAndroidVersionAtLeast(29))
		{
			var path = ReadString(uri, "_data");
			var siblings = path is null ? null : new JavaFile(path).ParentFile?.List();
			return siblings is not null && siblings.Any(s => string.Equals(s, name, StringComparison.OrdinalIgnoreCase));
		}

		// The row itself tells the volume and folder; MediaStore.Files also finds files of another kind.
		string? volume;
		string? relativePath;
		using (var row = Resolver.Query(uri, ["volume_name", "relative_path"], null, null, null))
		{
			if (row is null || !row.MoveToFirst())
			{
				return false;
			}

			volume = row.IsNull(0) ? null : row.GetString(0);
			relativePath = row.IsNull(1) ? null : row.GetString(1);
		}

		if (volume is null || relativePath is null)
		{
			return false;
		}

		using var cursor = Resolver.Query(
			MediaStore.Files.GetContentUri(volume)!,
			["_id"],
			"relative_path = ? AND _display_name = ? COLLATE NOCASE AND _id != ?",
			[relativePath, name, item.Id.MediaStoreId.ToString(System.Globalization.CultureInfo.InvariantCulture)],
			null);
		return cursor is not null && cursor.MoveToFirst();
	}

	private string? ReadString(AndroidUri uri, string column)
	{
		using var cursor = Resolver.Query(uri, [column], null, null, null);
		return cursor is not null && cursor.MoveToFirst() && !cursor.IsNull(0) ? cursor.GetString(0) : null;
	}

	private MediaItem? ReadUpdated(MediaItem item, AndroidUri uri)
	{
		using var cursor = Resolver.Query(uri, ["_id", "_size", "date_modified", "_display_name", "mime_type"], null, null, null);
		if (cursor is null || !cursor.MoveToFirst())
		{
			return null;
		}

		return item with
		{
			Id = new MediaFileId(cursor.GetLong(0), cursor.GetLong(1), cursor.GetLong(2)),
			DisplayName = cursor.GetString(3) ?? item.DisplayName,
			MimeType = cursor.IsNull(4) ? item.MimeType : cursor.GetString(4) ?? item.MimeType,
		};
	}

	private async Task FollowIdentityAsync(MediaFileId before, MediaFileId after)
	{
		if (!FileOperationRules.IdentityChanged(before, after))
		{
			return;
		}

		var ct = CancellationToken.None;
		try
		{
			var state = await _stateStore.GetAsync(before, ct).ConfigureAwait(false);
			if (state is not null)
			{
				await _stateStore.SaveAsync(state with { Id = after }, ct).ConfigureAwait(false);
			}

			await _stateStore.DeleteAsync(before, ct).ConfigureAwait(false);
		}
		catch (Exception ex)
		{
			Log.Warn(Tag, $"Moving the playback state failed: {ex}");
		}
	}

	private async Task DeleteStateAsync(MediaFileId id)
	{
		try
		{
			await _stateStore.DeleteAsync(id, CancellationToken.None).ConfigureAwait(false);
		}
		catch (Exception ex)
		{
			Log.Warn(Tag, $"Deleting the playback state failed: {ex}");
		}
	}

	private sealed class StorageWritePermission : Permissions.BasePlatformPermission
	{
		public override (string androidPermission, bool isRuntime)[] RequiredPermissions =>
			[(global::Android.Manifest.Permission.WriteExternalStorage, true)];
	}
}
