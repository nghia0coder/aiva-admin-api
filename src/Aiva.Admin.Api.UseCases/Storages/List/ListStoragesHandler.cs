namespace Aiva.Admin.Api.UseCases.Storages.List;

public class ListStoragesHandler : IQueryHandler<ListStoragesQuery, Result<PagedResult<StorageDto>>>
{
  private readonly IListStoragesQueryService _query;

  public ListStoragesHandler(IListStoragesQueryService query)
  {
    _query = query;
  }

  public async ValueTask<Result<PagedResult<StorageDto>>> Handle(ListStoragesQuery request,
                                                                     CancellationToken cancellationToken)
  {

    var result = await _query.ListAsync(request.Page ?? 1, request.PerPage ?? Constants.DEFAULT_PAGE_SIZE);

    return Result.Success(result);
  }
}
