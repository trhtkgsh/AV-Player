using Android.Graphics;
using Android.Media;
using Player.Core.Models;
using Uri = Android.Net.Uri;

namespace Player.App.Platforms.Android.Thumbnails;

public sealed record ExtractedMedia(long DurationMs, int Width, int Height, string? ThumbnailPath);

/// <summary>Reads duration, size and one JPEG frame from a media item. Opens it by content URI only.</summary>
public sealed class AndroidMetadataExtractor
{
    private const int MaxThumbWidth = 320;
    private const int JpegQuality = 80;

    public static string ThumbnailFileName(MediaFileId id) => id.ToKey().Replace(':', '_') + ".jpg";

    public Task<ExtractedMedia> ExtractAsync(MediaItem item, string thumbsDir, CancellationToken ct) =>
        Task.Run(() => Extract(item, thumbsDir, ct), ct);

    private static ExtractedMedia Extract(MediaItem item, string thumbsDir, CancellationToken ct)
    {
        var context = global::Android.App.Application.Context;
        var uri = Uri.Parse(item.ContentUri) ?? throw new ArgumentException("Invalid content URI.");

        using var retriever = new MediaMetadataRetriever();
        try
        {
            using (var pfd = context.ContentResolver!.OpenFileDescriptor(uri, "r")
                ?? throw new IOException("Cannot open " + item.ContentUri))
            {
                retriever.SetDataSource(pfd.FileDescriptor);
            }
            ct.ThrowIfCancellationRequested();

            var durationText = retriever.ExtractMetadata(MetadataKey.Duration);
            var widthText = retriever.ExtractMetadata(MetadataKey.VideoWidth);
            var heightText = retriever.ExtractMetadata(MetadataKey.VideoHeight);
            var rotationText = retriever.ExtractMetadata(MetadataKey.VideoRotation);
            if (durationText is null && widthText is null)
                throw new InvalidDataException("No readable media metadata.");

            long.TryParse(durationText, out var durationMs);
            int.TryParse(widthText, out var width);
            int.TryParse(heightText, out var height);
            if (int.TryParse(rotationText, out var rotation) && rotation is 90 or 270)
                (width, height) = (height, width);

            string? thumbPath = null;
            try
            {
                thumbPath = SaveThumbnail(item, uri, retriever, durationMs, width, height, thumbsDir, ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // A missing frame is not a failure: the row keeps its metadata and has no thumbnail.
            }
            return new ExtractedMedia(durationMs, width, height, thumbPath);
        }
        finally
        {
            retriever.Release();
        }
    }

    private static string? SaveThumbnail(MediaItem item, Uri uri, MediaMetadataRetriever retriever,
        long durationMs, int width, int height, string thumbsDir, CancellationToken ct)
    {
        using var frame = LoadFrame(uri, retriever, durationMs, width, height);
        if (frame is null)
            return null;
        ct.ThrowIfCancellationRequested();

        var target = System.IO.Path.Combine(thumbsDir, ThumbnailFileName(item.Id));
        var temp = target + ".tmp";
        var scaled = frame.Width > MaxThumbWidth
            ? Bitmap.CreateScaledBitmap(frame, MaxThumbWidth, Math.Max(1, frame.Height * MaxThumbWidth / frame.Width), true)
            : null;
        try
        {
            var bitmap = scaled ?? frame;
            using (var stream = new FileStream(temp, System.IO.FileMode.Create, System.IO.FileAccess.Write))
            {
                if (!bitmap.Compress(Bitmap.CompressFormat.Jpeg!, JpegQuality, stream))
                    throw new IOException("JPEG compression failed.");
            }
            File.Move(temp, target, overwrite: true);
            return target;
        }
        finally
        {
            scaled?.Dispose();
            if (File.Exists(temp))
                File.Delete(temp);
        }
    }

    private static Bitmap? LoadFrame(Uri uri, MediaMetadataRetriever retriever, long durationMs, int width, int height)
    {
        if (OperatingSystem.IsAndroidVersionAtLeast(29))
        {
            try
            {
                using var size = new global::Android.Util.Size(MaxThumbWidth, MaxThumbWidth);
                var bitmap = global::Android.App.Application.Context.ContentResolver!.LoadThumbnail(uri, size, null);
                if (bitmap is not null)
                    return bitmap;
            }
            catch (Exception ex) when (ex is Java.Lang.Exception or IOException)
            {
                // Fall through to the retriever.
            }
        }

        var timeUs = Math.Min(durationMs / 10, 10_000) * 1000;
        if (OperatingSystem.IsAndroidVersionAtLeast(27) && width > 0 && height > 0)
        {
            var w = Math.Min(width, MaxThumbWidth);
            return retriever.GetScaledFrameAtTime(timeUs, Option.ClosestSync, w, Math.Max(1, height * w / width));
        }
        return retriever.GetFrameAtTime(timeUs, Option.ClosestSync);
    }
}
