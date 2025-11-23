using Aiva.Admin.Api.Core.ContributorAggregate;

namespace Aiva.Admin.Api.UseCases.Contributors;
public record ContributorDto(ContributorId Id, ContributorName Name, PhoneNumber PhoneNumber);
