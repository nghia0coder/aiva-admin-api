namespace Aiva.Admin.Api.Core.Interfaces;

using Commons.Models;

/// <summary>
/// Abstract interface for vector database operations
/// Supports both Qdrant and Azure AI Search implementations
/// </summary>
public interface IVectorStoreService
{
  /// <summary>
  /// Ensures the vector collection/index exists
  /// </summary>
  Task<Result> EnsureCollectionExistsAsync(
      string collectionName,
      int vectorDimension,
      CancellationToken cancellationToken = default);

  /// <summary>
  /// Upserts document chunks with their embeddings
  /// </summary>
  Task<Result> UpsertChunksAsync(
      string collectionName,
      IReadOnlyList<DocumentChunk> chunks,
      CancellationToken cancellationToken = default);

  /// <summary>
  /// Searches for similar documents using vector similarity
  /// </summary>
  Task<Result<IReadOnlyList<VectorSearchResult>>> SearchAsync(
      string collectionName,
      ReadOnlyMemory<float> queryVector,
      VectorSearchOptions options,
      CancellationToken cancellationToken = default);

  /// <summary>
  /// Deletes all chunks associated with a document
  /// </summary>
  Task<Result> DeleteByDocumentIdAsync(
      string collectionName,
      string documentId,
      CancellationToken cancellationToken = default);

  /// <summary>
  /// Deletes an entire collection/index
  /// </summary>
  Task<Result> DeleteCollectionAsync(
      string collectionName,
      CancellationToken cancellationToken = default);

  /// <summary>
  /// Checks if a collection exists
  /// </summary>
  Task<bool> CollectionExistsAsync(
      string collectionName,
      CancellationToken cancellationToken = default);
}
