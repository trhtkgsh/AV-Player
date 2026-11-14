using Android.Content;
using Android.Database;
using Android.OS;
using Android.Provider;
using Player.Core.Contracts;
using Player.Core.Logic.Library;
using Player.Core.Models;
using AndroidUri = Android.Net.Uri;

namespace Player.App.Platforms.Android.Library;

/// <summary>IMediaLibrary backed by MediaStore. Items are identified by MediaFileId and opened by content URI, never by path.</summary>
public sealed class MediaStoreLibrary : IMediaLibrary, IDisposable
{
	private static readonly TimeSpan ChangedInterval = TimeSpan.FromSeconds(1);

	private readonly IPlaybackStateStore _stateStore;
	private readonly Context _context;
	private readonly MediaChangeObserver _observer;
	private readonly object _changedLock = new();
	private Timer? _changedTimer;

	public MediaStoreLibrary(IPlaybackStateStore stateStore)
	{
		_stateStore = stateStore ?? throw new ArgumentNullException(nameof(stateStore));
		_context = global::Android.App.Application.Context;
		_observer = new MediaChangeObserver(OnStoreChanged);
		_context.ContentResolver!.RegisterContentObserver(MediaStore.Video.Media.ExternalContentUri!, true, _observer);
		_context.ContentResolver.RegisterContentObserver(MediaStore.Audio.Media.ExternalContentUri!, true, _observer);
	}

	public event EventHandler? Changed;

	public async Task<bool> EnsureReadPermissionAsync(CancellationToken ct)
	{
		ct.ThrowIfCancellationRequested();
		var permission = new MediaReadPermission();
		var status = await permission.CheckStatusAsync().WaitAsync(ct).ConfigureAwait(false);
		if (status != PermissionStatus.Granted)
		{
			status = await MainThread.InvokeOnMainThreadAsync(permission.RequestAsync).WaitAsync(ct).ConfigureAwait(false);
		}

		return status == PermissionStatus.Granted || HasPartialAccess();
	}

	public async Task<IReadOnlyList<MediaFolder>> GetFoldersAsync(int newTagDays, CancellationToken ct)
	{
		var items = await GetItemsAsync(new MediaQuery(null, null), ct).ConfigureAwait(false);
		var states = await _stateStore.GetManyAsync(items.Select(i => i.Id).ToList(), ct).ConfigureAwait(false);
		return FolderGrouper.Group(items, states, newTagDays, DateTimeOffset.UtcNow.ToUnixTimeSeconds());
	}

	public Task<IReadOnlyList<MediaItem>> GetItemsAsync(MediaQuery query, CancellationToken ct)
	{
		ArgumentNullException.ThrowIfNull(query);
		return Task.Run<IReadOnlyList<MediaItem>>(() =>
		{
			var items = new List<MediaItem>();
			if (query.Kind is null or MediaKind.Video)
			{
				Query(MediaStore.Video.Media.ExternalContentUri!, MediaKind.Video, query.BucketId, items, ct);
			}

			if (query.Kind is null or MediaKind.Audio)
			{
				Query(MediaStore.Audio.Media.ExternalContentUri!, MediaKind.Audio, query.BucketId, items, ct);
			}

			return items;
		}, ct);
	}

	public void Dispose()
	{
		_context.ContentResolver?.UnregisterContentObserver(_observer);
		_observer.Dispose();
		lock (_changedLock)
		{
			_changedTimer?.Dispose();
			_changedTimer = null;
		}
	}

	private void Query(AndroidUri collection, MediaKind kind, long? bucketId, List<MediaItem> items, CancellationToken ct)
	{
		var modern = OperatingSystem.IsAndroidVersionAtLeast(29);
		var projection = new List<string>
		{
			Columns.Id, Columns.Size, Columns.DateModified, Columns.DateAdded, Columns.DisplayName, Columns.MimeType,
		};
		projection.AddRange(modern
			? [Columns.BucketId, Columns.BucketDisplayName, Columns.RelativePath]
			: [Columns.Data, Columns.BucketId]);

		// IS_PENDING exists from API 29, IS_TRASHED from API 30.
		string? selection = null;
		if (OperatingSystem.IsAndroidVersionAtLeast(30))
		{
			selection = $"{Columns.IsPending} = 0 AND {Columns.IsTrashed} = 0";
		}
		else if (modern)
		{
			selection = $"{Columns.IsPending} = 0";
		}

		using var cursor = _context.ContentResolver!.Query(collection, projection.ToArray(), selection, null, null);
		if (cursor is null)
		{
			return;
		}

		var col = projection.ToDictionary(name => name, name => cursor.GetColumnIndexOrThrow(name));
		while (cursor.MoveToNext())
		{
			ct.ThrowIfCancellationRequested();

			string folderName;
			string relativePath;
			long bucket;
			if (modern)
			{
				bucket = cursor.GetLong(col[Columns.BucketId]);
				folderName = GetString(cursor, col[Columns.BucketDisplayName]);
				relativePath = GetString(cursor, col[Columns.RelativePath]);
			}
			else
			{
				// API 26-28: no RELATIVE_PATH, and audio rows have no bucket. DATA is used only to derive display text and a grouping key.
				var directory = Path.GetDirectoryName(GetString(cursor, col[Columns.Data])) ?? string.Empty;
				relativePath = directory;
				folderName = Path.GetFileName(directory);
				bucket = kind == MediaKind.Video ? cursor.GetLong(col[Columns.BucketId]) : JavaHash(directory.ToLowerInvariant());
			}

			if (bucketId is not null && bucket != bucketId)
			{
				continue;
			}

			var id = cursor.GetLong(col[Columns.Id]);
			items.Add(new MediaItem(
				new MediaFileId(id, cursor.GetLong(col[Columns.Size]), cursor.GetLong(col[Columns.DateModified])),
				ContentUris.WithAppendedId(collection, id)!.ToString()!,
				GetString(cursor, col[Columns.DisplayName]),
				kind,
				bucket,
				folderName,
				relativePath,
				cursor.GetLong(col[Columns.DateAdded]),
				GetString(cursor, col[Columns.MimeType])));
		}
	}

	private static string GetString(ICursor cursor, int index) => cursor.IsNull(index) ? string.Empty : cursor.GetString(index) ?? string.Empty;

	// Same value Android uses for BUCKET_ID: String.hashCode of the lower-cased directory.
	private static long JavaHash(string value)
	{
		var hash = 0;
		foreach (var c in value)
		{
			hash = unchecked((31 * hash) + c);
		}

		return hash;
	}

	// API 34 "selected media" access: only the user-selected permission is granted.
	private bool HasPartialAccess() =>
		OperatingSystem.IsAndroidVersionAtLeast(34)
		&& _context.CheckSelfPermission("android.permission.READ_MEDIA_VISUAL_USER_SELECTED") == global::Android.Content.PM.Permission.Granted;

	// Coalesces bursts: the first change arms a one-second timer and Changed fires once when it elapses.
	private void OnStoreChanged()
	{
		lock (_changedLock)
		{
			_changedTimer ??= new Timer(_ =>
			{
				lock (_changedLock)
				{
					_changedTimer?.Dispose();
					_changedTimer = null;
				}

				Changed?.Invoke(this, EventArgs.Empty);
			}, null, ChangedInterval, Timeout.InfiniteTimeSpan);
		}
	}

	private static class Columns
	{
		public const string Id = "_id";
		public const string Size = "_size";
		public const string DateModified = "date_modified";
		public const string DateAdded = "date_added";
		public const string DisplayName = "_display_name";
		public const string MimeType = "mime_type";
		public const string BucketId = "bucket_id";
		public const string BucketDisplayName = "bucket_display_name";
		public const string RelativePath = "relative_path";
		public const string Data = "_data";
		public const string IsPending = "is_pending";
		public const string IsTrashed = "is_trashed";
	}

	private sealed class MediaChangeObserver : ContentObserver
	{
		private readonly Action _onChange;

		public MediaChangeObserver(Action onChange)
			: base(new Handler(Looper.MainLooper!))
		{
			_onChange = onChange;
		}

		public override void OnChange(bool selfChange) => _onChange();
	}
}
