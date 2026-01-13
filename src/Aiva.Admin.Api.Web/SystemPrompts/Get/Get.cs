using Aiva.Admin.Api.Core.SystemPromptAggregate;
using Aiva.Admin.Api.UseCases.SystemPrompts.Get;

namespace Aiva.Admin.Api.Web.SystemPrompts.Get;

public sealed class Get(IMediator mediator)
  : Endpoint<GetSystemPromptRequest, GetSystemPromptResponse>
{
  private readonly IMediator _mediator = mediator;

  public override void Configure()
  {
    Get(GetSystemPromptRequest.Route);

    Summary(s =>
    {
      s.Summary = "Get a single system prompt by ID";
      s.Description = "Retrieves a specific system prompt by its numeric ID.";
      s.Responses[200] = "System prompt returned successfully";
      s.Responses[404] = "System prompt not found";
    });

    Tags("SystemPrompts");
  }

  public override async Task HandleAsync(GetSystemPromptRequest request, CancellationToken cancellationToken)
  {
    var query = new GetSystemPromptQuery(SystemPromptId.From(request.Id));
    var result = await _mediator.Send(query, cancellationToken);

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

    await Send.OkAsync(new GetSystemPromptResponse(record), cancellationToken);
  }
}

