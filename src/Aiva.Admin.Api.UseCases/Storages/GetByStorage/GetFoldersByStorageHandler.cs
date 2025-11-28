namespace Aiva.Admin.Api.UseCases.Folders.GetByStorage;

using Core.StorageAggregate;

public class GetFoldersByStorageHandler(
  IGetFoldersByStorageQueryService queryService,
  IReadRepository<Storage> storageRepository) : IQueryHandler<GetFoldersByStorageQuery, Result<FoldersByStorageResult>>
{
  public async ValueTask<Result<FoldersByStorageResult>> Handle(GetFoldersByStorageQuery query, CancellationToken ct)
  {
    // Validate storage exists
    var storage = await storageRepository.GetByIdAsync(query.StorageId, ct);
    if (storage is null)
    {
      return Result.NotFound($"Storage with ID {query.StorageId.Value} not found");
    }

    if (query.AsTree)
    {
      var tree = await queryService.GetTreeAsync(query.StorageId, ct);
      return new FoldersByStorageResult(query.StorageId, tree, null);
    }

    var flatList = await queryService.GetFlatListAsync(query.StorageId, ct);
    return new FoldersByStorageResult(query.StorageId, null, flatList);
  }
}
