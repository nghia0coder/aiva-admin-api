namespace Aiva.Admin.Api.UseCases.Storages.Create;

using Core.StorageAggregate;

/// <summary>
/// Create a new Storage.
/// </summary>
/// <param name="StorageName"></param>
/// <param name="StorageDescription"></param>
public record CreateStorageCommand(StorageName StorageName, string? StorageDescription) : ICommand<Result<StorageId>>;
