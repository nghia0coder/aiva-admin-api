namespace Aiva.Admin.Api.UseCases.Folders.Create;

using Core.FolderAggregate;
using Core.StorageAggregate;

public record CreateFolderCommand(
  StorageId StorageId,
  FolderName Name,
  FolderId? ParentFolderId = null,
  string? Description = null
) : ICommand<Result<FolderId>>;
