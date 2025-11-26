namespace Aiva.Admin.Api.UseCases.Storages.List;

public record ListStoragesQuery(int? Page = 1, int? PerPage = Constants.DEFAULT_PAGE_SIZE)
  : IQuery<Result<PagedResult<StorageDto>>>;
