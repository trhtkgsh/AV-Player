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
    /// Returns the text of a subtitle file. Order: UTF-8 BOM, UTF-16 BOM, valid UTF-8 without BOM. For other bytes:
    /// (a) the code page of <paramref name="languageHint"/> when it names a non-Latin language
    /// (he, yi, ar, fa, ru, uk, bg, sr, el, th); (b) the code page found by <see cref="DetectLegacyCodePage"/>;
    /// (c) the code page of the hint; (d) Windows-1252. Never throws.
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

            if (LooksLikeUtf8(bytes))
            {
                // Valid UTF-8, or UTF-8 with a few bad bytes: those become replacement characters.
                return new UTF8Encoding(false, false).GetString(bytes);
            }

            var codePage = LegacyCodePage(languageHint);
            if (!IsNonLatin(codePage))
            {
                var detected = DetectLegacyCodePage(bytes);
                if (detected != 0)
                {
                    codePage = detected;
                }
            }

            return LegacyEncoding(codePage).GetString(bytes);
        }
        catch (Exception)
        {
            return Encoding.Latin1.GetString(bytes);
        }
    }

    /// <summary>
    /// Guesses the code page of text that is not UTF-8 from where its high bytes (0x80 and above) fall.
    /// Returns 1255 (Hebrew), 1251 (Cyrillic) or 0 when it cannot tell, for example for Latin text with a few accents
    /// or for fewer than 20 high bytes.
    /// Limits: Greek (1253) and Arabic (1256) are not detected. Their byte ranges overlap Cyrillic and, for upper-case
    /// text, each other, so they stay with the language hint; a Greek or Arabic file without a hint may come out as
    /// Cyrillic or Latin. Cyrillic is only claimed when most high bytes are lower-case letters (0xE0..0xFF), so a
    /// text in capitals returns 0.
    /// </summary>
    public static int DetectLegacyCodePage(ReadOnlySpan<byte> bytes)
    {
        int high = 0, letters = 0, hebrew = 0, cyrillic = 0, cyrillicLower = 0, cyrillicMark = 0, hebrewExclude = 0;
        foreach (var b in bytes)
        {
            if (b < 0x80)
            {
                if ((b >= 'A' && b <= 'Z') || (b >= 'a' && b <= 'z'))
                {
                    letters++;
                }

                continue;
            }

            high++;
            if (b >= 0xE0 && b <= 0xFA)
            {
                hebrew++;
            }

            if (b >= 0xC0)
            {
                cyrillic++;
            }

            if (b >= 0xE0)
            {
                cyrillicLower++;
            }

            if ((b >= 0xC0 && b <= 0xDF) || b >= 0xFB)
            {
                cyrillicMark++;

                // 0xFD and 0xFE are the left/right marks that Hebrew files use.
                if (b != 0xFD && b != 0xFE)
                {
                    hebrewExclude++;
                }
            }
        }

        if (high < 20 || high * 100 < 15 * (letters + high))
        {
            return 0;
        }

        if (hebrew * 100 >= 90 * high && hebrewExclude * 100 < 5 * high)
        {
            return 1255;
        }

        if (cyrillic * 100 >= 90 * high && cyrillicMark * 100 >= 5 * high && cyrillicLower * 100 >= 70 * high)
        {
            return 1251;
        }

        return 0;
    }

    private static bool IsNonLatin(int codePage) => codePage is 1255 or 1256 or 1251 or 1253 or 874;

    /// <summary>
    /// True for text that is UTF-8: it is valid, or it has at least one valid multi-byte sequence and the invalid bytes
    /// are at most 1% of the valid multi-byte sequences. One truncated sequence at the very end is always tolerated.
    /// Pure ASCII is UTF-8. A legacy-encoded file has many invalid bytes and almost no valid sequences, so it is not.
    /// </summary>
    public static bool LooksLikeUtf8(ReadOnlySpan<byte> bytes)
    {
        var multiByte = 0;
        var invalid = 0;
        var i = 0;
        while (i < bytes.Length)
        {
            if (bytes[i] < 0x80)
            {
                i++;
                continue;
            }

            var status = Rune.DecodeFromUtf8(bytes.Slice(i), out _, out var consumed);
            if (status == System.Buffers.OperationStatus.Done)
            {
                multiByte++;
            }
            else if (status == System.Buffers.OperationStatus.NeedMoreData)
            {
                break; // truncated at the very end
            }
            else
            {
                invalid += Math.Max(consumed, 1);
            }

            i += Math.Max(consumed, 1);
        }

        return invalid == 0 || (multiByte >= 1 && invalid * 100 <= multiByte);
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
