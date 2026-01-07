using FluentValidation;

namespace Aiva.Admin.Api.Web.Conversations.ListMine;

public class ListMyConversationsValidator : Validator<ListMyConversationsRequest>
{
  public ListMyConversationsValidator()
  {
    RuleFor(x => x.Page)
        .GreaterThanOrEqualTo(1)
        .WithMessage("Page must be at least 1.");

    RuleFor(x => x.PerPage)
        .InclusiveBetween(1, UseCases.Constants.MAX_PAGE_SIZE)
        .WithMessage($"Per page must be between 1 and {UseCases.Constants.MAX_PAGE_SIZE}.");
  }
}
