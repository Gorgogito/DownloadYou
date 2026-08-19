using DownloadYou.Infrastructure.VideoSources;

namespace DownloadYou.Infrastructure.Tests;

public class TransientErrorClassifierTests
{
    [Theory]
    [InlineData("ERROR: unable to download webpage: HTTP Error 503: Service Unavailable")]
    [InlineData("urlopen error [Errno 11001] getaddrinfo failed: Temporary failure in name resolution")]
    [InlineData("The read operation timed out")]
    [InlineData("Connection reset by peer")]
    [InlineData("Connection refused")]
    [InlineData("Network is unreachable")]
    public void IsTransient_ReturnsTrue_ForKnownNetworkGlitches(string message) =>
        Assert.True(TransientErrorClassifier.IsTransient(message));

    [Theory]
    [InlineData("ERROR: [youtube] abc123: Private video. Sign in if you've been invited")]
    [InlineData("ERROR: [youtube] abc123: Video unavailable. This video has been removed")]
    [InlineData("ERROR: unable to download video data: HTTP Error 403: Forbidden")]
    [InlineData("ERROR: Unsupported URL")]
    public void IsTransient_ReturnsFalse_ForPermanentErrors(string message) =>
        Assert.False(TransientErrorClassifier.IsTransient(message));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void IsTransient_ReturnsFalse_ForEmptyMessages(string? message) =>
        Assert.False(TransientErrorClassifier.IsTransient(message));

    [Fact]
    public void IsTransient_IsCaseInsensitive()
    {
        Assert.True(TransientErrorClassifier.IsTransient("CONNECTION RESET by peer"));
    }
}
