using Microsoft.AspNetCore.Http.HttpResults;

namespace Aiva.Admin.Api.Web.Conversations.Create;

using Common;
using Core.Interfaces;
using Extensions;
using UseCases.Conversations.Create;

public class Create(IMediator mediator, ICurrentUserService currentUser)
    : AuthenticatedEndpoint<CreateConversationRequest,
        Results<Created<CreateConversationResponse>,
                ValidationProblem,
                ProblemHttpResult>>
{
  public override void Configure()
  {
    Post(CreateConversationRequest.Route);
    Summary(s =>
    {
      s.Summary = "Create a new AI conversation";
      s.Description = "Creates a new conversation with optional system prompt for context.";
      s.ExampleRequest = new CreateConversationRequest
      {
        Title = "Help with coding",
        SystemPrompt = "You are a helpful coding assistant."
      };
    });
    Tags("Conversations");
  }

  public override async Task<Results<Created<CreateConversationResponse>, ValidationProblem, ProblemHttpResult>>
      ExecuteAsync(CreateConversationRequest request, CancellationToken ct)
  {

    if (!IsAuthenticated)
    {
      return UnauthorizedResult();
    }

    var command = new CreateConversationCommand(currentUser.UserId, request.Title, request.SystemPrompt);
    var result = await mediator.Send(command, ct);

    return result.ToCreatedResult(
        id => $"/conversations/{id.Value}",
        id => new CreateConversationResponse(id.Value, request.Title));
  }
}
