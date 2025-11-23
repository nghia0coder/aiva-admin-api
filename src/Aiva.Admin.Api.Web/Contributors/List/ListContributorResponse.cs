namespace Aiva.Admin.Api.Web.Contributors.List;

public sealed record ListContributorResponse
  : UseCases.PagedResult<ContributorRecord>
{
  public ListContributorResponse(
    IReadOnlyList<ContributorRecord> items,
    int page,
    int perPage,
    int totalCount,
    int totalPages)
    : base(items, page, perPage, totalCount, totalPages)
  {
  }
}
