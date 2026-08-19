namespace DownloadYou.Infrastructure.MediaProcessing;

internal sealed record FfprobeOutput(FfprobeFormat? Format, List<FfprobeStream>? Streams);

internal sealed record FfprobeFormat(string? Duration);

internal sealed record FfprobeStream(string? CodecType);
