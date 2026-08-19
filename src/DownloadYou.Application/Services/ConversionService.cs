using DownloadYou.Application.Abstractions;
using DownloadYou.Domain.Entities;
using DownloadYou.Domain.Enums;

namespace DownloadYou.Application.Services;

/// <summary>
/// Continúa un DownloadJob que quedó en estado Converting (Fase 3): une video+audio o
/// convierte a MP3 con FFmpeg, mueve el resultado a la carpeta de destino y lo verifica
/// con ffprobe antes de marcarlo Completed.
/// </summary>
public sealed class ConversionService(IMediaProcessor mediaProcessor)
{
    public async Task RunAsync(
        DownloadJob job,
        Action<string>? onOutputLine = null,
        Action? onProgressChanged = null,
        CancellationToken cancellationToken = default)
    {
        if (job.Status != JobStatus.Converting)
        {
            throw new InvalidOperationException($"El job no está listo para convertir (estado actual: {job.Status}).");
        }

        var stagingDir = Path.GetDirectoryName(job.PrimaryFilePath)!;

        try
        {
            var (convertedPath, requireVideoStream) = job.Kind == DownloadKind.AudioMp3
                ? await ExtractAudioAsync(job, stagingDir, onOutputLine, onProgressChanged, cancellationToken)
                : await MuxAsync(job, stagingDir, onOutputLine, onProgressChanged, cancellationToken);

            job.Status = JobStatus.Verifying;
            onProgressChanged?.Invoke();

            var outputExt = Path.GetExtension(convertedPath).TrimStart('.');
            var fileName = FileNameTemplateEngine.Resolve(
                job.FileNameTemplate, job.MediaInfo.Title, job.MediaInfo.Author, job.SelectedFormat.DisplayLabel, outputExt);

            Directory.CreateDirectory(job.TargetDirectory);
            var destination = DestinationPathResolver.ResolveCollision(Path.Combine(job.TargetDirectory, fileName), job.ExistingFileBehavior);
            File.Move(convertedPath, destination, overwrite: job.ExistingFileBehavior == ExistingFileBehavior.Overwrite);

            var verification = await mediaProcessor.VerifyAsync(
                destination, job.MediaInfo.Duration, requireVideoStream, onOutputLine, cancellationToken);

            if (!verification.IsValid)
            {
                TempCleanup.TryDeleteFile(destination);
                job.Status = JobStatus.Failed;
                job.ErrorMessage = verification.Error;
                return;
            }

            job.OutputFilePath = destination;
            job.Status = JobStatus.Completed;
            job.CompletedAt = DateTimeOffset.UtcNow;
            TempCleanup.TryDeleteDirectory(stagingDir);
        }
        catch (OperationCanceledException)
        {
            // Conserva PrimaryFilePath/PairedAudioFilePath: un Resume debe poder rehacer
            // solo el paso de conversión (FFmpeg no soporta reanudar un mux a medias) sin
            // volver a descargar los streams de origen.
            job.Status = JobStatus.Canceled;
        }
        catch (Exception ex)
        {
            job.Status = JobStatus.Failed;
            job.ErrorMessage = ex.Message;
            TempCleanup.TryDeleteDirectory(stagingDir);
        }
        finally
        {
            onProgressChanged?.Invoke();
        }
    }

    private async Task<(string ConvertedPath, bool RequireVideoStream)> ExtractAudioAsync(
        DownloadJob job, string stagingDir, Action<string>? onOutputLine, Action? onProgressChanged, CancellationToken cancellationToken)
    {
        // Nunca anunciar una calidad de MP3 superior a la que realmente ofrece la fuente (§2).
        var effectiveBitrate = Math.Min(job.TargetAudioBitrateKbps, job.SelectedFormat.AudioBitrateKbps ?? job.TargetAudioBitrateKbps);
        var convertedPath = Path.Combine(stagingDir, "converted.mp3");

        await mediaProcessor.ExtractAudioAsync(
            job.PrimaryFilePath!,
            convertedPath,
            effectiveBitrate,
            _ => onProgressChanged?.Invoke(),
            onOutputLine,
            cancellationToken);

        return (convertedPath, false);
    }

    private async Task<(string ConvertedPath, bool RequireVideoStream)> MuxAsync(
        DownloadJob job, string stagingDir, Action<string>? onOutputLine, Action? onProgressChanged, CancellationToken cancellationToken)
    {
        if (job.PairedAudioFormat is null)
        {
            throw new InvalidOperationException("El job está marcado para convertir pero no tiene un audio emparejado ni es de tipo AudioMp3.");
        }

        var convertedPath = Path.Combine(stagingDir, $"converted.{job.SelectedFormat.Container}");

        await mediaProcessor.MuxAsync(
            job.PrimaryFilePath!,
            job.PairedAudioFilePath!,
            convertedPath,
            _ => onProgressChanged?.Invoke(),
            onOutputLine,
            cancellationToken);

        return (convertedPath, true);
    }
}
