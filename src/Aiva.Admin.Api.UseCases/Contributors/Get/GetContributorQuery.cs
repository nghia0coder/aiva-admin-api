using Aiva.Admin.Api.Core.ContributorAggregate;

namespace Aiva.Admin.Api.UseCases.Contributors.Get;

public record GetContributorQuery(ContributorId ContributorId) : IQuery<Result<ContributorDto>>;
