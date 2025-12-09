namespace Aiva.Admin.Api.Core.Interfaces;

using FileAggregate;

/// <summary>
/// Queue interface for file processing jobs
/// </summary>
public interface IFileProcessingQueue
{
  /// <summary>
  /// Enqueues a file for processing
  /// </summary>
  ValueTask EnqueueAsync(FileId fileId, CancellationToken cancellationToken = default);

  /// <summary>
  /// Dequeues the next file for processing
  /// </summary>
  ValueTask<FileId?> DequeueAsync(CancellationToken cancellationToken = default);

  /// <summary>
  /// Gets the current queue count
  /// </summary>
  int Count { get; }
}
