using Microsoft.AspNetCore.Http.HttpResults;
using Aiva.Admin.Api.UseCases.Conversations; // Correct full namespace
using Aiva.Admin.Api.UseCases.Conversations.History;

namespace Aiva.Admin.Api.Web.Conversations.Get;

using Common;
using Core.ConversationAggregate;
using Extensions;

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
            s.Summary = "Get conversation details with paginated message history";
            s.Description = "Retrieves a specific conversation with paginated messages using cursor-based pagination. " +
                          "Supports loading latest messages (default), older messages (scroll up), " +
                          "newer messages (scroll down), and context around a specific message (deep linking). " +
                          "Response includes rich metadata and pagination info optimized for frontend consumption.";
            s.ExampleRequest = new GetConversationHistoryRequest
            {
                Id = Guid.Parse("123e4567-e89b-12d3-a456-426614174000"),
                Limit = 50
            };
            s.Params["id"] = "The unique identifier of the conversation";
            s.Params["beforeMessageId"] = "(Optional) Load messages before this message ID (for scrolling up)";
            s.Params["afterMessageId"] = "(Optional) Load messages after this message ID (for scrolling down)";
            s.Params["aroundMessageId"] = "(Optional) Load messages around this message ID (for deep linking)";
            s.Params["limit"] = "(Optional) Number of messages to return (default: 50, max: 200)";
            s.Responses[200] = "Conversation details with paginated messages retrieved successfully";
            s.Responses[400] = "Invalid pagination parameters";
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
        
        // Create pagination params if any cursor is provided
        PaginationParams? paginationParams = null;
        if (request.BeforeMessageId.HasValue || request.AfterMessageId.HasValue || 
            request.AroundMessageId.HasValue || request.Limit.HasValue)
        {
            paginationParams = new PaginationParams(
                BeforeMessageId: request.BeforeMessageId,
                AfterMessageId: request.AfterMessageId,
                AroundMessageId: request.AroundMessageId,
                Limit: request.Limit ?? 50);
        }
        
        var query = new GetConversationHistoryQuery(conversationId, RequiredUserId, paginationParams);
        
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

        // Map pagination info if available
        PaginationInfo? paginationInfo = null;
        if (dto.Pagination != null)
        {
            paginationInfo = new PaginationInfo(
                dto.Pagination.HasMore,
                dto.Pagination.HasNewer,
                dto.Pagination.OldestMessageId,
                dto.Pagination.NewestMessageId,
                dto.Pagination.OldestTimestamp,
                dto.Pagination.NewestTimestamp,
                dto.Pagination.TotalMessages,
                dto.Pagination.ReturnedCount);
        }

        return new GetConversationHistoryResponse(
            dto.Id,
            dto.Title,
            dto.SystemPrompt,
            dto.CreatedAt,
            messages,
            metadata,
            paginationInfo);
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
