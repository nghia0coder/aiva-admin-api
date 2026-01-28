namespace Aiva.Admin.Api.UseCases.Folders.List;

public record ListFoldersResult(
  IReadOnlyList<FolderDto> Folders,
  int TotalCount,
  int Page,
  int PageSize,
  bool HasNextPage,
  bool HasPreviousPage
);
