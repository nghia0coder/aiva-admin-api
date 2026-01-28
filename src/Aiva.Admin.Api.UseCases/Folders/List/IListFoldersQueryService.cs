namespace Aiva.Admin.Api.UseCases.Folders.List;

using Core.StorageAggregate;

public interface IListFoldersQueryService
{
  Task<ListFoldersResult> GetFoldersAsync(
    StorageId? storageId,
    int? parentFolderId,
    int page,
    int pageSize,
    string? searchTerm = null,
    bool includeChildren = false,
    CancellationToken cancellationToken = default);
}
