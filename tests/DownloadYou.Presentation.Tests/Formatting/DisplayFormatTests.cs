using DownloadYou.Presentation.Formatting;

namespace DownloadYou.Presentation.Tests.Formatting;

public class DisplayFormatTests
{
    [Fact]
    public void Duration_UnderAnHour_OmitsHourSegment() =>
        Assert.Equal("04:05", DisplayFormat.Duration(new TimeSpan(0, 4, 5)));

    [Fact]
    public void Duration_OverAnHour_IncludesHourSegment() =>
        Assert.Equal("1:02:03", DisplayFormat.Duration(new TimeSpan(1, 2, 3)));

    [Fact]
    public void Speed_Null_ReturnsEmpty() =>
        Assert.Equal(string.Empty, DisplayFormat.Speed(null));

    [Fact]
    public void Speed_FormatsBytesPerSecond_WithUnitSuffix() =>
        Assert.Equal("1 MB/s", DisplayFormat.Speed(1024 * 1024));

    [Fact]
    public void Size_WithDownloadedOnly_OmitsTotal() =>
        Assert.Equal("500 B", DisplayFormat.Size(500, null));

    [Fact]
    public void Size_WithDownloadedAndTotal_ShowsBoth() =>
        Assert.Equal("500 KB / 1 MB", DisplayFormat.Size(500 * 1024, 1024 * 1024));

    [Fact]
    public void Size_NullDownloaded_ReturnsEmpty() =>
        Assert.Equal(string.Empty, DisplayFormat.Size(null, 1000));

    [Theory]
    [InlineData(500, "500 B")]
    [InlineData(2048, "2 KB")]
    [InlineData(5 * 1024 * 1024, "5 MB")]
    [InlineData(3L * 1024 * 1024 * 1024, "3 GB")]
    public void Size_PicksLargestSensibleUnit(double bytes, string expected) =>
        Assert.Equal(expected, DisplayFormat.Size(bytes));

    [Fact]
    public void Size_StopsAtGigabytes_EvenForHugeValues() =>
        Assert.EndsWith("GB", DisplayFormat.Size(9999L * 1024 * 1024 * 1024));

    [Fact]
    public void Eta_Null_ReturnsEmpty() =>
        Assert.Equal(string.Empty, DisplayFormat.Eta(null));

    [Fact]
    public void Eta_UsesDurationFormatting() =>
        Assert.Equal("00:45", DisplayFormat.Eta(TimeSpan.FromSeconds(45)));
}
