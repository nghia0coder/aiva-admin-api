namespace Aiva.Admin.Api.Core.Interfaces;

/// <summary>
/// Service for generating text embeddings using AI models
/// </summary>
public interface IEmbeddingService
{
  /// <summary>
  /// Generates embedding vector for a single text
  /// </summary>
  Task<Result<ReadOnlyMemory<float>>> GenerateEmbeddingAsync(
      string text,
      CancellationToken cancellationToken = default);

  /// <summary>
  /// Generates embeddings for multiple texts in batch
  /// </summary>
  Task<Result<IReadOnlyList<ReadOnlyMemory<float>>>> GenerateEmbeddingsAsync(
      IReadOnlyList<string> texts,
      CancellationToken cancellationToken = default);

  /// <summary>
  /// Gets the dimension of the embedding vectors
  /// </summary>
  int EmbeddingDimension { get; }

  /// <summary>
  /// Gets the model name used for embeddings
  /// </summary>
  string ModelName { get; }
}
