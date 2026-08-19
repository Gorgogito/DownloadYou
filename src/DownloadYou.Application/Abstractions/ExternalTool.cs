namespace DownloadYou.Application.Abstractions;

public enum ExternalTool
{
    YtDlp,
    FfMpeg,
    FfProbe,

    /// <summary>
    /// Runtime de JavaScript que yt-dlp necesita para resolver los desafíos de firma de
    /// YouTube (deprecó la extracción sin él). Opcional en <see cref="IExternalToolLocator"/>:
    /// si no está, YtDlpVideoSource sigue funcionando en modo degradado (yt-dlp emite su
    /// propia advertencia y puede que falten algunos formatos).
    /// </summary>
    Deno
}
