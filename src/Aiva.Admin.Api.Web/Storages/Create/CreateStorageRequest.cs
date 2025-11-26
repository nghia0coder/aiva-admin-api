using System.ComponentModel.DataAnnotations;

namespace Aiva.Admin.Api.Web.Storages.Create;

public class CreateStorageRequest
{
  public const string Route = "/Storages";

  [Required]
  public string StorageName { get; set; } = string.Empty;

  public string? StorageDescription { get; set; }
}


