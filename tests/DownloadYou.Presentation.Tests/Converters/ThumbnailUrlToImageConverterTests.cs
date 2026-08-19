using DownloadYou.Presentation.Converters;

namespace DownloadYou.Presentation.Tests.Converters;

public class ThumbnailUrlToImageConverterTests
{
    private readonly ThumbnailUrlToImageConverter _converter = new();

    [Fact]
    public void Convert_ReturnsNull_WhenValueIsNotAString() =>
        Assert.Null(_converter.Convert(123, typeof(object), null!, null!));

    [Fact]
    public void Convert_ReturnsNull_WhenUrlIsEmpty() =>
        Assert.Null(_converter.Convert(string.Empty, typeof(object), null!, null!));

    [Fact]
    public void Convert_ReturnsNull_WhenUrlIsRelativeOrMalformed() =>
        Assert.Null(_converter.Convert("not a url", typeof(object), null!, null!));

    [Fact]
    public void ConvertBack_ThrowsNotSupported() =>
        Assert.Throws<NotSupportedException>(() => _converter.ConvertBack(null, typeof(object), null!, null!));
}
