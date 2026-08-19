using DownloadYou.Application.Validation;

namespace DownloadYou.Application.Tests;

public class YouTubeUrlValidatorTests
{
    [Theory]
    [InlineData("https://www.youtube.com/watch?v=BaW_jenozKc")]
    [InlineData("https://youtube.com/watch?v=BaW_jenozKc")]
    [InlineData("https://m.youtube.com/watch?v=BaW_jenozKc")]
    [InlineData("https://music.youtube.com/watch?v=BaW_jenozKc")]
    [InlineData("https://youtu.be/BaW_jenozKc")]
    [InlineData("http://youtu.be/BaW_jenozKc")]
    [InlineData("  https://youtu.be/BaW_jenozKc  ")]
    public void IsValid_AcceptsKnownYouTubeHosts(string url) => Assert.True(YouTubeUrlValidator.IsValid(url));

    [Theory]
    [InlineData("https://vimeo.com/12345")]
    [InlineData("https://evil.com/?redirect=youtube.com")]
    [InlineData("https://youtube.com.evil.com/watch?v=x")]
    [InlineData("ftp://youtube.com/watch?v=x")]
    [InlineData("not a url")]
    [InlineData("")]
    [InlineData(null)]
    public void IsValid_RejectsNonYouTubeOrMalformedUrls(string? url) => Assert.False(YouTubeUrlValidator.IsValid(url));
}
