using Aiva.Admin.Api.UseCases.SystemPrompts.List;

namespace Aiva.Admin.Api.Web.SystemPrompts.List;

public sealed class List(IMediator mediator)
  : Endpoint<ListSystemPromptsRequest, ListSystemPromptsResponse, ListSystemPromptsMapper>
{
  private readonly IMediator _mediator = mediator;

  public override void Configure()
  {
    Get("/SystemPrompts");

    Summary(s =>
    {
      s.Summary = "List all system prompts";
      s.Description = "Retrieves all system prompts configured in the system.";
      s.ResponseExamples[200] = new ListSystemPromptsResponse(
        new List<SystemPromptRecord>
        {
          new(1, "default", "Default system prompt", "You are a helpful AI assistant.", "Default prompt", 1, true)
        });
      s.Responses[200] = "List of system prompts returned successfully";
    });

    Tags("SystemPrompts");

    Description(builder => builder
      .Accepts<ListSystemPromptsRequest>()
      .Produces<ListSystemPromptsResponse>(200, "application/json")
      .ProducesProblem(400));
  }

  public override async Task HandleAsync(ListSystemPromptsRequest request, CancellationToken cancellationToken)
  {
    var result = await _mediator.Send(new ListSystemPromptsQuery(), cancellationToken);
    if (!result.IsSuccess)
    {
      await Send.ErrorsAsync(statusCode: 400, cancellationToken);
      return;
    }

    var response = Map.FromEntity(result.Value);
    await Send.OkAsync(response, cancellationToken);
  }
}

