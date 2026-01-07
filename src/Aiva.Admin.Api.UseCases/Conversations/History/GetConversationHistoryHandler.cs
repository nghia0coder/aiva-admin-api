namespace Aiva.Admin.Api.UseCases.Conversations.History;

using Core.ConversationAggregate;
using Core.ConversationAggregate.Specifications;

public class GetConversationHistoryHandler(IReadRepository<Conversation> repository)
    : IQueryHandler<GetConversationHistoryQuery, Result<ConversationDetailDTO>>
{
  public async ValueTask<Result<ConversationDetailDTO>> Handle(
      GetConversationHistoryQuery query,
      CancellationToken cancellationToken)
  {
    // Use specification that includes user authorization at the database level
    var spec = new ConversationByIdAndUserWithMessagesSpec(query.ConversationId, query.UserId);
    var conversation = await repository.FirstOrDefaultAsync(spec, cancellationToken);

    if (conversation is null)
    {
      return Result.NotFound("Conversation not found.");
    }

    // Process messages for optimal FE consumption
    var processedMessages = ProcessMessagesForDisplay(conversation.Messages);

    // Create rich metadata
    var metadata = new ConversationMetadataDTO(
        TotalMessages: conversation.Messages.Count,
        TotalTokens: conversation.Messages.Sum(m => EstimateTokenCount(m.Content)),
        LastActiveAt: conversation.LastMessageAt ?? conversation.CreatedAt,
        Status: "active",
        IsArchived: false);

    var dto = new ConversationDetailDTO(
        conversation.Id.Value,
        conversation.Title,
        conversation.SystemPrompt,
        conversation.CreatedAt,
        processedMessages,
        metadata);

    return Result.Success(dto);
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
            CreateMessageMetadata(m)))
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
}
