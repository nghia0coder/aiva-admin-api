using Aiva.Admin.Api.Core.ContributorAggregate;

namespace Aiva.Admin.Api.UseCases.Contributors.Update;

public record UpdateContributorCommand(ContributorId ContributorId, ContributorName NewName) : ICommand<Result<ContributorDto>>;
