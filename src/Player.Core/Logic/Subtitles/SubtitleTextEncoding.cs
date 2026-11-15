using System.Text;

namespace Player.Core.Logic.Subtitles;

/// <summary>
/// Reads subtitle files of unknown encoding. Many files are not UTF-8 (Hebrew files are often Windows-1255).
/// </summary>
public static class SubtitleTextEncoding
{
    static SubtitleTextEncoding()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    /// <summary>
    /// Returns the text of a subtitle file. Order: UTF-8 BOM, UTF-16 BOM, valid UTF-8 without BOM,
    /// otherwise the legacy code page of <paramref name="languageHint"/>, otherwise Windows-1252. Never throws.
    /// </summary>
    public static string Decode(ReadOnlySpan<byte> bytes, string? languageHint)
    {
        if (bytes.IsEmpty)
        {
            return string.Empty;
        }

        try
        {
            if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
            {
                return Encoding.UTF8.GetString(bytes.Slice(3));
            }

            if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE)
            {
                return Encoding.Unicode.GetString(bytes.Slice(2));
            }

            if (bytes.Length >= 2 && bytes[0] == 0xFE && bytes[1] == 0xFF)
            {
                return Encoding.BigEndianUnicode.GetString(bytes.Slice(2));
            }

            try
            {
                return new UTF8Encoding(false, true).GetString(bytes);
            }
            catch (ArgumentException)
            {
                // Not valid UTF-8: a legacy code page.
            }

            return LegacyEncoding(LegacyCodePage(languageHint)).GetString(bytes);
        }
        catch (Exception)
        {
            return Encoding.Latin1.GetString(bytes);
        }
    }

    /// <summary>The Windows code page that subtitle files of this language usually use; 1252 by default.</summary>
    public static int LegacyCodePage(string? languageHint) => LanguageCodes.Normalize(languageHint) switch
    {
        "he" or "yi" => 1255,
        "ar" or "fa" => 1256,
        "ru" or "uk" or "bg" or "sr" => 1251,
        "el" => 1253,
        "tr" => 1254,
        "pl" or "cs" or "hu" or "ro" or "hr" or "sk" or "sl" => 1250,
        "th" => 874,
        "lt" or "lv" or "et" => 1257,
        "vi" => 1258,
        _ => 1252,
    };

    private static Encoding LegacyEncoding(int codePage)
    {
        try
        {
            return Encoding.GetEncoding(codePage);
        }
        catch (Exception)
        {
            return Encoding.Latin1;
        }
    }
}
