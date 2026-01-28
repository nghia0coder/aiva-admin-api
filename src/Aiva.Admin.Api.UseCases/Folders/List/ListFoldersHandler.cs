namespace Aiva.Admin.Api.UseCases.Folders.List;

using Core.StorageAggregate;

public class ListFoldersHandler(
  IListFoldersQueryService queryService,
  IReadRepository<Storage> storageRepository) : IQueryHandler<ListFoldersQuery, Result<ListFoldersResult>>
{
  public async ValueTask<Result<ListFoldersResult>> Handle(ListFoldersQuery query, CancellationToken cancellationToken)
  {
    // Validate storage exists if storageId is provided
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

    var result = await queryService.GetFoldersAsync(
      query.StorageId,
      query.ParentFolderId,
      query.Page,
      query.PageSize,
      query.SearchTerm,
      query.IncludeChildren,
      cancellationToken);

    return Result.Success(result);
  }
}
