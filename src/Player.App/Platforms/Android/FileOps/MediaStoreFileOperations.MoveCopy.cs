using System.Globalization;
using System.Runtime.Versioning;
using Android.Content;
using Android.Provider;
using Android.Util;
using Player.Core.Contracts;
using Player.Core.Models;
using AndroidUri = Android.Net.Uri;
using JavaFile = Java.IO.File;
using JavaSecurityException = Java.Lang.SecurityException;

namespace Player.App.FileOps;

// Move, copy and the subtitles that follow the files. See docs/agents/file-ops.md (R7-30).
public sealed partial class MediaStoreFileOperations
{
	private const int CopyBufferSize = 1024 * 1024;

	/// <summary>The target folder as the media store knows it. Volume and RelativePath on API 29+, Directory below.</summary>
	private sealed record TargetFolder(string? Volume, string? RelativePath, string? Directory);

	private enum Step { Done, NeedsConsent, Failed }

	private sealed class MoveUnit(MediaItem? item, AndroidUri uri, string name)
	{
		public MediaItem? Item { get; } = item;

		public AndroidUri Uri { get; } = uri;

		public string Name { get; } = name;

		public List<MoveUnit> Subtitles { get; } = [];

		public IntentSender? Sender { get; set; }

		public string? Error { get; set; }
	}

	private sealed class BatchRun
	{
		public int Done { get; set; }

		public int Total { get; init; }

		public string? FirstError { get; private set; }

		public List<(MediaItem Item, AndroidUri Uri)> Moved { get; } = [];

		public void Fail(string error) => FirstError ??= error;
	}

	// ---- shared ----

	private TargetFolder? ResolveTarget(long bucketId)
	{
		var files = MediaStore.Files.GetContentUri("external")!;
		var args = new[] { bucketId.ToString(CultureInfo.InvariantCulture) };
		if (OperatingSystem.IsAndroidVersionAtLeast(29))
		{
			using var row = Resolver.Query(files, ["volume_name", "relative_path"], "bucket_id = ?", args, null);
			if (row is null || !row.MoveToFirst() || row.IsNull(0) || row.IsNull(1))
			{
				return null;
			}

			return new TargetFolder(row.GetString(0), row.GetString(1), null);
		}

		using var legacy = Resolver.Query(files, ["_data"], "bucket_id = ?", args, null);
		if (legacy is null || !legacy.MoveToFirst() || legacy.IsNull(0))
		{
			return null;
		}

		var directory = new JavaFile(legacy.GetString(0)!).ParentFile?.AbsolutePath;
		return directory is null ? null : new TargetFolder(null, null, directory);
	}

	private bool NameExistsAt(TargetFolder target, string name, long excludeId)
	{
		if (target.Directory is not null)
		{
			var siblings = new JavaFile(target.Directory).List();
			return siblings is not null && siblings.Any(s => string.Equals(s, name, StringComparison.OrdinalIgnoreCase));
		}

		using var cursor = Resolver.Query(
			MediaStore.Files.GetContentUri(target.Volume!)!,
			["_id"],
			"relative_path = ? AND _display_name = ? COLLATE NOCASE AND _id != ?",
			[target.RelativePath!, name, excludeId.ToString(CultureInfo.InvariantCulture)],
			null);
		return cursor is not null && cursor.MoveToFirst();
	}

	/// <summary>Subtitles per item (by id), best effort, each file once, never one of the selected files. Never throws.</summary>
	private async Task<Dictionary<MediaFileId, IReadOnlyList<SubtitleFile>>> CollectSubtitlesAsync(IReadOnlyList<MediaItem> items, CancellationToken ct)
	{
		var result = new Dictionary<MediaFileId, IReadOnlyList<SubtitleFile>>();
		var seen = new HashSet<string>(items.Select(i => i.ContentUri), StringComparer.Ordinal);
		foreach (var item in items)
		{
			if (ct.IsCancellationRequested)
			{
				break;
			}

			try
			{
				var found = await _subtitles.FindSidecarsAsync(item, ct).ConfigureAwait(false);
				result[item.Id] = found.Where(s => seen.Add(s.ContentUri)).ToList();
			}
			catch (Exception ex)
			{
				Log.Warn(Tag, $"Finding subtitles failed: {ex.Message}");
			}
		}

		return result;
	}

	private async Task DeleteSidecarsAsync(List<MediaItem> removed, Dictionary<MediaFileId, IReadOnlyList<SubtitleFile>> subtitles)
	{
		try
		{
			var uris = removed.Where(i => subtitles.ContainsKey(i.Id)).SelectMany(i => subtitles[i.Id]).Select(s => s.ContentUri).ToList();
			await Task.Run(() =>
			{
				foreach (var text in uris)
				{
					try
					{
						var uri = AndroidUri.Parse(text)!;
						if (!OperatingSystem.IsAndroidVersionAtLeast(29) && ReadString(uri, "_data") is { } path)
						{
							var file = new JavaFile(path);
							if (file.Exists())
							{
								file.Delete();
							}
						}

						Resolver.Delete(uri, null, null);
					}
					catch (Exception ex)
					{
						Log.Warn(Tag, $"A subtitle was not deleted: {ex.Message}");
					}
				}
			}).ConfigureAwait(false);
		}
		catch (Exception ex)
		{
			Log.Warn(Tag, $"Deleting subtitles failed: {ex.Message}");
		}
	}

	// ---- move ----

	public async Task<FileOperationResult> MoveAsync(IReadOnlyList<MediaItem> items, MediaFolder target, CancellationToken ct)
	{
		ArgumentNullException.ThrowIfNull(items);
		ArgumentNullException.ThrowIfNull(target);
		var run = new BatchRun { Total = items.Count };
		try
		{
			if (items.Count == 0)
			{
				return new FileOperationResult(FileOperationStatus.Success, null, null);
			}

			var folder = await Task.Run(() => ResolveTarget(target.BucketId), ct).ConfigureAwait(false);
			if (folder is null)
			{
				return FileOperationRules.Failed(FileOperationRules.TargetMissingError);
			}

			var open = items.Where(i => i.BucketId != target.BucketId).ToList();
			run.Done = items.Count - open.Count;
			var subtitles = await CollectSubtitlesAsync(open, ct).ConfigureAwait(false);
			var units = open.Select(i =>
			{
				var unit = new MoveUnit(i, AndroidUri.Parse(i.ContentUri)!, i.DisplayName);
				if (subtitles.TryGetValue(i.Id, out var found))
				{
					unit.Subtitles.AddRange(found.Select(s => new MoveUnit(null, AndroidUri.Parse(s.ContentUri)!, s.DisplayName)));
				}

				return unit;
			}).ToList();

			FileOperationResult? refused = null;
			if (units.Count > 0)
			{
				refused = OperatingSystem.IsAndroidVersionAtLeast(29)
					? await MoveScopedAsync(units, folder, run, ct).ConfigureAwait(false)
					: await RunLegacyAsync(() => MoveLegacy(units, folder, run, ct), ct).ConfigureAwait(false);
			}

			await FinishMovedAsync(run).ConfigureAwait(false);
			return refused is not null
				? FileOperationRules.WithCount(refused, run.Done)
				: FileOperationRules.BatchResult(run.Done, run.Total, run.FirstError, "moved");
		}
		catch (OperationCanceledException)
		{
			await FinishMovedAsync(run).ConfigureAwait(false);
			return FileOperationRules.WithCount(FileOperationRules.Cancelled(), run.Done);
		}
		catch (Exception ex)
		{
			Log.Warn(Tag, $"Move failed: {ex}");
			await FinishMovedAsync(run).ConfigureAwait(false);
			return FileOperationRules.WithCount(FileOperationRules.Failed(ex.Message), run.Done);
		}
	}

	/// <summary>API 29+. Tries everything first; what the app may not write is asked once (API 30+) or per file (API 29).</summary>
	private async Task<FileOperationResult?> MoveScopedAsync(List<MoveUnit> units, TargetFolder folder, BatchRun run, CancellationToken ct)
	{
		var pending = new List<MoveUnit>();
		var pendingSubtitles = new List<MoveUnit>();
		await Task.Run(() =>
		{
			foreach (var unit in units)
			{
				ct.ThrowIfCancellationRequested();
				switch (TryMove(unit, folder))
				{
					case Step.Done:
						Record(unit, run);
						MoveSubtitles(unit, folder, pendingSubtitles);
						break;
					case Step.NeedsConsent:
						pending.Add(unit);
						break;
					default:
						run.Fail($"{unit.Name}: {unit.Error}");
						break;
				}
			}
		}, ct).ConfigureAwait(false);

		var retry = pending.Concat(pendingSubtitles).ToList();
		if (retry.Count == 0)
		{
			return null;
		}

		if (OperatingSystem.IsAndroidVersionAtLeast(30))
		{
			var uris = retry.Concat(pending.SelectMany(p => p.Subtitles)).Select(u => u.Uri).DistinctBy(u => u.ToString()).ToList();
			IntentSender sender;
			try
			{
				sender = await Task.Run(() => OperatingSystem.IsAndroidVersionAtLeast(30) ? WriteRequest(uris) : null!, ct).ConfigureAwait(false);
			}
			catch (Exception ex) when (ex is not OperationCanceledException)
			{
				Log.Warn(Tag, $"The write request with subtitles was refused, trying without them: {ex.Message}");
				var onlyItems = pending.Select(u => u.Uri).ToList();
				sender = await Task.Run(() => OperatingSystem.IsAndroidVersionAtLeast(30) ? WriteRequest(onlyItems) : null!, ct).ConfigureAwait(false);
			}

			var refused = await AskAsync(sender, ct).ConfigureAwait(false);
			if (refused is not null)
			{
				return refused;
			}
		}

		foreach (var unit in retry)
		{
			if (!OperatingSystem.IsAndroidVersionAtLeast(30))
			{
				if (unit.Sender is null)
				{
					FailOrLog(unit, run, FileOperationRules.AccessDeniedError);
					continue;
				}

				var refused = await AskAsync(unit.Sender, ct).ConfigureAwait(false);
				if (refused is not null)
				{
					return refused;
				}
			}

			await Task.Run(() =>
			{
				ct.ThrowIfCancellationRequested();
				var step = TryMove(unit, folder);
				if (step == Step.Done)
				{
					Record(unit, run);
					MoveSubtitles(unit, folder, null);
				}
				else
				{
					FailOrLog(unit, run, step == Step.NeedsConsent ? FileOperationRules.AccessDeniedError : unit.Error!);
				}
			}, ct).ConfigureAwait(false);
		}

		return null;
	}

	private static void FailOrLog(MoveUnit unit, BatchRun run, string error)
	{
		if (unit.Item is null)
		{
			Log.Warn(Tag, $"Subtitle {unit.Name} was not handled: {error}");
		}
		else
		{
			run.Fail($"{unit.Name}: {error}");
		}
	}

	private static void Record(MoveUnit unit, BatchRun run)
	{
		if (unit.Item is not null)
		{
			run.Done++;
			run.Moved.Add((unit.Item, unit.Uri));
		}
	}

	// Subtitles never fail the move: a refusal that needs consent is queued when a queue is given.
	private void MoveSubtitles(MoveUnit unit, TargetFolder folder, List<MoveUnit>? queue)
	{
		foreach (var sub in unit.Subtitles)
		{
			var step = TryMove(sub, folder);
			if (step == Step.NeedsConsent && queue is not null)
			{
				queue.Add(sub);
			}
			else if (step != Step.Done)
			{
				Log.Warn(Tag, $"Subtitle {sub.Name} was not moved: {sub.Error ?? "no permission"}");
			}
		}
	}

	private Step TryMove(MoveUnit unit, TargetFolder folder)
	{
		try
		{
			var volume = ReadString(unit.Uri, "volume_name");
			if (volume is not null && !string.Equals(volume, folder.Volume, StringComparison.OrdinalIgnoreCase))
			{
				unit.Error = FileOperationRules.OtherVolumeError;
				return Step.Failed;
			}

			if (NameExistsAt(folder, unit.Name, ContentUris.ParseId(unit.Uri)))
			{
				unit.Error = FileOperationRules.TargetNameTakenError;
				return Step.Failed;
			}

			using var values = new ContentValues();
			values.Put("relative_path", folder.RelativePath);
			if (Resolver.Update(unit.Uri, values, null, null) < 1)
			{
				throw new InvalidOperationException("The file is no longer in the media library.");
			}

			return Step.Done;
		}
		catch (JavaSecurityException ex)
		{
			unit.Sender = OperatingSystem.IsAndroidVersionAtLeast(29) ? RecoverableSender(ex) : null;
			return Step.NeedsConsent;
		}
		catch (Exception ex) when (ex is not OperationCanceledException)
		{
			Log.Warn(Tag, $"Move of {unit.Name} failed: {ex.Message}");
			unit.Error = ex is Java.Lang.IllegalArgumentException or Java.Lang.IllegalStateException
				? FileOperationRules.RefusedKindError
				: ex.Message;
			return Step.Failed;
		}
	}

	/// <summary>API 26-28: rename the file into the target directory and fix the row.</summary>
	private void MoveLegacy(List<MoveUnit> units, TargetFolder folder, BatchRun run, CancellationToken ct)
	{
		foreach (var unit in units)
		{
			ct.ThrowIfCancellationRequested();
			if (TryMoveLegacy(unit, folder))
			{
				Record(unit, run);
				foreach (var sub in unit.Subtitles)
				{
					if (!TryMoveLegacy(sub, folder))
					{
						Log.Warn(Tag, $"Subtitle {sub.Name} was not moved: {sub.Error}");
					}
				}
			}
			else
			{
				run.Fail($"{unit.Name}: {unit.Error}");
			}
		}
	}

	private bool TryMoveLegacy(MoveUnit unit, TargetFolder folder)
	{
		try
		{
			var path = ReadString(unit.Uri, "_data") ?? throw new InvalidOperationException("The file path is unknown.");
			var destination = new JavaFile(folder.Directory!, unit.Name);
			if (destination.Exists())
			{
				unit.Error = FileOperationRules.TargetNameTakenError;
				return false;
			}

			if (!new JavaFile(path).RenameTo(destination))
			{
				unit.Error = "The file could not be moved.";
				return false;
			}

			using var values = new ContentValues();
			values.Put("_data", destination.AbsolutePath);
			Resolver.Update(unit.Uri, values, null, null);
			return true;
		}
		catch (Exception ex) when (ex is not OperationCanceledException && ex is not JavaSecurityException)
		{
			unit.Error = ex.Message;
			return false;
		}
	}

	// Committed moves: read the row back and carry the saved state to the new identity. No caller token, no throw.
	private async Task FinishMovedAsync(BatchRun run)
	{
		foreach (var (item, uri) in run.Moved)
		{
			try
			{
				var updated = await Task.Run(() => ReadUpdated(item, uri)).ConfigureAwait(false);
				if (updated is not null)
				{
					await FollowIdentityAsync(item.Id, updated.Id).ConfigureAwait(false);
				}
			}
			catch (Exception ex)
			{
				Log.Warn(Tag, $"Following a moved file failed: {ex.Message}");
			}
		}

		run.Moved.Clear();
	}

	// ---- copy ----

	public async Task<FileOperationResult> CopyAsync(IReadOnlyList<MediaItem> items, MediaFolder target, CancellationToken ct)
	{
		ArgumentNullException.ThrowIfNull(items);
		ArgumentNullException.ThrowIfNull(target);
		var run = new BatchRun { Total = items.Count };
		try
		{
			if (items.Count == 0)
			{
				return new FileOperationResult(FileOperationStatus.Success, null, null);
			}

			var folder = await Task.Run(() => ResolveTarget(target.BucketId), ct).ConfigureAwait(false);
			if (folder is null)
			{
				return FileOperationRules.Failed(FileOperationRules.TargetMissingError);
			}

			var subtitles = await CollectSubtitlesAsync(items, ct).ConfigureAwait(false);
			if (OperatingSystem.IsAndroidVersionAtLeast(29))
			{
				await Task.Run(() => CopyAll(items, subtitles, folder, run, ct), ct).ConfigureAwait(false);
				return FileOperationRules.BatchResult(run.Done, run.Total, run.FirstError, "copied");
			}

			var denied = await RunLegacyAsync(() => CopyAllLegacy(items, subtitles, folder, run, ct), ct).ConfigureAwait(false);
			return denied is not null
				? FileOperationRules.WithCount(denied, run.Done)
				: FileOperationRules.BatchResult(run.Done, run.Total, run.FirstError, "copied");
		}
		catch (OperationCanceledException)
		{
			return FileOperationRules.WithCount(FileOperationRules.Cancelled(), run.Done);
		}
		catch (Exception ex)
		{
			Log.Warn(Tag, $"Copy failed: {ex}");
			return FileOperationRules.WithCount(FileOperationRules.Failed(ex.Message), run.Done);
		}
	}

	private void CopyAll(IReadOnlyList<MediaItem> items, Dictionary<MediaFileId, IReadOnlyList<SubtitleFile>> subtitles, TargetFolder folder, BatchRun run, CancellationToken ct)
	{
		if (!OperatingSystem.IsAndroidVersionAtLeast(29))
		{
			return;
		}

		foreach (var item in items)
		{
			ct.ThrowIfCancellationRequested();
			var name = FileOperationRules.FreeName(item.DisplayName, n => NameExistsAt(folder, n, -1));
			var error = CopyRow(item.Kind == MediaKind.Video ? VideoCollection(folder) : AudioCollection(folder), AndroidUri.Parse(item.ContentUri)!, name, item.MimeType, folder, ct);
			if (error is not null)
			{
				run.Fail($"{item.DisplayName}: {error}");
				continue;
			}

			run.Done++;
			if (!subtitles.TryGetValue(item.Id, out var found))
			{
				continue;
			}

			foreach (var sub in found)
			{
				ct.ThrowIfCancellationRequested();
				var subName = FileOperationRules.SidecarName(item.DisplayName, name, sub.DisplayName);
				subName = FileOperationRules.FreeName(subName, n => NameExistsAt(folder, n, -1));
				var uri = AndroidUri.Parse(sub.ContentUri)!;
				var mime = ReadString(uri, "mime_type") ?? "application/octet-stream";
				var problem = CopyRow(MediaStore.Files.GetContentUri(folder.Volume!)!, uri, subName, mime, folder, ct);
				if (problem is not null)
				{
					Log.Warn(Tag, $"Subtitle {sub.DisplayName} was not copied: {problem}");
				}
			}
		}
	}

	[SupportedOSPlatform("android29.0")]
	private static AndroidUri VideoCollection(TargetFolder folder) => MediaStore.Video.Media.GetContentUri(folder.Volume!)!;

	[SupportedOSPlatform("android29.0")]
	private static AndroidUri AudioCollection(TargetFolder folder) => MediaStore.Audio.Media.GetContentUri(folder.Volume!)!;

	/// <summary>Inserts a pending row, copies the bytes, clears the pending flag. Null on success, else the error text. A cancel deletes the partial row and throws.</summary>
	private string? CopyRow(AndroidUri collection, AndroidUri source, string name, string mime, TargetFolder folder, CancellationToken ct)
	{
		AndroidUri? created = null;
		try
		{
			using var values = new ContentValues();
			values.Put("relative_path", folder.RelativePath);
			values.Put("_display_name", name);
			values.Put("mime_type", mime);
			values.Put("is_pending", 1);
			created = Resolver.Insert(collection, values);
			if (created is null)
			{
				return FileOperationRules.CopyFailedError;
			}

			using (var input = Resolver.OpenInputStream(source) ?? throw new InvalidOperationException("The source file cannot be read."))
			using (var output = Resolver.OpenOutputStream(created) ?? throw new InvalidOperationException("The new file cannot be written."))
			{
				CopyStream(input, output, ct);
			}

			using var done = new ContentValues();
			done.Put("is_pending", 0);
			Resolver.Update(created, done, null, null);
			created = null;
			return null;
		}
		catch (Exception ex) when (ex is not OperationCanceledException)
		{
			Log.Warn(Tag, $"Copy of {name} failed: {ex.Message}");
			return ex is Java.Lang.IllegalArgumentException or Java.Lang.IllegalStateException
				? FileOperationRules.RefusedKindError
				: FileOperationRules.CopyFailedError;
		}
		finally
		{
			if (created is not null)
			{
				try
				{
					Resolver.Delete(created, null, null);
				}
				catch (Exception ex)
				{
					Log.Warn(Tag, $"The partial copy could not be removed: {ex.Message}");
				}
			}
		}
	}

	private static void CopyStream(Stream input, Stream output, CancellationToken ct)
	{
		var buffer = new byte[CopyBufferSize];
		int read;
		while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
		{
			ct.ThrowIfCancellationRequested();
			output.Write(buffer, 0, read);
		}

		output.Flush();
	}

	/// <summary>API 26-28: copy with streams into the directory and scan the new file.</summary>
	private void CopyAllLegacy(IReadOnlyList<MediaItem> items, Dictionary<MediaFileId, IReadOnlyList<SubtitleFile>> subtitles, TargetFolder folder, BatchRun run, CancellationToken ct)
	{
		foreach (var item in items)
		{
			ct.ThrowIfCancellationRequested();
			var name = FileOperationRules.FreeName(item.DisplayName, n => NameExistsAt(folder, n, -1));
			var error = CopyFileLegacy(AndroidUri.Parse(item.ContentUri)!, name, item.MimeType, folder, ct);
			if (error is not null)
			{
				run.Fail($"{item.DisplayName}: {error}");
				continue;
			}

			run.Done++;
			foreach (var sub in subtitles.TryGetValue(item.Id, out var found) ? found : [])
			{
				ct.ThrowIfCancellationRequested();
				var subName = FileOperationRules.SidecarName(item.DisplayName, name, sub.DisplayName);
				subName = FileOperationRules.FreeName(subName, n => NameExistsAt(folder, n, -1));
				var problem = CopyFileLegacy(AndroidUri.Parse(sub.ContentUri)!, subName, null, folder, ct);
				if (problem is not null)
				{
					Log.Warn(Tag, $"Subtitle {sub.DisplayName} was not copied: {problem}");
				}
			}
		}
	}

	private string? CopyFileLegacy(AndroidUri source, string name, string? mime, TargetFolder folder, CancellationToken ct)
	{
		var path = Path.Combine(folder.Directory!, name);
		var complete = false;
		try
		{
			using (var input = Resolver.OpenInputStream(source) ?? throw new InvalidOperationException("The source file cannot be read."))
			using (var output = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, CopyBufferSize))
			{
				CopyStream(input, output, ct);
			}

			complete = true;
			global::Android.Media.MediaScannerConnection.ScanFile(_context, [path], mime is null ? null : [mime], null);
			return null;
		}
		catch (Exception ex) when (ex is not OperationCanceledException)
		{
			Log.Warn(Tag, $"Copy of {name} failed: {ex.Message}");
			return FileOperationRules.CopyFailedError;
		}
		finally
		{
			if (!complete)
			{
				try
				{
					File.Delete(path);
				}
				catch (Exception ex)
				{
					Log.Warn(Tag, $"The partial copy could not be removed: {ex.Message}");
				}
			}
		}
	}
}
