using System.Windows;
using DownloadYou.Presentation.Converters;

namespace DownloadYou.Presentation.Tests.Converters;

public class PercentToStarWidthConverterTests
{
    private readonly PercentToStarWidthConverter _converter = new();

    [Fact]
    public void Convert_ReturnsStarWidth_MatchingPercent()
    {
        var result = (GridLength)_converter.Convert(40.0, typeof(GridLength), null!, null!);

        Assert.Equal(40, result.Value);
        Assert.True(result.IsStar);
    }

    [Fact]
    public void Convert_WithRemainingParameter_ReturnsComplement()
    {
        var result = (GridLength)_converter.Convert(40.0, typeof(GridLength), "Remaining", null!);

        Assert.Equal(60, result.Value);
    }

    [Theory]
    [InlineData(-10.0)]
    [InlineData(150.0)]
    public void Convert_ClampsOutOfRangeValues(double percent)
    {
        var result = (GridLength)_converter.Convert(percent, typeof(GridLength), null!, null!);

        Assert.InRange(result.Value, 0, 100);
    }

    [Fact]
    public void Convert_NeverReturnsZeroWidth_ToAvoidCollapsingTheColumn()
    {
        var result = (GridLength)_converter.Convert(0.0, typeof(GridLength), null!, null!);

        Assert.True(result.Value > 0);
    }

    [Fact]
    public void Convert_NonDoubleValue_TreatedAsZero()
    {
        var result = (GridLength)_converter.Convert("not-a-number", typeof(GridLength), null!, null!);

        Assert.True(result.Value > 0);
        Assert.True(result.Value < 1);
    }
}
