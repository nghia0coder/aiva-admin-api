namespace Aiva.Admin.Api.Web.Folders.List;

public record ListFoldersRequest(
  int? StorageId = null,
  int? ParentFolderId = null,
  int Page = 1,
  int PageSize = 50,
  string? SearchTerm = null,
  bool IncludeChildren = false
);
