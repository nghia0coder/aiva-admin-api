namespace Aiva.Admin.Api.Web.Conversations.ListMine;

public class ListMyConversationsRequest
{
  public const string Route = "/conversations"; // Clean route without userId

  /// <summary>Page number (1-based)</summary>
  [QueryParam]
  public int Page { get; set; } = 1;

  /// <summary>Number of items per page</summary>
  [QueryParam]  
  public int PerPage { get; set; } = UseCases.Constants.DEFAULT_PAGE_SIZE;
}
