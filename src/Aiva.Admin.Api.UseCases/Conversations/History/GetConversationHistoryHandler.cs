namespace Aiva.Admin.Api.UseCases.Conversations.History;

using Core.Commons.Models;
using Core.ConversationAggregate;
using Core.ConversationAggregate.Specifications;
using Core.Interfaces;

public class GetConversationHistoryHandler(
    IReadRepository<Conversation> repository,
    IConversationMessageQueryService messageQueryService)
    : IQueryHandler<GetConversationHistoryQuery, Result<ConversationDetailDTO>>
{
  public async ValueTask<Result<ConversationDetailDTO>> Handle(
      GetConversationHistoryQuery query,
      CancellationToken cancellationToken)
  {
    // First, verify conversation exists and user has access (without loading all messages)
    var spec = new ConversationByIdAndUserSpec(query.ConversationId, query.UserId);
    var conversation = await repository.FirstOrDefaultAsync(spec, cancellationToken);

    if (conversation is null)
    {
      return Result.NotFound("Conversation not found.");
    }

    // Get paginated messages using cursor-based approach
    var paginationParams = query.Pagination ?? new PaginationParams();
    var messageResult = await GetPaginatedMessages(
        query.ConversationId, 
        paginationParams, 
        cancellationToken);

    // Process messages for optimal FE consumption
    var processedMessages = ProcessMessagesForDisplay(messageResult.Messages.ToList());

    // Get total message count for metadata
    var totalMessages = await messageQueryService.CountMessagesAsync(query.ConversationId, cancellationToken);

    // Create rich metadata
    var metadata = new ConversationMetadataDTO(
        TotalMessages: totalMessages,
        TotalTokens: messageResult.Messages.Sum(m => EstimateTokenCount(m.Content)),
        LastActiveAt: conversation.LastMessageAt ?? conversation.CreatedAt,
        Status: "active",
        IsArchived: false);

    // Create pagination info
    var paginationInfo = new PaginationInfoDTO(
        HasMore: messageResult.HasMore,
        HasNewer: messageResult.HasNewer,
        OldestMessageId: messageResult.Messages.FirstOrDefault()?.Id.Value,
        NewestMessageId: messageResult.Messages.LastOrDefault()?.Id.Value,
        OldestTimestamp: messageResult.Messages.FirstOrDefault()?.CreatedAt,
        NewestTimestamp: messageResult.Messages.LastOrDefault()?.CreatedAt,
        TotalMessages: totalMessages,
        ReturnedCount: messageResult.Messages.Count);

    var dto = new ConversationDetailDTO(
        conversation.Id.Value,
        conversation.Title,
        conversation.SystemPrompt,
        conversation.CreatedAt,
        processedMessages,
        metadata,
        paginationInfo);

    return Result.Success(dto);
  }

  private async Task<PaginatedMessages> GetPaginatedMessages(
      ConversationId conversationId,
      PaginationParams pagination,
      CancellationToken cancellationToken)
  {
    var loadingMode = pagination.GetLoadingMode();
    var limit = Math.Min(pagination.Limit, 200); // Cap at 200

    return loadingMode switch
    {
      LoadingMode.Latest => await messageQueryService.GetLatestMessagesAsync(conversationId, limit, cancellationToken),
      LoadingMode.Before => await messageQueryService.GetMessagesBeforeAsync(conversationId, pagination.BeforeMessageId!.Value, limit, cancellationToken),
      LoadingMode.After => await messageQueryService.GetMessagesAfterAsync(conversationId, pagination.AfterMessageId!.Value, limit, cancellationToken),
      LoadingMode.Around => await messageQueryService.GetMessagesAroundAsync(conversationId, pagination.AroundMessageId!.Value, limit, cancellationToken),
      _ => await messageQueryService.GetLatestMessagesAsync(conversationId, limit, cancellationToken)
    };
  }

  private static IReadOnlyList<ChatMessageDTO> ProcessMessagesForDisplay(
      IReadOnlyList<ChatMessage> messages)
  {
    return messages
        .OrderBy(m => m.CreatedAt)
        .Select(m => new ChatMessageDTO(
            m.Id.Value,
            NormalizeRole(m.Role.Name),
            ProcessContentForDisplay(m.Content, m.Role.Name),
            m.CreatedAt,
            CreateMessageMetadata(m),
            NormalizeResponseType(m.ResponseType.Name),
            MapStructuredData(m.GetStructuredData())))
        .ToList()
        .AsReadOnly();
  }

  private static string NormalizeRole(string role) => role.ToLowerInvariant() switch
  {
    "system" => "system",
    "user" => "user",
    "assistant" => "assistant", 
    "ai" => "assistant",        // Legacy mapping
    "bot" => "assistant",       // Legacy mapping
    _ => "assistant"            // Safe default
  };

  private static string NormalizeResponseType(string responseType) => responseType switch
  {
    "Text" => "text",
    "StructuredTable" => "structured",
    "Mixed" => "mixed",
    _ => "text"                // Safe default
  };

  private static string ProcessContentForDisplay(string content, string role)
  {
    if (string.IsNullOrWhiteSpace(content))
      return string.Empty;

    return role.ToLowerInvariant() switch
    {
      "system" => content.Trim(),
      "user" => content.Trim(),
      "assistant" => ProcessAssistantContent(content),
      _ => content.Trim()
    };
  }

  private static string ProcessAssistantContent(string content)
  {
    // Format assistant content similar to ChatGPT
    return content.Trim()
        .Replace("```\n", "```\n")           // Ensure code blocks are properly formatted
        .Replace("\n```", "\n```")
        .Replace("\n\n\n", "\n\n")          // Remove excessive line breaks
        .Replace("**", "**");               // Preserve markdown formatting
  }

  private static MessageMetadataDTO CreateMessageMetadata(ChatMessage message)
  {
    return new MessageMetadataDTO(
        TokenCount: EstimateTokenCount(message.Content),
        ResponseTime: null, // Can be populated if available
        Model: null,        // Can be populated if available
        IsEdited: false,    // Can be populated if editing feature exists
        EditedAt: null,
        Status: "completed");
  }

  private static int EstimateTokenCount(string content)
  {
    if (string.IsNullOrWhiteSpace(content))
      return 0;
      
    // Rough estimation: ~4 characters per token for English
    // More sophisticated tokenization can be added later
    return (int)Math.Ceiling(content.Length / 4.0);
  }

  private static TableDataDTO? MapStructuredData(TableData? structuredData)
  {
    if (structuredData == null)
      return null;

    return new TableDataDTO(
        new TableMetadataDTO(
            structuredData.Metadata.Title,
            structuredData.Metadata.Description,
            structuredData.Metadata.TotalCount,
            structuredData.Metadata.DisplayedCount),
        structuredData.Columns.Select(c => new TableColumnDTO(
            c.Key,
            c.Label,
            c.Type.ToString(),
            c.Sortable,
            c.Filterable)).ToList(),
        structuredData.Rows.Select(r => new TableRowDTO(
            r.Id,
            r.Cells,
            r.Actions.Select(a => new ActionMetadataDTO(
                a.Type.ToString(),
                a.Label,
                a.Icon,
                a.Endpoint,
                a.Method,
                a.Params,
                a.IsDisabled,
                a.DisabledReason)).ToList())).ToList(),
        structuredData.GlobalActions.Select(a => new ActionMetadataDTO(
            a.Type.ToString(),
            a.Label,
            a.Icon,
            a.Endpoint,
            a.Method,
            a.Params,
            a.IsDisabled,
            a.DisabledReason)).ToList());
  }
}
