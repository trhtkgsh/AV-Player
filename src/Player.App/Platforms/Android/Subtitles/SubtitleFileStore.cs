using System.Security.Cryptography;
using System.Text;

namespace Player.App.Subtitles;

/// <summary>The app's own folder of UTF-8 subtitle copies. Lives in app data so the system never clears it.</summary>
internal static class SubtitleFileStore
{
    public const long MaxBytes = 20L * 1024 * 1024;
    private static readonly TimeSpan MaxAge = TimeSpan.FromDays(30);
    private static readonly string[] Extensions = [".srt", ".ass", ".ssa"];

    public static string Folder => Path.Combine(FileSystem.AppDataDirectory, "subtitles");

    /// <summary>The lower-case extension of an accepted subtitle file name, or null.</summary>
    public static string? ExtensionOf(string? fileName)
    {
        var extension = Path.GetExtension(fileName ?? string.Empty).ToLowerInvariant();
        return Array.IndexOf(Extensions, extension) >= 0 ? extension : null;
    }

    /// <summary>Writes the UTF-8 copy under a stable name from the source key, or from the text when there is no key.</summary>
    public static string Write(string text, string? sourceKey, string extension)
    {
        var key = sourceKey ?? "content:" + text;
        var name = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(key)), 0, 8).ToLowerInvariant() + extension;
        Directory.CreateDirectory(Folder);
        var path = Path.Combine(Folder, name);
        File.WriteAllText(path, text, new UTF8Encoding(false));
        return path;
    }

    public static string ToFileUri(string path) => new Uri(path).AbsoluteUri;

    /// <summary>The path of a file: URI that points to an existing subtitle file inside the subtitles folder, else null.</summary>
    public static string? FromFileUri(string? uri)
    {
        try
        {
            if (string.IsNullOrEmpty(uri) || !Uri.TryCreate(uri, UriKind.Absolute, out var parsed) || !parsed.IsFile)
            {
                return null;
            }

            var path = Path.GetFullPath(parsed.LocalPath);
            var inside = path.StartsWith(Path.GetFullPath(Folder) + Path.DirectorySeparatorChar, StringComparison.Ordinal);
            return inside && File.Exists(path) && ExtensionOf(path) is not null ? path : null;
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <summary>Reads at most <see cref="MaxBytes"/>; null when the stream is larger.</summary>
    public static async Task<byte[]?> ReadLimitedAsync(Stream stream, CancellationToken ct)
    {
        using var buffer = new MemoryStream();
        var chunk = new byte[81920];
        int read;
        while ((read = await stream.ReadAsync(chunk, ct).ConfigureAwait(false)) > 0)
        {
            if (buffer.Length + read > MaxBytes)
            {
                return null;
            }

            buffer.Write(chunk, 0, read);
        }

        return buffer.ToArray();
    }

    /// <summary>Keeps a file that is in use from being cleaned up.</summary>
    public static void Touch(string path)
    {
        try
        {
            File.SetLastWriteTimeUtc(path, DateTime.UtcNow);
        }
        catch (Exception)
        {
            // Best effort.
        }
    }

    /// <summary>Deletes copies that were not written or restored for 30 days.</summary>
    public static void DeleteOld()
    {
        try
        {
            if (!Directory.Exists(Folder))
            {
                return;
            }

            var limit = DateTime.UtcNow - MaxAge;
            foreach (var file in Directory.EnumerateFiles(Folder))
            {
                try
                {
                    if (File.GetLastWriteTimeUtc(file) < limit)
                    {
                        File.Delete(file);
                    }
                }
                catch (Exception)
                {
                    // Skip a file that is locked or already gone.
                }
            }
        }
        catch (Exception)
        {
            // Cleanup must never matter.
        }
    }
}
