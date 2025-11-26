namespace Aiva.Admin.Api.Infrastructure.Data.Queries;

using UseCases;
using UseCases.Storages;
using UseCases.Storages.List;

public class ListStoragesQueryService : IListStoragesQueryService
{
  // You can use EF, Dapper, SqlClient, etc. for queries
  private readonly AppDbContext _db;

  public ListStoragesQueryService(AppDbContext db)
  {
    _db = db;
  }

  public async Task<PagedResult<StorageDto>> ListAsync(int page, int perPage)
  {
    var items = await _db.Storages
      .OrderBy(c => c.Id)
      .Skip((page - 1) * perPage)
      .Take(perPage)
      .Select(c => new StorageDto(c.Id, c.StorageName, c.StorageDescription))
      .AsNoTracking()
      .ToListAsync();

    int totalCount = await _db.Storages.CountAsync();
    int totalPages = (int)Math.Ceiling(totalCount / (double)perPage);

    return new PagedResult<StorageDto>(items, page, perPage, totalCount, totalPages);
  }
}
