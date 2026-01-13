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

  /// <summary>
  /// Semantic caption from Azure AI Search (if semantic ranking was used)
  /// </summary>
  public string? SemanticCaption { get; set; }

  /// <summary>
  /// Highlighted portions of the semantic caption
  /// </summary>
  public string? SemanticCaptionHighlights { get; set; }

  /// <summary>
  /// Semantic answer from Azure AI Search (if available)
  /// </summary>
  public string? SemanticAnswer { get; set; }

  /// <summary>
  /// Score for the semantic answer
  /// </summary>
  public double? SemanticAnswerScore { get; set; }
}
