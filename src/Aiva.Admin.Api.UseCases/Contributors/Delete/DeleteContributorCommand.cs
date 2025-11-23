using Aiva.Admin.Api.Core.ContributorAggregate;

namespace Aiva.Admin.Api.UseCases.Contributors.Delete;

public record DeleteContributorCommand(ContributorId ContributorId) : ICommand<Result>;
