namespace Aiva.Admin.Api.Web.Conversations.Stream;

public class StreamChatRequest
{
  public const string Route = "/conversations/{ConversationId}/stream";
  public Guid ConversationId { get; set; }
  public string Message { get; set; } = string.Empty;
  public string AdditionalUserData { get; set; } = string.Empty;
}
