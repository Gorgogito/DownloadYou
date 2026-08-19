using DownloadYou.Presentation.Converters;
using Wpf.Ui.Controls;

namespace DownloadYou.Presentation.Tests.Converters;

public class BoolToAppearanceConverterTests
{
    private readonly BoolToAppearanceConverter _converter = new();

    [Fact]
    public void Convert_ReturnsPrimary_WhenTrue() =>
        Assert.Equal(ControlAppearance.Primary, _converter.Convert(true, typeof(ControlAppearance), null!, null!));

    [Theory]
    [InlineData(false)]
    [InlineData(null)]
    public void Convert_ReturnsSecondary_WhenNotTrue(object? value) =>
        Assert.Equal(ControlAppearance.Secondary, _converter.Convert(value, typeof(ControlAppearance), null!, null!));

    [Fact]
    public void ConvertBack_ThrowsNotSupported() =>
        Assert.Throws<NotSupportedException>(() => _converter.ConvertBack(true, typeof(bool), null!, null!));
}
