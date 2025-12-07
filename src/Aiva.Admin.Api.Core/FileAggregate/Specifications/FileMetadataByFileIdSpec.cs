namespace Aiva.Admin.Api.Core.FileAggregate.Specifications;

/// <summary>
/// Specification to get FileMetadata by FileId
/// </summary>
public sealed class FileMetadataByFileIdSpec : Specification<FileMetadata>, ISingleResultSpecification<FileMetadata>
{
  public FileMetadataByFileIdSpec(FileId fileId)
  {
    Query.Where(m => m.FileId == fileId);
  }
}
