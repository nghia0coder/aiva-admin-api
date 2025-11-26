namespace Aiva.Admin.Api.UseCases.Storages;

using Core.StorageAggregate;

public record StorageDto(StorageId Id, StorageName StorageName, string? StorageDescription);
