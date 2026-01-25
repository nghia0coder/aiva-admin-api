using Microsoft.AspNetCore.Http.HttpResults;

namespace Aiva.Admin.Api.Web.Conversations.Delete;

using Aiva.Admin.Api.UseCases.Conversations.Delete;
using Aiva.Admin.Api.Web.Common;
using Aiva.Admin.Api.Web.Extensions;
using Core.ConversationAggregate;

public class Delete(IMediator mediator)
    : AuthenticatedEndpoint<DeleteConversationRequest,
        Results<NoContent, NotFound, ProblemHttpResult>>
{
  public override void Configure()
  {
    Delete(DeleteConversationRequest.Route);
    Summary(s =>
    {
      s.Summary = "Delete conversation (soft delete)";
      s.Description = "Soft deletes an existing conversation. Returns deletion metadata for UI feedback.";
      s.ExampleRequest = new DeleteConversationRequest();
      s.Response<DeleteConversationResponse>(200, "Conversation successfully deleted");
    });
    Tags("Conversations");
  }

  public override async Task<Results<NoContent, NotFound, ProblemHttpResult>> ExecuteAsync(
      DeleteConversationRequest req,
      CancellationToken ct)
  {
    var conversationId = ConversationId.From(Route<Guid>("conversationId"));

    var command = new DeleteConversationCommand(conversationId);
    var result = await mediator.Send(command, ct);

    return result.ToDeleteResult();
  }
}
