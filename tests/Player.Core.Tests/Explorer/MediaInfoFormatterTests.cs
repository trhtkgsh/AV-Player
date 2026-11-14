using System.Globalization;
using Player.Core.Logic.Explorer;
using Player.Core.Models;

namespace Player.Core.Tests.Explorer;

public sealed class MediaInfoFormatterTests
{
    private const long SampleUnixSeconds = 1_700_000_000;

    private const ExplorerInfoFields AllFields =
        ExplorerInfoFields.Duration | ExplorerInfoFields.Resolution | ExplorerInfoFields.Size |
        ExplorerInfoFields.Date | ExplorerInfoFields.Path;

    // Size 1_048_576 gives "1.0 MB", date 1_700_000_000 gives "2023-11-14", path is "Movies/".
    private static MediaItem SampleItem() =>
        TestData.Item(1, "clip.mp4", sizeBytes: 1_048_576, dateAddedSeconds: SampleUnixSeconds);

    private static MediaMetadata SampleMetadata(MediaItem item) =>
        new(item.Id, 754_000, 1920, 1080, ThumbnailPath: null);

    [Theory]
    [InlineData(0, "")]
    [InlineData(-5, "")]
    [InlineData(7_000, "0:07")]
    [InlineData(59_999, "0:59")]
    [InlineData(754_000, "12:34")]
    [InlineData(3_600_000, "1:00:00")]
    [InlineData(3_723_000, "1:02:03")]
    public void Duration_Milliseconds_FormatsAsExpected(long milliseconds, string expected)
    {
        Assert.Equal(expected, MediaInfoFormatter.Duration(milliseconds));
    }

    [Theory]
    [InlineData(-1, "")]
    [InlineData(0, "0 B")]
    [InlineData(1023, "1023 B")]
    [InlineData(1024, "1.0 KB")]
    [InlineData(1536, "1.5 KB")]
    [InlineData(1_048_575, "1.0 MB")]
    [InlineData(1_048_576, "1.0 MB")]
    [InlineData(24_536_679, "23.4 MB")]
    [InlineData(1_073_741_823, "1.00 GB")]
    [InlineData(1_288_490_189, "1.20 GB")]
    public void Size_Bytes_FormatsAsExpected(long bytes, string expected)
    {
        Assert.Equal(expected, MediaInfoFormatter.Size(bytes));
    }

    [Fact]
    public void Resolution_PositiveWidthAndHeight_FormatsWidthXHeight()
    {
        Assert.Equal("1920x1080", MediaInfoFormatter.Resolution(1920, 1080));
    }

    [Theory]
    [InlineData(0, 1080)]
    [InlineData(1920, 0)]
    [InlineData(-1, 1080)]
    [InlineData(1920, -1)]
    [InlineData(0, 0)]
    public void Resolution_ZeroOrNegativeDimension_ReturnsEmpty(int width, int height)
    {
        Assert.Equal(string.Empty, MediaInfoFormatter.Resolution(width, height));
    }

    [Fact]
    public void Date_UnixSecondsInUtc_FormatsAsIsoDate()
    {
        Assert.Equal("2023-11-14", MediaInfoFormatter.Date(SampleUnixSeconds, TimeZoneInfo.Utc));
    }

    [Fact]
    public void InfoLine_AllFieldsAndMetadata_JoinsPartsInFixedOrder()
    {
        var item = SampleItem();

        var line = MediaInfoFormatter.InfoLine(item, SampleMetadata(item), AllFields, TimeZoneInfo.Utc);

        Assert.Equal("12:34 · 1920x1080 · 1.0 MB · 2023-11-14 · Movies/", line);
    }

    [Fact]
    public void InfoLine_NullMetadata_SkipsDurationAndResolution()
    {
        var line = MediaInfoFormatter.InfoLine(SampleItem(), null, AllFields, TimeZoneInfo.Utc);

        Assert.Equal("1.0 MB · 2023-11-14 · Movies/", line);
    }

    [Fact]
    public void InfoLine_NoFieldsEnabled_ReturnsEmpty()
    {
        var item = SampleItem();

        var line = MediaInfoFormatter.InfoLine(item, SampleMetadata(item), ExplorerInfoFields.None, TimeZoneInfo.Utc);

        Assert.Equal(string.Empty, line);
    }

    [Fact]
    public void InfoLine_SizeAndPathEnabled_IncludesOnlyThoseFields()
    {
        var item = SampleItem();

        var line = MediaInfoFormatter.InfoLine(
            item, SampleMetadata(item), ExplorerInfoFields.Size | ExplorerInfoFields.Path, TimeZoneInfo.Utc);

        Assert.Equal("1.0 MB · Movies/", line);
    }

    [Fact]
    public void Size_GermanCurrentCulture_StillUsesInvariantDecimalPoint()
    {
        var original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("de-DE");

            Assert.Equal("1.5 KB", MediaInfoFormatter.Size(1536));
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }
}
