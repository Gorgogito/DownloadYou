using DownloadYou.Presentation.Converters;

namespace DownloadYou.Presentation.Tests.Converters;

public class LanguageDisplayConverterTests
{
    private readonly LanguageDisplayConverter _converter = new();

    [Fact]
    public void Convert_KnownCode_ReturnsFriendlyName() =>
        Assert.Equal("Español", _converter.Convert("es", typeof(string), null!, null!));

    [Fact]
    public void Convert_UnknownCode_ReturnsCodeAsIs() =>
        Assert.Equal("xx-unknown", _converter.Convert("xx-unknown", typeof(string), null!, null!));

    [Fact]
    public void Convert_Null_ReturnsEmpty() =>
        Assert.Equal(string.Empty, _converter.Convert(null, typeof(string), null!, null!));

    [Fact]
    public void Convert_IsCaseInsensitive() =>
        Assert.Equal("Español", _converter.Convert("ES", typeof(string), null!, null!));
}
