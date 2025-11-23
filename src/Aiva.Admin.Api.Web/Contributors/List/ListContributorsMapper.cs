using Aiva.Admin.Api.UseCases.Contributors;

namespace Aiva.Admin.Api.Web.Contributors.List;

public sealed class ListContributorsMapper
  : Mapper<ListContributorsRequest, ListContributorResponse, UseCases.PagedResult<ContributorDto>>
{
  public override ListContributorResponse FromEntity(UseCases.PagedResult<ContributorDto> e)
  {
    var items = e.Items
      .Select(c => new ContributorRecord(c.Id.Value, c.Name.Value, c.PhoneNumber.ToString()))
      .ToList();

    return new ListContributorResponse(items, e.Page, e.PerPage, e.TotalCount, e.TotalPages);
  }
}
