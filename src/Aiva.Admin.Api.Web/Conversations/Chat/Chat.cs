using Microsoft.AspNetCore.Http.HttpResults;

namespace Aiva.Admin.Api.Web.Conversations.Chat;

using Core.ConversationAggregate;
using Extensions;
using UseCases.Conversations.Chat;

public class Chat(IMediator mediator)
    : Endpoint<ChatRequest,
        Results<Ok<ChatResponse>,
                NotFound,
                ValidationProblem,
                ProblemHttpResult>>
{
  public override void Configure()
  {
    Post(ChatRequest.Route);
    AllowAnonymous();
    Summary(s =>
    {
      s.Summary = "Send a message and get AI response";
      s.Description = "Sends a user message to the conversation and receives an AI assistant response.";
    });
    Tags("Conversations");
  }

  public override async Task<Results<Ok<ChatResponse>, NotFound, ValidationProblem, ProblemHttpResult>>
      ExecuteAsync(ChatRequest request, CancellationToken ct)
  {
    var command = new SendMessageCommand(
        ConversationId.From(request.ConversationId),
        request.Message);

    var result = await mediator.Send(command, ct);

    return result.ToOkResult(dto => new ChatResponse(
        dto.Id,
        dto.Role,
        dto.Content,
        dto.CreatedAt));
  }
}
