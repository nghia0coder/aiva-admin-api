namespace Aiva.Admin.Api.Web.Folders.List;

using Aiva.Admin.Api.Web.Folders;

public record ListFoldersResponse(
  IReadOnlyList<FolderRecord> Folders,
  int TotalCount,
  int Page,
  int PageSize,
  bool HasNextPage,
  bool HasPreviousPage
);
