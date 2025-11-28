namespace Aiva.Admin.Api.UseCases.Folders;

using Core.FolderAggregate;
using Core.StorageAggregate;

public record FolderDto(
  FolderId Id,
  FolderName Name,
  string? Description,
  string BlobPrefix,
  StorageId StorageId,
  FolderId? ParentFolderId,
  DateTime CreatedOnUtc
);

public record FolderTreeNodeDto(
  FolderId Id,
  FolderName Name,
  string? Description,
  string BlobPrefix,
  List<FolderTreeNodeDto> Children
);
