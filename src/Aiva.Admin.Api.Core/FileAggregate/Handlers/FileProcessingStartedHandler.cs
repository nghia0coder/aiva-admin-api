namespace Aiva.Admin.Api.Core.FileAggregate.Handlers;

using Events;

/// <summary>
/// Handles the FileProcessingStartedEvent domain event.
/// This handler is responsible for logging and any side effects when file processing begins.
/// </summary>
public class FileProcessingStartedHandler(
    ILogger<FileProcessingStartedHandler> logger)
    : INotificationHandler<FileProcessingStartedEvent>
{
  public ValueTask Handle(FileProcessingStartedEvent domainEvent, CancellationToken cancellationToken)
  {
    logger.LogInformation(
        "File processing started - FileId: {FileId}, MetadataId: {MetadataId}, Timestamp: {Timestamp}",
        domainEvent.FileId,
        domainEvent.MetadataId,
        domainEvent.DateOccurred);

    // Future enhancements:
    // - Send notification to monitoring system
    // - Update real-time dashboard
    // - Track processing metrics

    return ValueTask.CompletedTask;
  }
}
