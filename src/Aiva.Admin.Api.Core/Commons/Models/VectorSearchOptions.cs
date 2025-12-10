namespace Aiva.Admin.Api.Core.Commons.Models;

/// <summary>
/// Options for vector similarity search
/// </summary>
public sealed record VectorSearchOptions
{
  /// <summary>
  /// Maximum number of results to return
  /// </summary>
  public int TopK { get; init; } = 5;

  /// <summary>
  /// Minimum similarity score threshold (0-1)
  /// </summary>
  public double? MinScore { get; init; }

  /// <summary>
  /// Filter by document IDs
  /// </summary>
  public IReadOnlyList<string>? DocumentIds { get; init; }

  /// <summary>
  /// Filter by storage ID
  /// </summary>
  public Guid? StorageId { get; init; }

  /// <summary>
  /// Filter by folder ID
  /// </summary>
  public Guid? FolderId { get; init; }

  /// <summary>
  /// Include the embedding vectors in results
  /// </summary>
  public bool IncludeVectors { get; init; } = false;
}
