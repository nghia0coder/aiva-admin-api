namespace Aiva.Admin.Api.Web.Storages.List;

using UseCases;

public sealed record ListStorageResponse
  : PagedResult<StorageRecord>
{
  public ListStorageResponse(
    IReadOnlyList<StorageRecord> items,
    int page,
    int perPage,
    int totalCount,
    int totalPages)
    : base(items, page, perPage, totalCount, totalPages)
  {
  }
}
