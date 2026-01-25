using Microsoft.AspNetCore.Http.HttpResults;

namespace Aiva.Admin.Api.Web.Conversations.UpdateTitle;

using Aiva.Admin.Api.UseCases.Conversations;
using Aiva.Admin.Api.UseCases.Conversations.UpdateTitle;
using Aiva.Admin.Api.Web.Common;
using Aiva.Admin.Api.Web.Extensions;
using Core.ConversationAggregate;
using static Aiva.Admin.Api.Web.Conversations.UpdateTitle.UpdateTitle;

public class UpdateTitle(IMediator mediator)
    : AuthenticatedEndpointWithMapper<UpdateTitleRequest,
        Results<Ok<UpdateTitleResponse>, NotFound, ProblemHttpResult>,
        UpdateTitleMapper>
{
  public override void Configure()
  {
    Put(UpdateTitleRequest.Route);
    Summary(s =>
    {
      s.Summary = "Update conversation title";
      s.Description = "Manually updates the title of an existing conversation.";
      s.ExampleRequest = new UpdateTitleRequest
      {
        Title = "Updated conversation title"
      };
    });
    Tags("Conversations");
  }

  public override async Task<Results<Ok<UpdateTitleResponse>, NotFound, ProblemHttpResult>> ExecuteAsync(
      UpdateTitleRequest req,
      CancellationToken ct)
  {
    var conversationId = ConversationId.From(Route<Guid>("conversationId"));

    var command = new UpdateTitleCommand(conversationId, req.Title);
    var result = await mediator.Send(command, ct);

    return result.ToUpdateResult(Map.FromEntity);
  }

  public sealed class UpdateTitleMapper
  : Mapper<UpdateTitleRequest, UpdateTitleResponse, ConversationDTO>
  {
    public override UpdateTitleResponse FromEntity(ConversationDTO e)
      => new(new ConversationRecord(e.Id, e.Title, e.CreatedAt, e.LastMessageAt, e.MessageCount));
  }
}
