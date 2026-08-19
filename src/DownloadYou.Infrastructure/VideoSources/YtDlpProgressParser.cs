using System.Globalization;
using DownloadYou.Application.Abstractions;

namespace DownloadYou.Infrastructure.VideoSources;

/// <summary>
/// Interpreta las líneas emitidas por nuestro --progress-template (ver YtDlpVideoSource),
/// en vez de parsear el texto de progreso legible por humanos de yt-dlp — mucho más
/// robusto porque son campos numéricos crudos, no texto formateado con unidades/locale.
/// </summary>
public static class YtDlpProgressParser
{
    public const string LinePrefix = "DYPROGRESS ";

    public static DownloadProgressUpdate? TryParse(string line)
    {
        if (!line.StartsWith(LinePrefix, StringComparison.Ordinal))
        {
            return null;
        }

        var parts = line[LinePrefix.Length..].Split('|');
        if (parts.Length != 6)
        {
            return null;
        }

        var status = parts[0];
        var downloadedBytes = ParseLong(parts[1]);
        var totalBytes = ParseLong(parts[2]) ?? ParseLong(parts[3]);
        var speed = ParseDouble(parts[4]);
        var etaSeconds = ParseDouble(parts[5]);

        return new DownloadProgressUpdate(
            status,
            downloadedBytes,
            totalBytes,
            speed,
            etaSeconds is null ? null : TimeSpan.FromSeconds(etaSeconds.Value));
    }

    private static long? ParseLong(string value) =>
        long.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var v) ? v : null;

    private static double? ParseDouble(string value) =>
        double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var v) ? v : null;
}
