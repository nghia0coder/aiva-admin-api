namespace Aiva.Admin.Api.Core.ContributorAggregate.Specifications;

public class ContributorByIdSpec : Specification<Contributor>
{
  public ContributorByIdSpec(ContributorId contributorId) =>
    Query
        .Where(contributor => contributor.Id == contributorId);
}
