using System.ComponentModel.DataAnnotations;

namespace Aiva.Admin.Api.Web.Conversations.Chat;

public class ChatRequest
{
  public const string Route = "/conversations/{ConversationId}/chat";

  [Required]
  public Guid ConversationId { get; set; }

  [Required]
  [StringLength(10000, MinimumLength = 1)]
  public string Message { get; set; } = string.Empty;
}
