namespace Aiva.Admin.Api.UseCases.Folders.List;

using Core.StorageAggregate;

public record ListFoldersQuery(
  StorageId? StorageId = null,
  int? ParentFolderId = null,
  int Page = 1,
  int PageSize = 50,
  string? SearchTerm = null,
  bool IncludeChildren = false
) : IQuery<Result<ListFoldersResult>>;
