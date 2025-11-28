namespace Aiva.Admin.Api.Core.Interfaces;

/// <summary>
/// Service for managing Azure Blob Storage containers
/// </summary>
public interface IBlobStorageService
{
  /// <summary>
  /// Creates a new blob container with the specified name
  /// </summary>
  /// <param name="containerName">The name of the container to create</param>
  /// <param name="cancellationToken">Cancellation token</param>
  /// <returns>Result indicating success or failure with the container URI</returns>
  Task<Result<string>> CreateContainerAsync(string containerName, CancellationToken cancellationToken = default);

  /// <summary>
  /// Deletes a blob container with the specified name
  /// </summary>
  Task<Result> DeleteContainerAsync(string containerName, CancellationToken cancellationToken = default);

  /// <summary>
  /// Checks if a container exists
  /// </summary>
  Task<bool> ContainerExistsAsync(string containerName, CancellationToken cancellationToken = default);
}
