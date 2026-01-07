namespace Aiva.Admin.Api.Web.Conversations.Get;

public record GetConversationHistoryResponse(
    Guid Id,
    string Title,
    string? SystemPrompt,
    DateTime CreatedAt,
    IReadOnlyList<ChatMessageRecord> Messages);

public record ChatMessageRecord(
    Guid Id,
    string Role,
    string Content,
    DateTime CreatedAt);
