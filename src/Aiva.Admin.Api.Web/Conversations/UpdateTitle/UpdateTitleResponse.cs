namespace Aiva.Admin.Api.Web.Conversations.UpdateTitle;

public record UpdateTitleResponse(ConversationRecord Conversation)
{
  public ConversationRecord Conversation { get; set; } = Conversation;
}
