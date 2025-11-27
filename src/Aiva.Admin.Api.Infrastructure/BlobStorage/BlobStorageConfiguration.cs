namespace Aiva.Admin.Api.Infrastructure.BlobStorage;

public sealed class BlobStorageConfiguration
{
  public const string SectionName = "AzureBlobStorage";
  public string? ConnectionString { get; set; }
  public string? ServiceUri { get; set; }
  public bool UseAzureIdentity { get; set; } = false;
  public string? AccountName { get; set; }
  public string? AccountKey { get; set; }
  public string DefaultAccessTier { get; set; } = "Hot";
}
