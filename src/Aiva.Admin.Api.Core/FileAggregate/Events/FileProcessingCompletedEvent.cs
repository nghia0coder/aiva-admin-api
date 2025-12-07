namespace Aiva.Admin.Api.Core.FileAggregate.Events;

/// <summary>
/// Domain event dispatched when file processing completes (success or failure)
/// </summary>
public sealed class FileProcessingCompletedEvent(
    FileId fileId,
    FileMetadataId metadataId,
    bool isSuccess,
    string? errorMessage = null) : DomainEventBase
{
  public FileId FileId { get; init; } = fileId;
  public FileMetadataId MetadataId { get; init; } = metadataId;
  public bool IsSuccess { get; init; } = isSuccess;
  public string? ErrorMessage { get; init; } = errorMessage;
}
