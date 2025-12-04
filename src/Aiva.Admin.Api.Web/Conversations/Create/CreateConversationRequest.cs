namespace Aiva.Admin.Api.Web.Conversations.Create;

using System.ComponentModel.DataAnnotations;

public class CreateConversationRequest
{
  public const string Route = "/conversations";

  [Required]
  [StringLength(200, MinimumLength = 1)]
  public string Title { get; set; } = string.Empty;

  [StringLength(2000)]
  public string? SystemPrompt { get; set; }
}
