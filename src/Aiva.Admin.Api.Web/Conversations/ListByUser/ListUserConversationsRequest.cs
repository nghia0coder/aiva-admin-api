namespace Aiva.Admin.Api.Web.Conversations.ListByUser;

public class ListUserConversationsRequest
{
  public const string Route = "/users/{UserId:int}/conversations";

  /// <summary>User ID to get conversations for</summary>
  public int UserId { get; set; }

  /// <summary>Page number (1-based)</summary>
  [QueryParam]
  public int Page { get; set; } = 1;

  /// <summary>Number of items per page</summary>
  [QueryParam]
  public int PerPage { get; set; } = UseCases.Constants.DEFAULT_PAGE_SIZE;
}
