using System.Globalization;
using System.Text.Json;
using DownloadYou.Application.Abstractions;

namespace DownloadYou.Infrastructure.MediaProcessing;

public static class FfprobeResultParser
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true
    };

    public static MediaVerificationResult Verify(string json, TimeSpan expectedDuration, bool requireVideoStream)
    {
        FfprobeOutput? probe;
        try
        {
            probe = JsonSerializer.Deserialize<FfprobeOutput>(json, Options);
        }
        catch (JsonException)
        {
            return MediaVerificationResult.Invalid("ffprobe devolvió una respuesta que no se pudo interpretar como JSON.");
        }

        if (probe?.Format?.Duration is not { } durationText ||
            !double.TryParse(durationText, NumberStyles.Float, CultureInfo.InvariantCulture, out var seconds))
        {
            return MediaVerificationResult.Invalid("ffprobe no reportó la duración del archivo final.");
        }

        var streams = probe.Streams ?? [];
        if (!streams.Any(s => s.CodecType == "audio"))
        {
            return MediaVerificationResult.Invalid("El archivo final no tiene ninguna pista de audio.");
        }

        if (requireVideoStream && !streams.Any(s => s.CodecType == "video"))
        {
            return MediaVerificationResult.Invalid("El archivo final no tiene ninguna pista de video.");
        }

        var actual = TimeSpan.FromSeconds(seconds);
        if (expectedDuration > TimeSpan.Zero)
        {
            var tolerance = TimeSpan.FromSeconds(Math.Max(2, expectedDuration.TotalSeconds * 0.05));
            if (actual < expectedDuration - tolerance || actual > expectedDuration + tolerance)
            {
                return MediaVerificationResult.Invalid(
                    $"Duración inesperada: se esperaban ~{expectedDuration:mm\\:ss} y el archivo final mide {actual:mm\\:ss}.");
            }
        }

        return MediaVerificationResult.Valid(actual);
    }
}
