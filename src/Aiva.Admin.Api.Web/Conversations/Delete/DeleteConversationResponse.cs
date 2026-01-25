namespace Aiva.Admin.Api.Web.Conversations.Delete;

/// <summary>
/// Response returned after successfully deleting a conversation
/// </summary>
public record DeleteConversationResponse(
  Guid ConversationId,
  string Title,
  DateTime DeletedAt,
  string Message);
