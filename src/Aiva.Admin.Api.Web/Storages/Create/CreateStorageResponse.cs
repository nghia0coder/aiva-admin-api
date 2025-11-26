namespace Aiva.Admin.Api.Web.Storages.Create;

public class CreateStorageResponse(int id, string StorageName, string? StorageDescription)
{
  public int Id { get; set; } = id;
  public string StorageName { get; set; } = StorageName;
  public string? StorageDescription { get; set; } = StorageDescription;
}
