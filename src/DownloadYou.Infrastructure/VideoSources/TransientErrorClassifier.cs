namespace DownloadYou.Infrastructure.VideoSources;

/// <summary>
/// Heurística sobre el texto de error de yt-dlp para decidir si vale la pena reintentar
/// (§10 del documento de arquitectura: solo los errores transitorios se reintentan
/// automáticamente; un video privado/eliminado o una URL inválida deben fallar de una).
/// yt-dlp no expone un código de error estructurado por stdout/stderr, así que esto es
/// necesariamente heurístico — mejor cubrir los casos comunes que bloquear todo reintento.
/// </summary>
public static class TransientErrorClassifier
{
    private static readonly string[] TransientMarkers =
    [
        "timed out",
        "timeout",
        "connection reset",
        "connection refused",
        "temporary failure in name resolution",
        "unable to download webpage",
        "http error 500",
        "http error 502",
        "http error 503",
        "http error 504",
        "network is unreachable",
        "the read operation timed out",
        "remote end closed connection"
    ];

    public static bool IsTransient(string? errorMessage)
    {
        if (string.IsNullOrWhiteSpace(errorMessage))
        {
            return false;
        }

        var lowered = errorMessage.ToLowerInvariant();
        return TransientMarkers.Any(lowered.Contains);
    }
}
