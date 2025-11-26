namespace Aiva.Admin.Api.UseCases.Storages.List;

/// <summary>
/// Represents a service that will actually fetch the necessary data
/// Typically implemented in Infrastructure
/// </summary>
public interface IListStoragesQueryService
{
  Task<PagedResult<StorageDto>> ListAsync(int page, int perPage);
}
