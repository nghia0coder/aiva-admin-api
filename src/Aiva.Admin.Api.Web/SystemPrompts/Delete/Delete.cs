using Aiva.Admin.Api.Core.SystemPromptAggregate;
using Aiva.Admin.Api.UseCases.SystemPrompts.Delete;

namespace Aiva.Admin.Api.Web.SystemPrompts.Delete;

public sealed class Delete(IMediator mediator)
  : Endpoint<DeleteSystemPromptRequest>
{
  private readonly IMediator _mediator = mediator;

  public override void Configure()
  {
    Delete(DeleteSystemPromptRequest.Route);

    Summary(s =>
    {
      s.Summary = "Delete a system prompt";
      s.Description = "Deletes an existing system prompt by ID.";
      s.Responses[204] = "System prompt deleted successfully";
      s.Responses[404] = "System prompt not found";
    });

    Tags("SystemPrompts");
  }

  public override async Task HandleAsync(DeleteSystemPromptRequest request, CancellationToken cancellationToken)
  {
    var command = new DeleteSystemPromptCommand(SystemPromptId.From(request.Id));
    var result = await _mediator.Send(command, cancellationToken);

    if (!result.IsSuccess)
    {
      await Send.NotFoundAsync(cancellationToken);
      return;
    }

    await Send.NoContentAsync(cancellationToken);
  }
}

