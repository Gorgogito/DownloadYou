namespace DownloadYou.Application.Abstractions;

public sealed class NoCompatibleAudioStreamException(string videoId)
    : Exception($"El video '{videoId}' no tiene ningún stream de audio disponible para combinar.")
{
    public string VideoId { get; } = videoId;
}
