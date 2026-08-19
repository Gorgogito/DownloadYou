using System.Windows;
using DownloadYou.Presentation.Converters;

namespace DownloadYou.Presentation.Tests.Converters;

public class CountToVisibilityConverterTests
{
    private readonly CountToVisibilityConverter _converter = new();

    [Fact]
    public void Convert_ReturnsVisible_WhenCountIsPositive() =>
        Assert.Equal(Visibility.Visible, _converter.Convert(3, typeof(Visibility), null!, null!));

    [Fact]
    public void Convert_ReturnsCollapsed_WhenCountIsZero() =>
        Assert.Equal(Visibility.Collapsed, _converter.Convert(0, typeof(Visibility), null!, null!));

    [Fact]
    public void Convert_WithInvertParameter_FlipsResult()
    {
        Assert.Equal(Visibility.Collapsed, _converter.Convert(3, typeof(Visibility), "Invert", null!));
        Assert.Equal(Visibility.Visible, _converter.Convert(0, typeof(Visibility), "Invert", null!));
    }
}
