namespace Aiva.Admin.Api.Web.Folders.GetContents;

public record GetFolderContentsRequest
{
  /// <summary>
  /// The folder ID to get contents for. If null, returns root level contents for the storage.
  /// </summary>
  public int? FolderId { get; set; }

  /// <summary>
  /// Storage ID (required if FolderId is null to get root contents)
  /// </summary>
  public int? StorageId { get; set; }

  /// <summary>
  /// Optional search query to filter folders and files
  /// </summary>
  public string? SearchQuery { get; set; }

  /// <summary>
  /// Sort by: "name", "date", "size"
  /// </summary>
  public string? SortBy { get; set; } = "name";

  /// <summary>
  /// Sort order: "asc", "desc"
  /// </summary>
  public string? SortOrder { get; set; } = "asc";

  /// <summary>
  /// Page number for pagination (default: 1)
  /// </summary>
  public int Page { get; set; } = 1;

  /// <summary>
  /// Page size for pagination (default: 50, max: 100)
  /// </summary>
  public int PageSize { get; set; } = 50;
}
