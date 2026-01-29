namespace Aiva.Admin.Api.UseCases.Folders.GetContents;

using Core.FolderAggregate;
using Core.StorageAggregate;

public interface IGetFolderContentsQueryService
{
  Task<GetFolderContentsResult> GetContentsAsync(
    FolderId? folderId,
    StorageId? storageId,
    string? searchQuery,
    string sortBy,
    string sortOrder,
    int page,
    int pageSize,
    CancellationToken cancellationToken = default);
}
