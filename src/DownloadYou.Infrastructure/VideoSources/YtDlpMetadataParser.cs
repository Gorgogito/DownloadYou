using System.Text.Json;
using DownloadYou.Domain.Entities;
using DownloadYou.Domain.Enums;

namespace DownloadYou.Infrastructure.VideoSources;

public static class YtDlpMetadataParser
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true
    };

    /// <exception cref="InvalidOperationException">El JSON no tiene la forma mínima esperada de yt-dlp.</exception>
    public static MediaInfo Parse(string json, string requestedUrl)
    {
        YtDlpVideoJson? parsed;
        try
        {
            parsed = JsonSerializer.Deserialize<YtDlpVideoJson>(json, Options);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException("yt-dlp devolvió una respuesta que no se pudo interpretar como JSON.", ex);
        }

        if (parsed is null || string.IsNullOrWhiteSpace(parsed.Id) || string.IsNullOrWhiteSpace(parsed.Title))
        {
            throw new InvalidOperationException("yt-dlp no devolvió metadatos utilizables para esta URL.");
        }

        var formats = (parsed.Formats ?? [])
            .Select(ToFormatOption)
            .Where(f => f is not null)
            .Select(f => f!)
            .ToList();

        return new MediaInfo(
            Url: requestedUrl,
            VideoId: parsed.Id,
            Title: parsed.Title,
            Author: parsed.Uploader ?? parsed.Channel ?? "Desconocido",
            Duration: TimeSpan.FromSeconds(parsed.Duration ?? 0),
            ThumbnailUrl: parsed.Thumbnail,
            AvailableFormats: formats);
    }

    private static FormatOption? ToFormatOption(YtDlpFormatJson f)
    {
        if (string.IsNullOrWhiteSpace(f.FormatId))
        {
            return null;
        }

        var hasVideo = IsPresent(f.Vcodec);
        var hasAudio = IsPresent(f.Acodec);

        // Formatos sin audio ni video (storyboards mhtml, etc.) no son descargables como contenido.
        if (!hasVideo && !hasAudio)
        {
            return null;
        }

        var kind = (hasVideo, hasAudio) switch
        {
            (true, true) => StreamKind.Muxed,
            (true, false) => StreamKind.VideoOnly,
            (false, true) => StreamKind.AudioOnly,
            _ => StreamKind.AudioOnly
        };

        var videoBitrate = hasVideo ? RoundToInt(f.Vbr ?? f.Tbr) : null;
        var audioBitrate = hasAudio ? RoundToInt(f.Abr) : null;

        return new FormatOption(
            FormatId: f.FormatId,
            Kind: kind,
            Container: f.Ext ?? "bin",
            VideoCodec: hasVideo ? f.Vcodec : null,
            AudioCodec: hasAudio ? f.Acodec : null,
            Height: hasVideo ? f.Height : null,
            Fps: hasVideo ? f.Fps : null,
            VideoBitrateKbps: videoBitrate,
            AudioBitrateKbps: audioBitrate,
            ApproxFileSizeBytes: f.Filesize ?? f.FilesizeApprox);
    }

    private static bool IsPresent(string? codec) => !string.IsNullOrWhiteSpace(codec) && codec != "none";

    private static int? RoundToInt(double? value) => value is null ? null : (int)Math.Round(value.Value);
}
