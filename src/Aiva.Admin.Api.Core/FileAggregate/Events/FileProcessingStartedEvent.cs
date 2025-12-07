namespace Aiva.Admin.Api.Core.FileAggregate.Events;

/// <summary>
/// Domain event dispatched when file processing starts
/// </summary>
public sealed class FileProcessingStartedEvent(FileId fileId, FileMetadataId metadataId) : DomainEventBase
{
  public FileId FileId { get; init; } = fileId;
  public FileMetadataId MetadataId { get; init; } = metadataId;
}
