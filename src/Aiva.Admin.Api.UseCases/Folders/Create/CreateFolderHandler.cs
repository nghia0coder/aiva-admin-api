namespace Aiva.Admin.Api.UseCases.Folders.Create;

using Core.FolderAggregate;
using Core.StorageAggregate;

public class CreateFolderHandler(
  IRepository<Folder> folderRepository,
  IReadRepository<Storage> storageRepository
) : ICommandHandler<CreateFolderCommand, Result<FolderId>>
{
  public async ValueTask<Result<FolderId>> Handle(
    CreateFolderCommand command,
    CancellationToken cancellationToken)
  {
    // Validate storage exists
    var storage = await storageRepository.GetByIdAsync(command.StorageId, cancellationToken);
    if (storage is null)
      return Result.NotFound("Storage not found");

    // Get parent folder prefix if exists
    string? parentPrefix = null;
    if (command.ParentFolderId.HasValue)
    {
      var parentFolder = await folderRepository.GetByIdAsync(command.ParentFolderId.Value, cancellationToken);
      if (parentFolder is null)
        return Result.NotFound("Parent folder not found");

      if (parentFolder.StorageId != command.StorageId)
        return Result.Invalid(new ValidationError("Parent folder must belong to the same storage"));

      parentPrefix = parentFolder.BlobPrefix;
    }

    var newFolder = Folder.Create(
      command.Name,
      command.StorageId,
      command.ParentFolderId,
      command.Description
    );

    var createdFolder = await folderRepository.AddAsync(newFolder, cancellationToken);

    createdFolder.SetBlobPrefix(parentPrefix);
    await folderRepository.UpdateAsync(createdFolder, cancellationToken);

    return createdFolder.Id;
  }
}
