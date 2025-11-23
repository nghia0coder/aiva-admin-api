using Aiva.Admin.Api.Core.ContributorAggregate;
using FluentValidation;

namespace Aiva.Admin.Api.Web.Contributors.Create;

public class CreateContributorValidator : Validator<CreateContributorRequest>
{
  public CreateContributorValidator()
  {
    RuleFor(x => x.Name)
      .NotEmpty()
      .WithMessage("Name is required.")
      .MinimumLength(2)
      .MaximumLength(ContributorName.MaxLength);
  }
}
