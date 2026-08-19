using DownloadYou.Domain.Enums;
using DownloadYou.Infrastructure.VideoSources;

namespace DownloadYou.Infrastructure.Tests;

public class YtDlpMetadataParserTests
{
    private const string SampleJson = """
    {
      "id": "BaW_jenozKc",
      "title": "youtube-dl test video \"'/\\ä↭𝕐",
      "uploader": "Philipp Hagemeister",
      "channel": "Philipp Hagemeister",
      "duration": 10.0,
      "thumbnail": "https://i.ytimg.com/vi/BaW_jenozKc/maxresdefault.jpg",
      "formats": [
        { "format_id": "sb0", "ext": "mhtml", "vcodec": "none", "acodec": "none" },
        { "format_id": "139", "ext": "m4a", "vcodec": "none", "acodec": "mp4a.40.5", "abr": 48.0, "filesize": 123456, "language": "en", "language_preference": 10 },
        { "format_id": "251", "ext": "webm", "vcodec": "none", "acodec": "opus", "abr": 160.0, "filesize_approx": 654321, "language": "es", "language_preference": -1 },
        { "format_id": "18", "ext": "mp4", "vcodec": "avc1.42001E", "acodec": "mp4a.40.2", "height": 360, "fps": 30.0, "tbr": 500.0, "abr": 96.0 },
        { "format_id": "299", "ext": "mp4", "vcodec": "avc1.640028", "acodec": "none", "height": 1080, "fps": 60.0, "vbr": 4500.0, "language": "en" }
      ]
    }
    """;

    [Fact]
    public void Parse_MapsTopLevelMetadata()
    {
        var info = YtDlpMetadataParser.Parse(SampleJson, "https://youtu.be/BaW_jenozKc");

        Assert.Equal("BaW_jenozKc", info.VideoId);
        Assert.Equal("Philipp Hagemeister", info.Author);
        Assert.Equal(TimeSpan.FromSeconds(10), info.Duration);
        Assert.Equal("https://i.ytimg.com/vi/BaW_jenozKc/maxresdefault.jpg", info.ThumbnailUrl);
    }

    [Fact]
    public void Parse_FiltersOutFormatsWithoutVideoOrAudio()
    {
        var info = YtDlpMetadataParser.Parse(SampleJson, "https://youtu.be/BaW_jenozKc");

        Assert.DoesNotContain(info.AvailableFormats, f => f.FormatId == "sb0");
        Assert.Equal(4, info.AvailableFormats.Count);
    }

    [Fact]
    public void Parse_ClassifiesStreamKindCorrectly()
    {
        var info = YtDlpMetadataParser.Parse(SampleJson, "https://youtu.be/BaW_jenozKc");
        var byId = info.AvailableFormats.ToDictionary(f => f.FormatId);

        Assert.Equal(StreamKind.AudioOnly, byId["139"].Kind);
        Assert.Equal(StreamKind.Muxed, byId["18"].Kind);
        Assert.Equal(StreamKind.VideoOnly, byId["299"].Kind);
    }

    [Fact]
    public void Parse_PicksHighestAvailableAudioBitrate()
    {
        var info = YtDlpMetadataParser.Parse(SampleJson, "https://youtu.be/BaW_jenozKc");

        Assert.Equal(160, info.BestAudioBitrateKbps);
    }

    [Fact]
    public void Parse_UsesTbrAsVideoBitrateFallback_WhenVbrMissing()
    {
        var info = YtDlpMetadataParser.Parse(SampleJson, "https://youtu.be/BaW_jenozKc");
        var muxed = info.AvailableFormats.Single(f => f.FormatId == "18");

        Assert.Equal(500, muxed.VideoBitrateKbps);
    }

    [Fact]
    public void Parse_MapsLanguageAndLanguagePreference_ForAudioFormats()
    {
        var info = YtDlpMetadataParser.Parse(SampleJson, "https://youtu.be/BaW_jenozKc");
        var byId = info.AvailableFormats.ToDictionary(f => f.FormatId);

        Assert.Equal("en", byId["139"].Language);
        Assert.Equal(10, byId["139"].LanguagePreference);
        Assert.Equal("es", byId["251"].Language);
        Assert.Equal(-1, byId["251"].LanguagePreference);
    }

    [Fact]
    public void Parse_IgnoresLanguage_OnVideoOnlyFormats_EvenIfYtDlpReportsOne()
    {
        // yt-dlp a veces manda "language" heredado del video en streams video-only; no tiene
        // sentido ahí (no hay audio) y confundiría al selector de idioma en la UI.
        var info = YtDlpMetadataParser.Parse(SampleJson, "https://youtu.be/BaW_jenozKc");
        var videoOnly = info.AvailableFormats.Single(f => f.FormatId == "299");

        Assert.Null(videoOnly.Language);
    }

    [Fact]
    public void Parse_Throws_WhenTitleIsMissing()
    {
        const string invalid = """{ "id": "abc" }""";

        Assert.Throws<InvalidOperationException>(() => YtDlpMetadataParser.Parse(invalid, "https://youtu.be/abc"));
    }

    [Fact]
    public void Parse_Throws_OnMalformedJson()
    {
        Assert.Throws<InvalidOperationException>(() => YtDlpMetadataParser.Parse("{ not json", "https://youtu.be/abc"));
    }
}
