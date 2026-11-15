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
public sealed class MediaStoreFileOperations : IFileOperations
{
	private const string Tag = "FileOps";

	private readonly IPlaybackStateStore _stateStore;
	private readonly ConsentLauncher _consent;
	private readonly Context _context;

	public MediaStoreFileOperations(IPlaybackStateStore stateStore, ConsentLauncher consent)
	{
		_stateStore = stateStore ?? throw new ArgumentNullException(nameof(stateStore));
		_consent = consent ?? throw new ArgumentNullException(nameof(consent));
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

			var updated = await Task.Run(() => ReadUpdated(item, uri), ct).ConfigureAwait(false);
			if (updated is null)
			{
				return FileOperationRules.Failed("The renamed file could not be read back.");
			}

			await FollowIdentityAsync(item.Id, updated.Id, ct).ConfigureAwait(false);
			return new FileOperationResult(FileOperationStatus.Success, updated, null);
		}
		catch (OperationCanceledException)
		{
			return FileOperationRules.Cancelled();
		}
		catch (Exception ex)
		{
			Log.Warn(Tag, $"Rename failed: {ex}");
			return FileOperationRules.Failed(ex.Message);
		}
	}

	public async Task<FileOperationResult> TrashAsync(IReadOnlyList<MediaItem> items, CancellationToken ct)
	{
		ArgumentNullException.ThrowIfNull(items);
		if (items.Count == 0)
		{
			return new FileOperationResult(FileOperationStatus.Success, null, null);
		}

		try
		{
			var uris = items.Select(i => AndroidUri.Parse(i.ContentUri)!).ToList();
			FileOperationResult? failure;
			if (OperatingSystem.IsAndroidVersionAtLeast(30))
			{
				failure = await AskAsync(TrashRequest(uris), ct).ConfigureAwait(false);
			}
			else if (OperatingSystem.IsAndroidVersionAtLeast(29))
			{
				failure = null;
				foreach (var uri in uris)
				{
					failure = await RunWithConsentAsync(() => DeleteRow(uri), [uri], ct).ConfigureAwait(false);
					if (failure is not null)
					{
						break;
					}
				}
			}
			else
			{
				failure = await RunLegacyAsync(() => DeleteLegacy(uris), ct).ConfigureAwait(false);
			}

			if (failure is not null)
			{
				return failure;
			}

			foreach (var item in items)
			{
				await DeleteStateAsync(item.Id, ct).ConfigureAwait(false);
			}

			return new FileOperationResult(FileOperationStatus.Success, null, null);
		}
		catch (OperationCanceledException)
		{
			return FileOperationRules.Cancelled();
		}
		catch (Exception ex)
		{
			Log.Warn(Tag, $"Trash failed: {ex}");
			return FileOperationRules.Failed(ex.Message);
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

	private void DeleteLegacy(List<AndroidUri> uris)
	{
		foreach (var uri in uris)
		{
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
		}
	}

	private bool NameExists(MediaItem item, string name)
	{
		var uri = AndroidUri.Parse(item.ContentUri)!;
		if (!OperatingSystem.IsAndroidVersionAtLeast(29))
		{
			var path = ReadString(uri, "_data");
			return path is not null && new JavaFile(new JavaFile(path).ParentFile, name).Exists();
		}

		var collection = item.Kind == MediaKind.Video ? MediaStore.Video.Media.ExternalContentUri! : MediaStore.Audio.Media.ExternalContentUri!;
		using var cursor = Resolver.Query(
			collection,
			["_id"],
			"relative_path = ? AND _display_name = ? AND _id != ?",
			[item.RelativePath, name, item.Id.MediaStoreId.ToString(System.Globalization.CultureInfo.InvariantCulture)],
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

	private async Task FollowIdentityAsync(MediaFileId before, MediaFileId after, CancellationToken ct)
	{
		if (!FileOperationRules.IdentityChanged(before, after))
		{
			return;
		}

		try
		{
			var state = await _stateStore.GetAsync(before, ct).ConfigureAwait(false);
			if (state is not null)
			{
				await _stateStore.SaveAsync(state with { Id = after }, ct).ConfigureAwait(false);
			}

			await _stateStore.DeleteAsync(before, ct).ConfigureAwait(false);
		}
		catch (Exception ex) when (ex is not OperationCanceledException)
		{
			Log.Warn(Tag, $"Moving the playback state failed: {ex}");
		}
	}

	private async Task DeleteStateAsync(MediaFileId id, CancellationToken ct)
	{
		try
		{
			await _stateStore.DeleteAsync(id, ct).ConfigureAwait(false);
		}
		catch (Exception ex) when (ex is not OperationCanceledException)
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
