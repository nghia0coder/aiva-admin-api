namespace Aiva.Admin.Api.Web.Storages.List;

using UseCases;
using UseCases.Storages;

public sealed class ListStoragesMapper
  : Mapper<ListStoragesRequest, ListStorageResponse, PagedResult<StorageDto>>
{
  public override ListStorageResponse FromEntity(PagedResult<StorageDto> e)
  {
    var items = e.Items
      .Select(c => new StorageRecord(c.Id.Value, c.StorageName.Value, c.StorageDescription))
      .ToList();

    return new ListStorageResponse(items, e.Page, e.PerPage, e.TotalCount, e.TotalPages);
  }
}
