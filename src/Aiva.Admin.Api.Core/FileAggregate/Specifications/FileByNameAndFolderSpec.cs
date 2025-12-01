namespace Aiva.Admin.Api.Core.FileAggregate.Specifications;

using FolderAggregate;

public sealed class FileByNameAndFolderSpec : Specification<File>, ISingleResultSpecification<File>
{
  public FileByNameAndFolderSpec(FolderId folderId, string originalFileName)
  {
    Query
        .Where(f => f.FolderId == folderId &&
                    f.OriginalFileName == FileName.From(originalFileName));
  }
}
