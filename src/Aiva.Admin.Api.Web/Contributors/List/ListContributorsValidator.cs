using FluentValidation;

namespace Aiva.Admin.Api.Web.Contributors.List;

public sealed class ListContributorsValidator : Validator<ListContributorsRequest>
{
  public ListContributorsValidator()
  {
    RuleFor(x => x.Page)
      .GreaterThanOrEqualTo(1)
      .WithMessage("page must be >= 1");

    RuleFor(x => x.PerPage)
      .InclusiveBetween(1, UseCases.Constants.MAX_PAGE_SIZE)
      .WithMessage($"per_page must be between 1 and {UseCases.Constants.MAX_PAGE_SIZE}");
  }
}
