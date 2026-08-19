using DownloadYou.Infrastructure.MediaProcessing;

namespace DownloadYou.Infrastructure.Tests;

public class FfprobeResultParserTests
{
    private const string MuxedJson = """
    {
      "streams": [
        { "codec_type": "video" },
        { "codec_type": "audio" }
      ],
      "format": { "duration": "19.133243" }
    }
    """;

    private const string AudioOnlyJson = """
    {
      "streams": [
        { "codec_type": "audio" }
      ],
      "format": { "duration": "19.133243" }
    }
    """;

    [Fact]
    public void Verify_Succeeds_WhenDurationMatchesAndStreamsPresent()
    {
        var result = FfprobeResultParser.Verify(MuxedJson, TimeSpan.FromSeconds(19), requireVideoStream: true);

        Assert.True(result.IsValid);
        Assert.Equal(TimeSpan.FromSeconds(19.133243), result.ActualDuration);
    }

    [Fact]
    public void Verify_Fails_WhenVideoStreamRequiredButMissing()
    {
        var result = FfprobeResultParser.Verify(AudioOnlyJson, TimeSpan.FromSeconds(19), requireVideoStream: true);

        Assert.False(result.IsValid);
        Assert.Contains("video", result.Error);
    }

    [Fact]
    public void Verify_Succeeds_ForAudioOnlyOutput_WhenVideoNotRequired()
    {
        var result = FfprobeResultParser.Verify(AudioOnlyJson, TimeSpan.FromSeconds(19), requireVideoStream: false);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Verify_Fails_WhenNoAudioStream()
    {
        const string videoOnlyJson = """{ "streams": [{ "codec_type": "video" }], "format": { "duration": "19.0" } }""";

        var result = FfprobeResultParser.Verify(videoOnlyJson, TimeSpan.FromSeconds(19), requireVideoStream: true);

        Assert.False(result.IsValid);
        Assert.Contains("audio", result.Error);
    }

    [Fact]
    public void Verify_Fails_WhenDurationIsFarFromExpected()
    {
        var result = FfprobeResultParser.Verify(MuxedJson, TimeSpan.FromMinutes(10), requireVideoStream: true);

        Assert.False(result.IsValid);
        Assert.Contains("Duración inesperada", result.Error);
    }

    [Fact]
    public void Verify_ToleratesSmallDurationDrift()
    {
        // 19.13s real vs 20s esperados: dentro de la tolerancia de max(2s, 5%).
        var result = FfprobeResultParser.Verify(MuxedJson, TimeSpan.FromSeconds(20), requireVideoStream: true);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Verify_SkipsDurationCheck_WhenExpectedDurationIsZero()
    {
        var result = FfprobeResultParser.Verify(MuxedJson, TimeSpan.Zero, requireVideoStream: true);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Verify_Fails_OnMalformedJson()
    {
        var result = FfprobeResultParser.Verify("{ not json", TimeSpan.FromSeconds(19), requireVideoStream: true);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Verify_Fails_WhenDurationFieldMissing()
    {
        const string json = """{ "streams": [{ "codec_type": "audio" }], "format": {} }""";

        var result = FfprobeResultParser.Verify(json, TimeSpan.FromSeconds(19), requireVideoStream: false);

        Assert.False(result.IsValid);
    }
}
