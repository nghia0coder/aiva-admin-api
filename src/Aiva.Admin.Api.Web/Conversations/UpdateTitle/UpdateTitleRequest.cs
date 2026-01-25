using System.ComponentModel.DataAnnotations;

namespace Aiva.Admin.Api.Web.Conversations.UpdateTitle;

public record UpdateTitleRequest
{
  public const string Route = "/conversations/{conversationId:guid}/title";

  [Required]
  [StringLength(200, MinimumLength = 1)]
  public required string Title { get; init; }
}
