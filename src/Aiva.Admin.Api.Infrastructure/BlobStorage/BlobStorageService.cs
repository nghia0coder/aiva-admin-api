using Ardalis.Result;
using Azure;
using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace Aiva.Admin.Api.Infrastructure.BlobStorage;

using Aiva.Admin.Api.Infrastructure.Configuration;
using Core.Interfaces;


public sealed class BlobStorageService : IBlobStorageService
{
  private readonly BlobServiceClient _blobServiceClient;
  private readonly BlobStorageConfiguration _configuration;
  private readonly ILogger<BlobStorageService> _logger;
  private const string FolderMarkerFileName = ".folder";
  private readonly AppSettings _appSettings;

  public BlobStorageService(
      IOptions<BlobStorageConfiguration> options,
      ILogger<BlobStorageService> logger,
      AppSettings appSettings)
  {
    _configuration = options.Value;
    _logger = logger;
    _appSettings = appSettings;
    _blobServiceClient = CreateBlobServiceClient();
  }

  private BlobServiceClient CreateBlobServiceClient()
  {
    if (_configuration.UseAzureIdentity && !string.IsNullOrEmpty(_configuration.ServiceUri))
    {
      return new BlobServiceClient(
          new Uri(_appSettings.AzureBlobStorage.ServiceUri),
          new DefaultAzureCredential());
    }

    if (!string.IsNullOrEmpty(_appSettings.AzureBlobStorage.AccountName) &&
        !string.IsNullOrEmpty(_appSettings.AzureBlobStorage.AccountKey))
    {
      var connectionString = $"DefaultEndpointsProtocol=https;" +
          $"AccountName={_appSettings.AzureBlobStorage.AccountName};" +
          $"AccountKey={_appSettings.AzureBlobStorage.AccountKey};" +
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

  public async Task<Result<string>> CreateVirtualFolderAsync(
    string containerName,
    string folderPath,
    CancellationToken cancellationToken = default)
  {
    try
    {
      var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);

      // Ensure container exists
      if (!await containerClient.ExistsAsync(cancellationToken))
      {
        return Result.Error($"Container '{containerName}' does not exist");
      }

      // Normalize folder path (ensure trailing slash for prefix)
      var normalizedPath = folderPath.TrimEnd('/');
      var markerBlobName = $"{normalizedPath}/{FolderMarkerFileName}";

      var blobClient = containerClient.GetBlobClient(markerBlobName);

      // Upload an empty placeholder blob to create the "folder"
      using var emptyStream = new MemoryStream(Array.Empty<byte>());
      await blobClient.UploadAsync(
        emptyStream,
        overwrite: true,
        cancellationToken: cancellationToken);

      _logger.LogInformation(
        "Created virtual folder: {FolderPath} in container: {ContainerName}",
        folderPath,
        containerName);

      return Result.Success($"{containerClient.Uri}/{normalizedPath}");
    }
    catch (RequestFailedException ex)
    {
      _logger.LogError(ex,
        "Failed to create virtual folder {FolderPath} in container {ContainerName}",
        folderPath,
        containerName);
      return Result.Error($"Failed to create virtual folder: {ex.Message}");
    }
  }

  public async Task<Result> DeleteVirtualFolderAsync(
      string containerName,
      string folderPath,
      bool deleteContents = false,
      CancellationToken cancellationToken = default)
  {
    try
    {
      var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
      var normalizedPath = folderPath.TrimEnd('/');

      if (deleteContents)
      {
        // Delete all blobs with this prefix
        await foreach (var blob in containerClient.GetBlobsAsync(
          prefix: normalizedPath + "/",
          cancellationToken: cancellationToken))
        {
          await containerClient.DeleteBlobIfExistsAsync(blob.Name, cancellationToken: cancellationToken);
        }
      }
      else
      {
        // Only delete the folder marker
        var markerBlobName = $"{normalizedPath}/{FolderMarkerFileName}";
        await containerClient.DeleteBlobIfExistsAsync(markerBlobName, cancellationToken: cancellationToken);
      }

      _logger.LogInformation(
        "Deleted virtual folder: {FolderPath} from container: {ContainerName}",
        folderPath,
        containerName);

      return Result.Success();
    }
    catch (RequestFailedException ex)
    {
      _logger.LogError(ex,
        "Failed to delete virtual folder {FolderPath} in container {ContainerName}",
        folderPath,
        containerName);
      return Result.Error($"Failed to delete virtual folder: {ex.Message}");
    }
  }

  public async Task<bool> VirtualFolderExistsAsync(
      string containerName,
      string folderPath,
      CancellationToken cancellationToken = default)
  {
    var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
    var normalizedPath = folderPath.TrimEnd('/') + "/";

    // Check if any blob exists with this prefix
    await foreach (var _ in containerClient.GetBlobsAsync(
      prefix: normalizedPath,
      cancellationToken: cancellationToken))
    {
      return true;
    }

    return false;
  }


  public async Task<Result<string>> UploadFileAsync(
    string containerName,
    string blobPath,
    Stream content,
    string contentType,
    CancellationToken cancellationToken = default)
  {
    try
    {
      var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);

      if (!await containerClient.ExistsAsync(cancellationToken))
      {
        return Result.Error($"Container '{containerName}' does not exist");
      }

      var blobClient = containerClient.GetBlobClient(blobPath);

      var blobHttpHeaders = new BlobHttpHeaders
      {
        ContentType = contentType
      };

      await blobClient.UploadAsync(
          content,
          new BlobUploadOptions { HttpHeaders = blobHttpHeaders },
          cancellationToken);

      _logger.LogInformation(
          "Uploaded file to blob: {BlobPath} in container: {ContainerName}",
          blobPath,
          containerName);

      return Result.Success(blobClient.Uri.ToString());
    }
    catch (RequestFailedException ex)
    {
      _logger.LogError(ex,
          "Failed to upload file {BlobPath} to container {ContainerName}",
          blobPath,
          containerName);
      return Result.Error($"Failed to upload file: {ex.Message}");
    }
  }

  public async Task<Result> DeleteFileAsync(
      string containerName,
      string blobPath,
      CancellationToken cancellationToken = default)
  {
    try
    {
      var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
      var blobClient = containerClient.GetBlobClient(blobPath);

      await blobClient.DeleteIfExistsAsync(cancellationToken: cancellationToken);

      _logger.LogInformation(
          "Deleted file: {BlobPath} from container: {ContainerName}",
          blobPath,
          containerName);

      return Result.Success();
    }
    catch (RequestFailedException ex)
    {
      _logger.LogError(ex,
          "Failed to delete file {BlobPath} from container {ContainerName}",
          blobPath,
          containerName);
      return Result.Error($"Failed to delete file: {ex.Message}");
    }
  }

  public async Task<Result<Stream>> DownloadFileAsync(
    string containerName,
    string blobPath,
    CancellationToken cancellationToken = default)
  {
    try
    {
      var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
      var blobClient = containerClient.GetBlobClient(blobPath);

      if (!await blobClient.ExistsAsync(cancellationToken))
      {
        return Result.NotFound($"Blob '{blobPath}' not found in container '{containerName}'");
      }

      // Download the blob content (< 100MB)
      var downloadResult = await blobClient.DownloadContentAsync(cancellationToken: cancellationToken);
      var memoryStream = new MemoryStream(downloadResult.Value.Content.ToArray());

      _logger.LogInformation(
          "Downloaded file from blob: {BlobPath} in container: {ContainerName}",
          blobPath,
          containerName);

      return Result.Success<Stream>(memoryStream);
    }
    catch (RequestFailedException ex)
    {
      _logger.LogError(ex,
          "Failed to download file {BlobPath} from container {ContainerName}",
          blobPath,
          containerName);
      return Result.Error($"Failed to download file: {ex.Message}");
    }
  }
}
