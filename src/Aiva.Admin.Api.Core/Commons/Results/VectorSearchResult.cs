namespace Aiva.Admin.Api.Core.Commons.Models;

/// <summary>
/// Represents a search result from vector similarity search
/// </summary>
public sealed record VectorSearchResult
{
  /// <summary>
  /// The chunk ID
  /// </summary>
  public required string ChunkId { get; init; }

  /// <summary>
  /// The document ID this chunk belongs to
  /// </summary>
  public required string DocumentId { get; init; }

  /// <summary>
  /// The text content of the chunk
  /// </summary>
  public required string Content { get; init; }

  /// <summary>
  /// Similarity score (0-1, higher is more similar)
  /// </summary>
  public required double Score { get; init; }

  /// <summary>
  /// Associated metadata
  /// </summary>
  public DocumentChunkMetadata? Metadata { get; init; }
}
