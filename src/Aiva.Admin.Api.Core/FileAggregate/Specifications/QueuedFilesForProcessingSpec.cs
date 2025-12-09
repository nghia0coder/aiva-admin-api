namespace Aiva.Admin.Api.Core.FileAggregate.Specifications;

/// <summary>
/// Specification to get files that are queued and waiting for processing
/// </summary>
public sealed class QueuedFilesForProcessingSpec : Specification<FileMetadata>
{
  public QueuedFilesForProcessingSpec(int take)
  {
    Query
        .Where(m => m.Status == FileProcessingStatus.Queued)
        .OrderBy(m => m.QueuedAt)
        .Take(take);
  }
}
