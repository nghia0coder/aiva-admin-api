using Microsoft.AspNetCore.Http.HttpResults;

namespace Aiva.Admin.Api.Web.Conversations.Get;

using Common;
using Core.ConversationAggregate;
using Extensions;
using UseCases.Conversations.History;

public class GetConversationHistory(IMediator mediator)
    : AuthenticatedEndpoint<GetConversationHistoryRequest,
        Results<Ok<GetConversationHistoryResponse>,
                NotFound,
                ValidationProblem,
                ProblemHttpResult>>
{
    public override void Configure()
    {
        Get(GetConversationHistoryRequest.Route);
        Summary(s =>
        {
            s.Summary = "Get conversation details with full message history";
            s.Description = "Retrieves a specific conversation with all its messages, " +
                          "including system prompts and chat history, ordered chronologically.";
            s.ExampleRequest = new GetConversationHistoryRequest
            {
                Id = Guid.Parse("123e4567-e89b-12d3-a456-426614174000")
            };
            s.Params["id"] = "The unique identifier of the conversation";
            s.Responses[200] = "Conversation details retrieved successfully";
            s.Responses[401] = "User not authenticated";
            s.Responses[404] = "Conversation not found or access denied";
        });
        Tags("Conversations");
    }

    public override async Task<Results<Ok<GetConversationHistoryResponse>, NotFound, ValidationProblem, ProblemHttpResult>>
        ExecuteAsync(GetConversationHistoryRequest request, CancellationToken ct)
    {
        if (!IsAuthenticated)
        {
            return UnauthorizedResult();
        }

        var conversationId = ConversationId.From(request.Id);
        var query = new GetConversationHistoryQuery(conversationId, RequiredUserId);
        
        var result = await mediator.Send(query, ct);

        return result.ToOkResult(dto => new GetConversationHistoryResponse(
            dto.Id,
            dto.Title,
            dto.SystemPrompt,
            dto.CreatedAt,
            dto.Messages.Select(m => new ChatMessageRecord(
                m.Id,
                m.Role,
                m.Content,
                m.CreatedAt)).ToList()));
    }
}
