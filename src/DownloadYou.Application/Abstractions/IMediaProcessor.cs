namespace DownloadYou.Application.Abstractions;

public interface IMediaProcessor
{
    Task<string> GetVersionAsync(Action<string>? onOutputLine = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Une un stream de video y uno de audio en un único archivo (remux con -c copy
    /// cuando los códecs son compatibles con el contenedor de salida; si no, reintenta
    /// transcodificando solo el audio a un códec compatible).
    /// </summary>
    /// <exception cref="InvalidOperationException">ffmpeg no pudo combinar los streams.</exception>
    Task MuxAsync(
        string videoFilePath,
        string audioFilePath,
        string outputFilePath,
        Action<TimeSpan>? onProgress = null,
        Action<string>? onOutputLine = null,
        CancellationToken cancellationToken = default);

    /// <summary>Extrae/convierte el audio de <paramref name="sourceFilePath"/> a MP3.</summary>
    /// <exception cref="InvalidOperationException">ffmpeg no pudo convertir el archivo.</exception>
    Task ExtractAudioAsync(
        string sourceFilePath,
        string outputFilePath,
        int bitrateKbps,
        Action<TimeSpan>? onProgress = null,
        Action<string>? onOutputLine = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifica con ffprobe que el archivo final tenga las pistas esperadas y una
    /// duración cercana a <paramref name="expectedDuration"/>.
    /// </summary>
    Task<MediaVerificationResult> VerifyAsync(
        string filePath,
        TimeSpan expectedDuration,
        bool requireVideoStream,
        Action<string>? onOutputLine = null,
        CancellationToken cancellationToken = default);
}
