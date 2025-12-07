namespace Aiva.Admin.Api.Core.FileAggregate.Handlers;

using Events;

/// <summary>
/// Handles the FileProcessingCompletedEvent domain event.
/// This handler is responsible for logging and any side effects when file processing completes.
/// </summary>
public class FileProcessingCompletedHandler(
    ILogger<FileProcessingCompletedHandler> logger)
    : INotificationHandler<FileProcessingCompletedEvent>
{
  public ValueTask Handle(FileProcessingCompletedEvent domainEvent, CancellationToken cancellationToken)
  {
    if (domainEvent.IsSuccess)
    {
      logger.LogInformation(
          "File processing completed successfully - FileId: {FileId}, MetadataId: {MetadataId}, Duration: {Duration}ms",
          domainEvent.FileId,
          domainEvent.MetadataId,
          (DateTime.UtcNow - domainEvent.DateOccurred).TotalMilliseconds);

      // Future enhancements:
      // - Trigger embedding generation
      // - Update search index
      // - Send success notification
    }
    else
    {
      logger.LogWarning(
          "File processing failed - FileId: {FileId}, MetadataId: {MetadataId}, Error: {ErrorMessage}",
          domainEvent.FileId,
          domainEvent.MetadataId,
          domainEvent.ErrorMessage);

      // Future enhancements:
      // - Send alert notification
      // - Queue for retry
      // - Update monitoring dashboard
    }

    return ValueTask.CompletedTask;
  }
}
