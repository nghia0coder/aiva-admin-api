using Microsoft.AspNetCore.Http.HttpResults;
using Aiva.Admin.Api.UseCases.Conversations; // Correct full namespace

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
                          "including system prompts and chat history, ordered chronologically. " +
                          "Response includes rich metadata optimized for frontend consumption.";
            s.ExampleRequest = new GetConversationHistoryRequest
            {
                Id = Guid.Parse("123e4567-e89b-12d3-a456-426614174000")
            };
            s.Params["id"] = "The unique identifier of the conversation";
            s.Responses[200] = "Conversation details with rich metadata retrieved successfully";
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

        return result.ToOkResult(dto => MapToResponse(dto));
    }

    private static GetConversationHistoryResponse MapToResponse(ConversationDetailDTO dto)
    {
        // Map messages with rich metadata
        var messages = dto.Messages.Select(m => new ChatMessageRecord(
            m.Id,
            m.Role,
            m.Content,
            m.CreatedAt,
            m.Metadata != null ? new MessageMetadata(
                m.Metadata.TokenCount,
                m.Metadata.ResponseTime,
                m.Metadata.Model,
                m.Metadata.IsEdited,
                m.Metadata.EditedAt,
                MapMessageStatus(m.Metadata.Status)) : null
        )).ToList();

        // Map conversation metadata
        var metadata = dto.Metadata != null 
            ? new ConversationMetadata(
                dto.Metadata.TotalMessages,
                dto.Metadata.TotalTokens,
                dto.Metadata.LastActiveAt,
                MapConversationStatus(dto.Metadata.Status),
                dto.Metadata.IsArchived)
            : new ConversationMetadata(
                dto.Messages.Count,
                dto.Messages.Sum(m => m.Metadata?.TokenCount ?? 0),
                dto.Messages.LastOrDefault()?.CreatedAt ?? dto.CreatedAt);

        return new GetConversationHistoryResponse(
            dto.Id,
            dto.Title,
            dto.SystemPrompt,
            dto.CreatedAt,
            messages,
            metadata);
    }

    private static MessageStatus MapMessageStatus(string status) => status.ToLowerInvariant() switch
    {
        "pending" => MessageStatus.Pending,
        "streaming" => MessageStatus.Streaming,
        "completed" => MessageStatus.Completed,
        "failed" => MessageStatus.Failed,
        "filtered" => MessageStatus.Filtered,
        _ => MessageStatus.Completed
    };

    private static ConversationStatus MapConversationStatus(string status) => status.ToLowerInvariant() switch
    {
        "active" => ConversationStatus.Active,
        "archived" => ConversationStatus.Archived,
        "deleted" => ConversationStatus.Deleted,
        _ => ConversationStatus.Active
    };
}
