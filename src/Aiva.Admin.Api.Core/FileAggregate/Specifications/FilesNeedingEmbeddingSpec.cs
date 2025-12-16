namespace Aiva.Admin.Api.Core.FileAggregate.Specifications;

public sealed class FilesNeedingEmbeddingSpec : Specification<FileMetadata>
{
  public FilesNeedingEmbeddingSpec(int take = 10)
  {
    Query
        .Where(m => m.Status == FileProcessingStatus.Completed && !m.IsEmbedded)
        .OrderBy(m => m.ProcessingCompletedAt)
        .Take(take);
  }
}
