using DownloadYou.Application.Abstractions;
using DownloadYou.Domain.Entities;
using DownloadYou.Domain.Enums;

namespace DownloadYou.Application.Services;

/// <summary>
/// Ejecuta la etapa de descarga de un job (sin cola todavía — Fase 6). Si el formato
/// elegido ya está muxed, mueve el archivo final directamente y completa el job; si
/// hace falta unir video+audio o convertir a MP3, deja el job en estado Converting
/// con los archivos descargados listos para que la Fase 4 los procese.
/// </summary>
public sealed class DownloadService(IVideoSource videoSource)
{
    public async Task RunAsync(
        DownloadJob job,
        ExistingFileBehavior existingFileBehavior = ExistingFileBehavior.Rename,
        Action<string>? onOutputLine = null,
        Action? onProgressChanged = null,
        CancellationToken cancellationToken = default)
    {
        job.Status = JobStatus.Downloading;
        job.ProgressPercent = 0;

        var stagingDir = Path.Combine(Path.GetTempPath(), "DownloadYou", job.Id.ToString("N"));
        Directory.CreateDirectory(stagingDir);

        try
        {
            var streamCount = job.PairedAudioFormat is null ? 1 : 2;

            var primaryPath = Path.Combine(stagingDir, $"primary.{job.SelectedFormat.Container}");
            await videoSource.DownloadAsync(
                job.MediaInfo.Url,
                job.SelectedFormat.FormatId,
                primaryPath,
                update =>
                {
                    ApplyProgress(job, update, streamIndex: 0, streamCount);
                    onProgressChanged?.Invoke();
                },
                onOutputLine,
                cancellationToken);
            job.PrimaryFilePath = primaryPath;

            if (job.PairedAudioFormat is not null)
            {
                var audioPath = Path.Combine(stagingDir, $"audio.{job.PairedAudioFormat.Container}");
                await videoSource.DownloadAsync(
                    job.MediaInfo.Url,
                    job.PairedAudioFormat.FormatId,
                    audioPath,
                    update =>
                    {
                        ApplyProgress(job, update, streamIndex: 1, streamCount);
                        onProgressChanged?.Invoke();
                    },
                    onOutputLine,
                    cancellationToken);
                job.PairedAudioFilePath = audioPath;
            }

            job.ProgressPercent = 100;

            if (job.RequiresConversion)
            {
                // La Fase 4 retoma desde aquí: unir/convertir PrimaryFilePath (+ PairedAudioFilePath) con FFmpeg.
                job.Status = JobStatus.Converting;
            }
            else
            {
                job.OutputFilePath = MoveToFinalDestination(job, existingFileBehavior);
                job.Status = JobStatus.Completed;
                job.CompletedAt = DateTimeOffset.UtcNow;
                TempCleanup.TryDeleteDirectory(stagingDir);
            }
        }
        catch (OperationCanceledException)
        {
            job.Status = JobStatus.Canceled;
            TempCleanup.TryDeleteDirectory(stagingDir);
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

    private static string MoveToFinalDestination(DownloadJob job, ExistingFileBehavior behavior)
    {
        Directory.CreateDirectory(job.TargetDirectory);

        var fileName = FileNameTemplateEngine.Resolve(
            job.FileNameTemplate,
            job.MediaInfo.Title,
            job.MediaInfo.Author,
            job.SelectedFormat.DisplayLabel,
            job.SelectedFormat.Container);

        var destination = DestinationPathResolver.ResolveCollision(Path.Combine(job.TargetDirectory, fileName), behavior);
        File.Move(job.PrimaryFilePath!, destination, overwrite: behavior == ExistingFileBehavior.Overwrite);
        return destination;
    }

    private static void ApplyProgress(DownloadJob job, DownloadProgressUpdate update, int streamIndex, int streamCount)
    {
        var span = 100.0 / streamCount;

        job.SpeedBytesPerSecond = update.SpeedBytesPerSecond;
        job.DownloadedBytes = update.DownloadedBytes;
        job.TotalBytes = update.TotalBytes;
        job.Eta = update.Eta;

        if (update.PercentComplete is { } percent)
        {
            job.ProgressPercent = streamIndex * span + percent * span / 100.0;
        }
    }
}
