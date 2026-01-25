namespace Aiva.Admin.Api.Web.Conversations.Delete;

public record DeleteConversationRequest
{
  public const string Route = "/conversations/{conversationId:guid}";

  public Guid ConversationId { get; set; }
}
