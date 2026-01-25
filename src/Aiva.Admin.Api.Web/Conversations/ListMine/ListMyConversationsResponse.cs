namespace Aiva.Admin.Api.Web.Conversations.ListMine;

public record ListMyConversationsResponse(
    IReadOnlyList<ConversationRecord> Items,
    int Page,
    int PerPage,
    int TotalCount,
    int TotalPages);

