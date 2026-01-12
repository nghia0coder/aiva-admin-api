namespace Aiva.Admin.Api.Core.Commons.Models;

/// <summary>
/// Represents a chunk of a document with its embedding
/// </summary>
public sealed record DocumentChunk
{
  /// <summary>
  /// Unique identifier for this chunk
  /// </summary>
  public required string Id { get; init; }

  /// <summary>
  /// Reference to the source document
  /// </summary>
  public required string DocumentId { get; init; }

  /// <summary>
  /// The text content of this chunk
  /// </summary>
  public required string Content { get; init; }

  /// <summary>
  /// The embedding vector for this chunk
  /// </summary>
  public required ReadOnlyMemory<float> Embedding { get; init; }

  /// <summary>
  /// Zero-based index of this chunk within the document
  /// </summary>
  public required int ChunkIndex { get; init; }

  /// <summary>
  /// Additional metadata for the chunk
  /// </summary>
  public DocumentChunkMetadata Metadata { get; init; } = new();
}

/// <summary>
/// Metadata associated with a document chunk
/// </summary>
public sealed record DocumentChunkMetadata
{
  /// <summary>
  /// Original file name
  /// </summary>
  public string? FileName { get; init; }

  /// <summary>
  /// Page number (for paginated documents)
  /// </summary>
  public int? PageNumber { get; init; }

  /// <summary>
  /// Section title or heading
  /// </summary>
  public string? SectionTitle { get; init; }

  /// <summary>
  /// Storage ID this document belongs to
  /// </summary>
  public int? StorageId { get; init; }

  /// <summary>
  /// Folder ID this document belongs to
  /// </summary>
  public int? FolderId { get; init; }

  /// <summary>
  /// Total number of chunks in the document
  /// </summary>
  public int? TotalChunks { get; init; }

  /// <summary>
  /// Character offset in original document
  /// </summary>
  public int? CharacterOffset { get; init; }

  /// <summary>
  /// Content type of the source file
  /// </summary>
  public string? ContentType { get; init; }

  /// <summary>
  /// Product ID/SKU
  /// </summary>
  public string? ProductId { get; init; }

  /// <summary>
  /// Product name
  /// </summary>
  public string? ProductName { get; init; }

  /// <summary>
  /// Product category
  /// </summary>
  public string? Category { get; init; }

  /// <summary>
  /// Product brand
  /// </summary>
  public string? Brand { get; init; }

  /// <summary>
  /// Product price
  /// </summary>
  public double? Price { get; init; }

  /// <summary>
  /// Product tags
  /// </summary>
  public IReadOnlyList<string>? Tags { get; init; }

  /// <summary>
  /// When the document was last modified
  /// </summary>
  public DateTime? LastModified { get; init; }

  /// <summary>
  /// Custom metadata as key-value pairs
  /// </summary>
  public Dictionary<string, string>? CustomMetadata { get; init; }
}
