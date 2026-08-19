namespace DownloadYou.Application.Validation;

/// <summary>
/// Defensa en profundidad antes de pasar la URL a yt-dlp (ver §12, Seguridad, del documento de arquitectura).
/// La validación real y definitiva de si el contenido existe la hace yt-dlp al intentar extraerlo.
/// </summary>
public static class YouTubeUrlValidator
{
    private static readonly HashSet<string> AllowedHosts = new(StringComparer.OrdinalIgnoreCase)
    {
        "youtube.com",
        "www.youtube.com",
        "m.youtube.com",
        "music.youtube.com",
        "youtu.be",
        "www.youtu.be"
    };

    public static bool IsValid(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return false;
        }

        if (!Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri))
        {
            return false;
        }

        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
        {
            return false;
        }

        return AllowedHosts.Contains(uri.Host);
    }
}
