using DownloadYou.Application.Services;

namespace DownloadYou.Application.Tests;

public class FileNameTemplateEngineTests
{
    [Fact]
    public void Resolve_SubstitutesAllPlaceholders()
    {
        var result = FileNameTemplateEngine.Resolve(
            "{title} - {author} [{quality}].{ext}", "Mi Video", "Autor", "1080p", "mp4");

        Assert.Equal("Mi Video - Autor [1080p].mp4", result);
    }

    [Theory]
    [InlineData("Video: \"raro\" / <test> | ?", "Video_ _raro_ _ _test_ _ _.mp4")]
    [InlineData("Título   con    espacios", "Título con espacios.mp4")]
    public void Resolve_SanitizesInvalidWindowsCharsAndCollapsesWhitespace(string title, string expected)
    {
        var result = FileNameTemplateEngine.Resolve("{title}.{ext}", title, "autor", "q", "mp4");

        Assert.Equal(expected, result);
    }

    [Fact]
    public void Sanitize_TruncatesLongNames_ButKeepsExtension()
    {
        var longTitle = new string('a', 300);

        var result = FileNameTemplateEngine.Sanitize($"{longTitle}.mp4");

        Assert.EndsWith(".mp4", result);
        Assert.True(result.Length <= 154);
    }

    [Fact]
    public void Sanitize_ReplacesPathSeparators_InsteadOfTruncatingOnThem()
    {
        // Path.GetFileNameWithoutExtension trataría "/" como separador de ruta y
        // truncaría todo lo anterior; Sanitize debe reemplazarlo como cualquier
        // otro carácter inválido, sin perder el resto del nombre.
        var result = FileNameTemplateEngine.Sanitize("2024/12: resumen.mp4");

        Assert.Equal("2024_12_ resumen.mp4", result);
    }

    [Fact]
    public void Sanitize_FallsBackToDefaultName_WhenResultWouldBeEmpty()
    {
        var result = FileNameTemplateEngine.Sanitize("   ");

        Assert.Equal("descarga", result);
    }
}
