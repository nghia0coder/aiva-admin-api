using Aiva.Admin.Api.Core.SystemPromptAggregate;
using Aiva.Admin.Api.UseCases.SystemPrompts.Update;

namespace Aiva.Admin.Api.Web.SystemPrompts.Update;

public sealed class Update(IMediator mediator)
  : Endpoint<UpdateSystemPromptRequest, UpdateSystemPromptResponse>
{
  private readonly IMediator _mediator = mediator;

  public override void Configure()
  {
    Put(UpdateSystemPromptRequest.Route);

    Summary(s =>
    {
      s.Summary = "Update an existing system prompt";
      s.Description = "Updates the name, content, description, and active state of an existing system prompt.";
      s.Responses[200] = "System prompt updated successfully";
      s.Responses[404] = "System prompt not found";
    });

    Tags("SystemPrompts");
  }

  public override async Task HandleAsync(UpdateSystemPromptRequest request, CancellationToken cancellationToken)
  {
    var command = new UpdateSystemPromptCommand(
      SystemPromptId.From(request.Id),
      request.Name,
      request.Content,
      request.Description,
      request.IsActive);

    var result = await _mediator.Send(command, cancellationToken);

    if (!result.IsSuccess || result.Value is null)
    {
      await Send.NotFoundAsync(cancellationToken);
      return;
    }

    var dto = result.Value;
    var record = new SystemPromptRecord(
      dto.Id.Value,
      dto.Key.Value,
      dto.Name,
      dto.Content,
      dto.Description,
      dto.Version,
      dto.IsActive);

    await Send.OkAsync(new UpdateSystemPromptResponse(record), cancellationToken);
  }
}

