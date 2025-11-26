namespace Aiva.Admin.Api.UseCases.Contributors.Create;

using Core.StorageAggregate;
using UseCases.Storages.Create;

public class CreateStorageHandler(IRepository<Storage> _repository)
  : ICommandHandler<CreateStorageCommand, Result<StorageId>>
{
  public async ValueTask<Result<StorageId>> Handle(CreateStorageCommand command,
    CancellationToken cancellationToken)
  {
    var newStorage = new Storage(command.StorageName, command.StorageDescription);
    var createdItem = await _repository.AddAsync(newStorage, cancellationToken);

    return createdItem.Id;
  }
}
