using DownloadYou.Presentation.Formatting;

namespace DownloadYou.Presentation.Tests.Formatting;

public class LanguageNamesTests
{
    [Theory]
    [InlineData("es", "Español")]
    [InlineData("en", "Inglés")]
    [InlineData("fr", "Francés")]
    [InlineData("pt", "Portugués")]
    [InlineData("ru", "Ruso")]
    public void Resolve_KnownCodes_ReturnsSpanishName(string code, string expected) =>
        Assert.Equal(expected, LanguageNames.Resolve(code));

    [Fact]
    public void Resolve_UnknownCode_FallsBackToTheCodeItself() =>
        Assert.Equal("zz", LanguageNames.Resolve("zz"));
}
