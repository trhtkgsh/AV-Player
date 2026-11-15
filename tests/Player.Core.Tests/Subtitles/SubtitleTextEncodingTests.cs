using System.Text;
using Player.Core.Logic.Subtitles;

namespace Player.Core.Tests.Subtitles;

public sealed class SubtitleTextEncodingTests
{
    private const string HebrewSentence = "שלום עולם, זו כתובית בעברית.";
    private const string RussianSentence = "Привет, мир. Это русские субтитры.";
    private const string FrenchSentence = "Voilà un été très chaud à Besançon.";
    private const string EnglishSentence = "Hello world, this is an English subtitle.";
    private const string GreekSentence = "Γεια σου κόσμε, αυτός είναι ελληνικός υπότιτλος.";
    private const string ArabicSentence = "مرحبا بالعالم، هذه ترجمة عربية.";

    static SubtitleTextEncodingTests()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    private static string Srt(string line) =>
        "1\r\n00:00:01,000 --> 00:00:03,500\r\n" + line + "\r\n" + line + "\r\n\r\n" +
        "2\r\n00:00:04,000 --> 00:00:06,000\r\n" + line + "\r\n";

    private static byte[] Bytes(int codePage, string text) => Encoding.GetEncoding(codePage).GetBytes(text);

    private static byte[] WithBom(byte[] bom, byte[] body) => [.. bom, .. body];

    public static TheoryData<string> AllTexts => new()
    {
        { Srt(HebrewSentence) },
        { Srt(RussianSentence) },
        { Srt(FrenchSentence) },
        { Srt(EnglishSentence) },
    };

    [Fact]
    public void Decode_EmptyInput_ReturnsEmptyString()
    {
        Assert.Equal(string.Empty, SubtitleTextEncoding.Decode([], "he"));
    }

    [Theory]
    [MemberData(nameof(AllTexts))]
    public void Decode_Utf8WithBom_ReturnsTextWithoutBom(string text)
    {
        var bytes = WithBom([0xEF, 0xBB, 0xBF], new UTF8Encoding(false).GetBytes(text));

        Assert.Equal(text, SubtitleTextEncoding.Decode(bytes, null));
    }

    [Theory]
    [MemberData(nameof(AllTexts))]
    public void Decode_Utf16LittleEndianWithBom_ReturnsTextWithoutBom(string text)
    {
        var bytes = WithBom([0xFF, 0xFE], Encoding.Unicode.GetBytes(text));

        Assert.Equal(text, SubtitleTextEncoding.Decode(bytes, null));
    }

    [Theory]
    [MemberData(nameof(AllTexts))]
    public void Decode_Utf16BigEndianWithBom_ReturnsTextWithoutBom(string text)
    {
        var bytes = WithBom([0xFE, 0xFF], Encoding.BigEndianUnicode.GetBytes(text));

        Assert.Equal(text, SubtitleTextEncoding.Decode(bytes, null));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("en")]
    [InlineData("he")]
    [InlineData("ru")]
    [InlineData("fr")]
    public void Decode_Utf8WithoutBom_DecodesAsUtf8WhateverTheHint(string? hint)
    {
        foreach (var sentence in new[] { HebrewSentence, RussianSentence, FrenchSentence })
        {
            var text = Srt(sentence);

            Assert.Equal(text, SubtitleTextEncoding.Decode(new UTF8Encoding(false).GetBytes(text), hint));
        }
    }

    [Fact]
    public void Decode_HebrewUtf8WithEnglishHint_StaysHebrew()
    {
        var text = Srt(HebrewSentence);

        Assert.Equal(text, SubtitleTextEncoding.Decode(Encoding.UTF8.GetBytes(text), "en"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("he")]
    [InlineData("en")]
    public void Decode_PureAscii_ReturnsUnchanged(string? hint)
    {
        var text = Srt(EnglishSentence);

        Assert.Equal(text, SubtitleTextEncoding.Decode(Encoding.ASCII.GetBytes(text), hint));
    }

    [Theory]
    [InlineData("he")]
    [InlineData(null)]
    [InlineData("en")]
    [InlineData("heb")]
    [InlineData("iw")]
    [InlineData("Hebrew")]
    [InlineData("he-IL")]
    public void Decode_Windows1255Hebrew_ReturnsHebrewText(string? hint)
    {
        var text = Srt(HebrewSentence);

        Assert.Equal(text, SubtitleTextEncoding.Decode(Bytes(1255, text), hint));
    }

    [Theory]
    [InlineData("ru")]
    [InlineData(null)]
    [InlineData("en")]
    [InlineData("rus")]
    [InlineData("Russian")]
    public void Decode_Windows1251Russian_ReturnsRussianText(string? hint)
    {
        var text = Srt(RussianSentence);

        Assert.Equal(text, SubtitleTextEncoding.Decode(Bytes(1251, text), hint));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("en")]
    [InlineData("fr")]
    public void Decode_Windows1252French_ReturnsFrenchText(string? hint)
    {
        var text = Srt(FrenchSentence);

        Assert.Equal(text, SubtitleTextEncoding.Decode(Bytes(1252, text), hint));
    }

    [Fact]
    public void Decode_Windows1252FrenchSingleSentence_ReturnsFrenchText()
    {
        Assert.Equal(FrenchSentence, SubtitleTextEncoding.Decode(Bytes(1252, FrenchSentence), null));
    }

    [Fact]
    public void Decode_Windows1253GreekWithGreekHint_ReturnsGreekText()
    {
        var text = Srt(GreekSentence);

        Assert.Equal(text, SubtitleTextEncoding.Decode(Bytes(1253, text), "el"));
    }

    [Fact]
    public void Decode_Windows1256ArabicWithArabicHint_ReturnsArabicText()
    {
        var text = Srt(ArabicSentence);

        Assert.Equal(text, SubtitleTextEncoding.Decode(Bytes(1256, text), "ar"));
    }

    [Fact]
    public void DetectLegacyCodePage_HebrewBytes_Returns1255()
    {
        Assert.Equal(1255, SubtitleTextEncoding.DetectLegacyCodePage(Bytes(1255, Srt(HebrewSentence))));
    }

    [Fact]
    public void DetectLegacyCodePage_RussianBytes_Returns1251()
    {
        Assert.Equal(1251, SubtitleTextEncoding.DetectLegacyCodePage(Bytes(1251, Srt(RussianSentence))));
    }

    [Fact]
    public void DetectLegacyCodePage_FrenchBytes_ReturnsZero()
    {
        Assert.Equal(0, SubtitleTextEncoding.DetectLegacyCodePage(Bytes(1252, Srt(FrenchSentence))));
    }

    [Fact]
    public void DetectLegacyCodePage_Ascii_ReturnsZero()
    {
        Assert.Equal(0, SubtitleTextEncoding.DetectLegacyCodePage(Encoding.ASCII.GetBytes(Srt(EnglishSentence))));
    }

    [Fact]
    public void DetectLegacyCodePage_EmptyInput_ReturnsZero()
    {
        Assert.Equal(0, SubtitleTextEncoding.DetectLegacyCodePage([]));
    }

    [Fact]
    public void DetectLegacyCodePage_FewerThan20HighBytes_ReturnsZero()
    {
        var bytes = Bytes(1255, "שלום עולם");

        Assert.True(bytes.Count(b => b >= 0x80) < 20);
        Assert.Equal(0, SubtitleTextEncoding.DetectLegacyCodePage(bytes));
    }

    [Theory]
    [InlineData("he", 1255)]
    [InlineData("yi", 1255)]
    [InlineData("ar", 1256)]
    [InlineData("fa", 1256)]
    [InlineData("ru", 1251)]
    [InlineData("uk", 1251)]
    [InlineData("bg", 1251)]
    [InlineData("sr", 1251)]
    [InlineData("el", 1253)]
    [InlineData("tr", 1254)]
    [InlineData("pl", 1250)]
    [InlineData("cs", 1250)]
    [InlineData("hu", 1250)]
    [InlineData("ro", 1250)]
    [InlineData("hr", 1250)]
    [InlineData("sk", 1250)]
    [InlineData("sl", 1250)]
    [InlineData("th", 874)]
    [InlineData("lt", 1257)]
    [InlineData("lv", 1257)]
    [InlineData("et", 1257)]
    [InlineData("vi", 1258)]
    [InlineData(null, 1252)]
    [InlineData("", 1252)]
    [InlineData("en", 1252)]
    [InlineData("fr", 1252)]
    [InlineData("xx", 1252)]
    public void LegacyCodePage_Language_ReturnsCodePage(string? hint, int expected)
    {
        Assert.Equal(expected, SubtitleTextEncoding.LegacyCodePage(hint));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(17)]
    [InlineData(255)]
    [InlineData(4096)]
    public void Decode_RandomBytes_DoesNotThrow(int length)
    {
        var bytes = new byte[length];
        new Random(12345 + length).NextBytes(bytes);

        foreach (var hint in new string?[] { null, "", "he", "en", "xx" })
        {
            var exception = Record.Exception(() => SubtitleTextEncoding.Decode(bytes, hint));

            Assert.Null(exception);
        }
    }

    [Theory]
    [InlineData(0xEF, 0xBB, 0xBF)]
    [InlineData(0xFF, 0xFE)]
    [InlineData(0xFE, 0xFF)]
    public void Decode_LoneBom_ReturnsEmptyString(params int[] bom)
    {
        var bytes = bom.Select(b => (byte)b).ToArray();

        Assert.Equal(string.Empty, SubtitleTextEncoding.Decode(bytes, "he"));
    }

    [Fact]
    public void Decode_OddNumberOfBytesAfterUtf16Bom_DoesNotThrow()
    {
        byte[] bytes = [0xFF, 0xFE, 0x41, 0x00, 0x42];

        var exception = Record.Exception(() => SubtitleTextEncoding.Decode(bytes, null));

        Assert.Null(exception);
    }

    private const string ReplacementChar = "\uFFFD";

    private static readonly UTF8Encoding Utf8 = new(false);

    // About 400 Hebrew letters: enough valid sequences for one or two bad bytes to stay under the 1% limit.
    private static string LongHebrewText() =>
        string.Concat(Enumerable.Repeat(HebrewSentence + " ", 20)).TrimEnd();

    // UTF-8 bytes of the text with one single byte (0xFF) put in place of the character at charIndex.
    private static byte[] ReplaceCharWithFfByte(string text, int charIndex) =>
        [.. Utf8.GetBytes(text[..charIndex]), 0xFF, .. Utf8.GetBytes(text[(charIndex + 1)..])];

    public static TheoryData<string?> Hints => new() { { null }, { "en" }, { "he" } };

    [Theory]
    [MemberData(nameof(Hints))]
    public void Decode_Utf8HebrewWithOneBadAsciiByte_KeepsAllLettersAndOneReplacementChar(string? hint)
    {
        var text = LongHebrewText();
        var index = text.IndexOf(' ', text.Length / 2);
        var bytes = ReplaceCharWithFfByte(text, index);

        Assert.True(SubtitleTextEncoding.LooksLikeUtf8(bytes));
        Assert.Equal(text[..index] + ReplacementChar + text[(index + 1)..], SubtitleTextEncoding.Decode(bytes, hint));
    }

    [Theory]
    [MemberData(nameof(Hints))]
    public void Decode_Utf8HebrewWithOneBadByteInsideALetter_KeepsEveryOtherLetter(string? hint)
    {
        // A Hebrew letter has two bytes: its first byte then stands alone, so the letter becomes two replacement chars.
        var text = LongHebrewText();
        var index = text.IndexOf('ל', text.Length / 2);
        byte[] bytes = [.. Utf8.GetBytes(text[..index]), 0xD7, 0xFF, .. Utf8.GetBytes(text[(index + 1)..])];

        Assert.True(SubtitleTextEncoding.LooksLikeUtf8(bytes));
        Assert.Equal(
            text[..index] + ReplacementChar + ReplacementChar + text[(index + 1)..],
            SubtitleTextEncoding.Decode(bytes, hint));
    }

    [Theory]
    [MemberData(nameof(Hints))]
    public void Decode_Utf8HebrewCutInsideLastLetter_KeepsOtherLettersAndEndsWithOneReplacementChar(string? hint)
    {
        var text = HebrewSentence.TrimEnd('.');
        var full = Utf8.GetBytes(text);
        var bytes = full[..^1];

        Assert.True(SubtitleTextEncoding.LooksLikeUtf8(bytes));
        Assert.Equal(text[..^1] + ReplacementChar, SubtitleTextEncoding.Decode(bytes, hint));
    }

    [Fact]
    public void LooksLikeUtf8_Windows1255Hebrew_ReturnsFalse()
    {
        Assert.False(SubtitleTextEncoding.LooksLikeUtf8(Bytes(1255, Srt(HebrewSentence))));
    }

    [Fact]
    public void LooksLikeUtf8_Windows1251Russian_ReturnsFalse()
    {
        Assert.False(SubtitleTextEncoding.LooksLikeUtf8(Bytes(1251, Srt(RussianSentence))));
    }

    [Fact]
    public void LooksLikeUtf8_Windows1252French_ReturnsFalse()
    {
        Assert.False(SubtitleTextEncoding.LooksLikeUtf8(Bytes(1252, Srt(FrenchSentence))));
    }

    [Fact]
    public void LooksLikeUtf8_PureAscii_ReturnsTrue()
    {
        Assert.True(SubtitleTextEncoding.LooksLikeUtf8(Encoding.ASCII.GetBytes(Srt(EnglishSentence))));
    }

    [Fact]
    public void LooksLikeUtf8_EmptyInput_ReturnsTrue()
    {
        Assert.True(SubtitleTextEncoding.LooksLikeUtf8([]));
    }

    [Fact]
    public void LooksLikeUtf8_ValidUtf8Hebrew_ReturnsTrue()
    {
        Assert.True(SubtitleTextEncoding.LooksLikeUtf8(Utf8.GetBytes(Srt(HebrewSentence))));
    }

    [Fact]
    public void LooksLikeUtf8_MoreThanOnePercentBrokenSequences_ReturnsFalse()
    {
        var bytes = new List<byte>();
        for (var i = 0; i < 200; i++)
        {
            var letter = Utf8.GetBytes(((char)('א' + (i % 27))).ToString());
            if (i % 10 == 9)
            {
                letter[0] = 0xFF;
            }

            bytes.AddRange(letter);
        }

        Assert.False(SubtitleTextEncoding.LooksLikeUtf8(bytes.ToArray()));
    }

    [Fact]
    public void LooksLikeUtf8_OnlyInvalidBytes_ReturnsFalse()
    {
        Assert.False(SubtitleTextEncoding.LooksLikeUtf8([0xFF, 0xFF, 0xFF]));
    }

    [Theory]
    [InlineData(0xC3, 0x28)]
    [InlineData(0xE2, 0x82)]
    [InlineData(0xF0, 0x9F, 0x92)]
    [InlineData(0x80, 0x80, 0x80)]
    [InlineData(0xFF, 0xFF, 0xFF)]
    public void Decode_InvalidUtf8Sequence_DoesNotThrow(params int[] values)
    {
        var bytes = values.Select(b => (byte)b).ToArray();

        foreach (var hint in new string?[] { null, "he", "en" })
        {
            var exception = Record.Exception(() => SubtitleTextEncoding.Decode(bytes, hint));

            Assert.Null(exception);
        }
    }
}
