namespace Aiva.Admin.Api.Worker.Services;

using Aiva.Admin.Api.UseCases.Files.EmbedFile;
using Core.FileAggregate;
using Core.FileAggregate.Specifications;
using UseCases.Files.ProcessFile;

/// <summary>
/// Background service that processes queued files for text extraction.
/// Runs independently from the API, allowing horizontal scaling.
/// </summary>
public sealed class FileProcessingBackgroundService : BackgroundService
{
  private readonly IServiceScopeFactory _scopeFactory;
  private readonly ILogger<FileProcessingBackgroundService> _logger;
  private readonly WorkerConfiguration _config;

  public FileProcessingBackgroundService(
      IServiceScopeFactory scopeFactory,
      IOptions<WorkerConfiguration> config,
      ILogger<FileProcessingBackgroundService> logger)
  {
    _scopeFactory = scopeFactory;
    _config = config.Value;
    _logger = logger;
  }

  protected override async Task ExecuteAsync(CancellationToken stoppingToken)
  {
    _logger.LogInformation(
        "File Processing Worker started. Polling every {Interval}s, BatchSize: {BatchSize}, MaxConcurrency: {MaxConcurrency}",
        _config.PollingIntervalSeconds,
        _config.BatchSize,
        _config.MaxConcurrency);

    if (!_config.Enabled)
    {
      _logger.LogWarning("File Processing Worker is DISABLED via configuration");
      return;
    }

    // Initial delay to allow services to start
    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

    while (!stoppingToken.IsCancellationRequested)
    {
      try
      {
        var processedCount = await ProcessQueuedFilesAsync(stoppingToken);

        if (processedCount > 0)
        {
          _logger.LogInformation("Processed {Count} files in this batch", processedCount);
        }
      }
      catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
      {
        // Graceful shutdown
        break;
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error in file processing worker loop");
      }

      await Task.Delay(TimeSpan.FromSeconds(_config.PollingIntervalSeconds), stoppingToken);
    }

    _logger.LogInformation("File Processing Worker stopped");
  }

  private async Task<int> ProcessQueuedFilesAsync(CancellationToken cancellationToken)
  {
    using var scope = _scopeFactory.CreateScope();

    var metadataRepository = scope.ServiceProvider
        .GetRequiredService<IReadRepository<FileMetadata>>();
    var mediator = scope.ServiceProvider
        .GetRequiredService<IMediator>();

    // Get queued files
    var queuedFilesSpec = new QueuedFilesForProcessingSpec(_config.BatchSize);
    var queuedFiles = await metadataRepository.ListAsync(queuedFilesSpec, cancellationToken);

    if (queuedFiles.Count == 0)
    {
      return 0;
    }

    _logger.LogInformation(
        "Found {Count} queued files for processing",
        queuedFiles.Count);

    // Process with limited concurrency
    using var semaphore = new SemaphoreSlim(_config.MaxConcurrency);
    var processedCount = 0;

    var tasks = queuedFiles.Select(async metadata =>
    {
      await semaphore.WaitAsync(cancellationToken);
      try
      {
        // Step 1: Extract text
        var extractCommand = new ProcessFileCommand(metadata.FileId);
        var extractResult = await mediator.Send(extractCommand, cancellationToken);

        if (!extractResult.IsSuccess)
        {
          _logger.LogWarning("Extraction failed for {FileId}: {Errors}",
              metadata.FileId.Value, string.Join(", ", extractResult.Errors));
          return;
        }

        _logger.LogInformation("Extracted {FileId}: {WordCount} words",
            metadata.FileId.Value, extractResult.Value.WordCount);

        // Step 2: Embed (only if extraction succeeded)
        var embedCommand = new EmbedFileCommand(metadata.FileId);
        var embedResult = await mediator.Send(embedCommand, cancellationToken);

        if (embedResult.IsSuccess && embedResult.Value.Success)
        {
          Interlocked.Increment(ref processedCount);
          _logger.LogInformation("Embedded {FileId}: {ChunkCount} chunks",
              metadata.FileId.Value, embedResult.Value.ChunkCount);
        }
        else
        {
          _logger.LogWarning("Embedding failed for {FileId}: {Error}",
              metadata.FileId.Value,
              embedResult.IsSuccess ? embedResult.Value.ErrorMessage : string.Join(", ", embedResult.Errors));
        }
      }
      catch (Exception ex)
      {
        _logger.LogError(ex,
            "Exception while processing file {FileId}",
            metadata.FileId.Value);
      }
      finally
      {
        semaphore.Release();
      }
    });

    await Task.WhenAll(tasks);

    return processedCount;
  }
}
