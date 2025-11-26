using FluentValidation;

namespace Aiva.Admin.Api.Web.Storages.List;

public sealed class ListStoragesValidator : Validator<ListStoragesRequest>
{
  public ListStoragesValidator()
  {
    RuleFor(x => x.Page)
      .GreaterThanOrEqualTo(1)
      .WithMessage("page must be >= 1");

    RuleFor(x => x.PerPage)
      .InclusiveBetween(1, UseCases.Constants.MAX_PAGE_SIZE)
      .WithMessage($"per_page must be between 1 and {UseCases.Constants.MAX_PAGE_SIZE}");
  }
}
