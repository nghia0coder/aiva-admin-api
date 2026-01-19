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
    
    // Validate Limit parameter
    RuleFor(x => x.Limit)
        .InclusiveBetween(1, 200)
        .When(x => x.Limit.HasValue)
        .WithMessage("Limit must be between 1 and 200");
    
    // Ensure only one cursor is used at a time
    RuleFor(x => x)
        .Must(x => CountActiveCursors(x) <= 1)
        .WithMessage("Only one cursor parameter (BeforeMessageId, AfterMessageId, or AroundMessageId) can be used at a time");
  }
  
  private static int CountActiveCursors(GetConversationHistoryRequest request)
  {
    int count = 0;
    if (request.BeforeMessageId.HasValue) count++;
    if (request.AfterMessageId.HasValue) count++;
    if (request.AroundMessageId.HasValue) count++;
    return count;
  }
}
