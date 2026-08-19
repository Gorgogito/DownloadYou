namespace DownloadYou.Application.Abstractions;

public sealed class InvalidYouTubeUrlException(string url)
    : Exception($"'{url}' no es una URL de YouTube válida.")
{
    public string Url { get; } = url;
}
