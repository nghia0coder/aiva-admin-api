namespace Aiva.Admin.Api.Web.Conversations.ListMine;

public record ListMyConversationsResponse(
    IReadOnlyList<ConversationRecord> Items,
    int Page,
    int PerPage,
    int TotalCount,
    int TotalPages);

public record ConversationRecord(
    Guid Id,
    string Title,
    DateTime CreatedAt,
    DateTime? LastMessageAt,
    int MessageCount);
