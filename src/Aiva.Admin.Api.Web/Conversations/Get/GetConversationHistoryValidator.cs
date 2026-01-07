namespace Aiva.Admin.Api.Web.Conversations.Get;

using FluentValidation;

public class GetConversationHistoryValidator : Validator<GetConversationHistoryRequest>
{
  public GetConversationHistoryValidator()
  {
    RuleFor(x => x.Id)
        .NotEmpty()
        .WithMessage("Conversation ID is required")
        .WithMessage("Conversation ID cannot be empty");
  }
}
