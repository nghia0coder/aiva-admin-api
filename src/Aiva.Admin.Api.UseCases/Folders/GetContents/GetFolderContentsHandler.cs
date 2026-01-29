namespace Aiva.Admin.Api.UseCases.Folders.GetContents;

using Core.FolderAggregate;
using Core.StorageAggregate;

public class GetFolderContentsHandler(
  IGetFolderContentsQueryService queryService,
  IReadRepository<Folder> folderRepository,
  IReadRepository<Storage> storageRepository)
  : IQueryHandler<GetFolderContentsQuery, Result<GetFolderContentsResult>>
{
  public async ValueTask<Result<GetFolderContentsResult>> Handle(
    GetFolderContentsQuery query,
    CancellationToken cancellationToken)
  {
    // Validate that either FolderId or StorageId is provided
    if (!query.FolderId.HasValue && !query.StorageId.HasValue)
    {
      return Result.Invalid(new ValidationError("Either FolderId or StorageId must be provided"));
    }

    // If FolderId is provided, validate it exists
    if (query.FolderId.HasValue)
    {
      var folder = await folderRepository.GetByIdAsync(query.FolderId.Value, cancellationToken);
      if (folder is null)
      {
        return Result.NotFound($"Folder with ID {query.FolderId.Value.Value} not found");
      }
    }

    // If StorageId is provided, validate it exists
    if (query.StorageId.HasValue)
    {
      var storage = await storageRepository.GetByIdAsync(query.StorageId.Value, cancellationToken);
      if (storage is null)
      {
        return Result.NotFound($"Storage with ID {query.StorageId.Value.Value} not found");
      }
    }

    // Validate pagination parameters
    if (query.Page < 1)
    {
      return Result.Invalid(new ValidationError("Page must be greater than 0"));
    }

    if (query.PageSize < 1 || query.PageSize > 100)
    {
      return Result.Invalid(new ValidationError("PageSize must be between 1 and 100"));
    }

    var result = await queryService.GetContentsAsync(
      query.FolderId,
      query.StorageId,
      query.SearchQuery,
      query.SortBy,
      query.SortOrder,
      query.Page,
      query.PageSize,
      cancellationToken);

    return Result.Success(result);
  }
}
