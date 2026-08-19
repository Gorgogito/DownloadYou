namespace DownloadYou.Infrastructure.MediaProcessing;

/// <summary>
/// FFmpeg reparte cada actualización de -progress pipe:1 en varias líneas "clave=valor"
/// y cierra el bloque con una línea "progress=continue" o "progress=end" — no es una
/// línea autocontenida como el --progress-template de yt-dlp, hay que acumular.
/// </summary>
public sealed class FfmpegProgressAccumulator
{
    private readonly Dictionary<string, string> _fields = [];

    /// <summary>Tiempo de salida procesado hasta ahora, o null si la línea no cierra un bloque.</summary>
    public TimeSpan? Ingest(string line)
    {
        var separatorIndex = line.IndexOf('=');
        if (separatorIndex <= 0)
        {
            return null;
        }

        var key = line[..separatorIndex].Trim();
        var value = line[(separatorIndex + 1)..].Trim();

        if (key != "progress")
        {
            _fields[key] = value;
            return null;
        }

        var elapsed = TryGetElapsed();
        _fields.Clear();
        return elapsed;
    }

    private TimeSpan? TryGetElapsed()
    {
        // Pese al nombre, out_time_ms viene en microsegundos igual que out_time_us
        // (comportamiento verificado con el binario real, no solo documentado).
        if (_fields.TryGetValue("out_time_us", out var us) && long.TryParse(us, out var microseconds) && microseconds >= 0)
        {
            return TimeSpan.FromMicroseconds(microseconds);
        }

        return null;
    }
}
