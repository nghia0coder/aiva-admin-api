using Microsoft.AspNetCore.Http.HttpResults;

namespace Aiva.Admin.Api.Web.SystemPrompts.Create;

using Common;
using Extensions;
using UseCases.SystemPrompts.Create;

public sealed class Create(IMediator mediator)
  : AuthenticatedEndpoint<CreateSystemPromptRequest,
      Results<Created<CreateSystemPromptResponse>, ValidationProblem, ProblemHttpResult>>
{
  private readonly IMediator _mediator = mediator;

  public override void Configure()
  {
    Post(CreateSystemPromptRequest.Route);

    Summary(s =>
    {
      s.Summary = "Create a new system prompt";
      s.Description = "Creates a new system prompt that can be used as a system message for conversations.";
      s.ExampleRequest = new CreateSystemPromptRequest
      {
        Key = "default",
        Name = "Default system prompt",
        Content = "You are a helpful AI assistant.",
        Description = "Default prompt for general conversations"
      };

      s.Responses[201] = "System prompt created successfully";
      s.Responses[400] = "Invalid input data - validation errors";
      s.Responses[500] = "Internal server error";
    });

    Tags("SystemPrompts");

    Description(builder => builder
      .Accepts<CreateSystemPromptRequest>("application/json")
      .Produces<CreateSystemPromptResponse>(201, "application/json")
      .ProducesProblem(400)
      .ProducesProblem(500));
  }

  public override async Task<Results<Created<CreateSystemPromptResponse>, ValidationProblem, ProblemHttpResult>>
    ExecuteAsync(CreateSystemPromptRequest request, CancellationToken cancellationToken)
  {
    var command = new CreateSystemPromptCommand(
      request.Key,
      request.Name,
      request.Content,
      CurrentUserId,
      request.Category ?? "Default",
      request.Description);

    var result = await _mediator.Send(command, cancellationToken);

    return result.ToCreatedResult(
      id => $"/SystemPrompts/{id}",
      id => new CreateSystemPromptResponse(id.Value));
  }
}

