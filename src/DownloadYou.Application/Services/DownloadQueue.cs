using System.Collections.Concurrent;
using System.Threading.Channels;
using DownloadYou.Domain.Entities;
using DownloadYou.Domain.Enums;

namespace DownloadYou.Application.Services;

/// <summary>
/// Cola de descargas con concurrencia acotada: un <see cref="Channel{T}"/> alimenta un
/// número fijo de workers, cada uno ejecutando el pipeline completo (descarga → si hace
/// falta, conversión) de un job por vez. Pausar y cancelar comparten el mismo mecanismo
/// de cancelación cooperativa; la diferencia es solo si se conserva el trabajo parcial
/// (.part de yt-dlp, streams ya descargados) para poder reanudar después.
/// </summary>
public sealed class DownloadQueue
{
    private readonly DownloadService _downloadService;
    private readonly ConversionService _conversionService;
    private readonly Channel<DownloadJob> _channel = Channel.CreateUnbounded<DownloadJob>();
    private readonly ConcurrentDictionary<Guid, CancellationTokenSource> _tokens = new();
    private readonly ConcurrentDictionary<Guid, bool> _pausing = new();

    public event Action<DownloadJob>? JobEnqueued;
    public event Action<DownloadJob>? JobUpdated;

    public DownloadQueue(DownloadService downloadService, ConversionService conversionService, int maxConcurrency = 3)
    {
        _downloadService = downloadService;
        _conversionService = conversionService;

        for (var i = 0; i < Math.Max(1, maxConcurrency); i++)
        {
            _ = WorkerLoopAsync();
        }
    }

    public void Enqueue(DownloadJob job)
    {
        job.Status = JobStatus.Queued;
        _tokens[job.Id] = new CancellationTokenSource();
        JobEnqueued?.Invoke(job);
        _channel.Writer.TryWrite(job);
    }

    /// <summary>Cancela definitivamente: al terminar de detenerse, borra los archivos parciales.</summary>
    public void Cancel(DownloadJob job)
    {
        _pausing[job.Id] = false;
        RequestStop(job.Id);
    }

    /// <summary>Detiene el job cooperativamente conservando lo ya descargado para poder reanudar.</summary>
    public void Pause(DownloadJob job)
    {
        _pausing[job.Id] = true;
        RequestStop(job.Id);
    }

    /// <summary>Vuelve a encolar un job pausado; yt-dlp reanuda cada stream desde su .part.</summary>
    public void Resume(DownloadJob job)
    {
        if (job.Status != JobStatus.Paused)
        {
            return;
        }

        _pausing.TryRemove(job.Id, out _);
        _tokens[job.Id] = new CancellationTokenSource();
        job.Status = JobStatus.Queued;
        JobUpdated?.Invoke(job);
        _channel.Writer.TryWrite(job);
    }

    private void RequestStop(Guid jobId)
    {
        if (_tokens.TryGetValue(jobId, out var cts))
        {
            cts.Cancel();
        }
    }

    private async Task WorkerLoopAsync()
    {
        await foreach (var job in _channel.Reader.ReadAllAsync())
        {
            try
            {
                await ProcessJobAsync(job);
            }
            catch (Exception ex)
            {
                // Red de seguridad: DownloadService/ConversionService ya capturan sus propias
                // excepciones, pero un fallo inesperado aquí no debe tumbar el worker para
                // siempre (aislamiento de fallos — ver §10 del documento de arquitectura).
                job.Status = JobStatus.Failed;
                job.ErrorMessage = ex.Message;
            }
            finally
            {
                _tokens.TryRemove(job.Id, out _);
                JobUpdated?.Invoke(job);
            }
        }
    }

    private async Task ProcessJobAsync(DownloadJob job)
    {
        var token = _tokens.GetOrAdd(job.Id, _ => new CancellationTokenSource()).Token;

        void OnProgress() => JobUpdated?.Invoke(job);

        await _downloadService.RunAsync(job, onProgressChanged: OnProgress, cancellationToken: token);

        if (job.Status == JobStatus.Converting)
        {
            await _conversionService.RunAsync(job, onProgressChanged: OnProgress, cancellationToken: token);
        }

        if (job.Status == JobStatus.Canceled)
        {
            if (_pausing.TryGetValue(job.Id, out var isPause) && isPause)
            {
                job.Status = JobStatus.Paused;
            }
            else
            {
                TempCleanup.TryDeleteDirectory(JobStagingPath.For(job.Id));
            }
        }
    }
}
