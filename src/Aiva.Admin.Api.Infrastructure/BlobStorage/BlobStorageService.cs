using Ardalis.Result;
using Azure;
using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace Aiva.Admin.Api.Infrastructure.BlobStorage;

using Core.Interfaces;


public sealed class BlobStorageService : IBlobStorageService
{
  private readonly BlobServiceClient _blobServiceClient;
  private readonly BlobStorageConfiguration _configuration;
  private readonly ILogger<BlobStorageService> _logger;

  public BlobStorageService(
      IOptions<BlobStorageConfiguration> options,
      ILogger<BlobStorageService> logger)
  {
    _configuration = options.Value;
    _logger = logger;
    _blobServiceClient = CreateBlobServiceClient();
  }

  private BlobServiceClient CreateBlobServiceClient()
  {
    if (_configuration.UseAzureIdentity && !string.IsNullOrEmpty(_configuration.ServiceUri))
    {
      return new BlobServiceClient(
          new Uri(_configuration.ServiceUri),
          new DefaultAzureCredential());
    }

    if (!string.IsNullOrEmpty(_configuration.ConnectionString))
    {
      return new BlobServiceClient(_configuration.ConnectionString);
    }

    if (!string.IsNullOrEmpty(_configuration.AccountName) &&
        !string.IsNullOrEmpty(_configuration.AccountKey))
    {
      var connectionString = $"DefaultEndpointsProtocol=https;" +
          $"AccountName={_configuration.AccountName};" +
          $"AccountKey={_configuration.AccountKey};" +
          $"EndpointSuffix=core.windows.net";
      return new BlobServiceClient(connectionString);
    }

    throw new InvalidOperationException(
        "Azure Blob Storage configuration is invalid. " +
        "Provide either ConnectionString, ServiceUri with UseAzureIdentity, or AccountName/AccountKey.");
  }

  public async Task<Result<string>> CreateContainerAsync(
      string containerName,
      CancellationToken cancellationToken = default)
  {
    try
    {
      var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);

      var accessTier = _configuration.DefaultAccessTier?.ToLower() switch
      {
        "cool" => PublicAccessType.None,
        "archive" => PublicAccessType.None,
        _ => PublicAccessType.None
      };

      var response = await containerClient.CreateIfNotExistsAsync(
          publicAccessType: accessTier,
          cancellationToken: cancellationToken);

      if (response?.Value != null)
      {
        _logger.LogInformation("Created new container: {ContainerName}", containerName);
      }
      else
      {
        _logger.LogInformation("Container already exists: {ContainerName}", containerName);
      }

      return Result.Success(containerClient.Uri.ToString());
    }
    catch (RequestFailedException ex)
    {
      _logger.LogError(ex, "Failed to create container {ContainerName}", containerName);
      return Result.Error($"Failed to create container: {ex.Message}");
    }
  }

  public async Task<Result> DeleteContainerAsync(
      string containerName,
      CancellationToken cancellationToken = default)
  {
    try
    {
      var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
      await containerClient.DeleteIfExistsAsync(cancellationToken: cancellationToken);

      _logger.LogInformation("Deleted container: {ContainerName}", containerName);
      return Result.Success();
    }
    catch (RequestFailedException ex)
    {
      _logger.LogError(ex, "Failed to delete container {ContainerName}", containerName);
      return Result.Error($"Failed to delete container: {ex.Message}");
    }
  }

  public async Task<bool> ContainerExistsAsync(
      string containerName,
      CancellationToken cancellationToken = default)
  {
    var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
    return await containerClient.ExistsAsync(cancellationToken);
  }
}
