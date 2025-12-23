using Aiva.Admin.Api.Core.Interfaces;
using Aiva.Admin.Api.UseCases.Conversations.Create;
using Aiva.Admin.Api.Web.Extensions;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Aiva.Admin.Api.Web.Conversations.Create;

public class Create(IMediator mediator, ICurrentUserService currentUser)
    : Endpoint<CreateConversationRequest,
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

    if (!currentUser.IsAuthenticated || currentUser.UserId is null)
    {
      return TypedResults.Problem(
          detail: "User not authenticated",
          statusCode: StatusCodes.Status401Unauthorized);
    }

    var command = new CreateConversationCommand(currentUser.UserId, request.Title, request.SystemPrompt);
    var result = await mediator.Send(command, ct);

    return result.ToCreatedResult(
        id => $"/conversations/{id.Value}",
        id => new CreateConversationResponse(id.Value, request.Title));
  }
}
