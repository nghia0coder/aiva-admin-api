namespace Aiva.Admin.Api.Core.Commons.Models;

public sealed record RetrievalOptions
{
  public int TopK { get; init; } = 5;
  public double MinScore { get; init; } = 0.7;
  public int? StorageId { get; init; }
  public int? FolderId { get; init; }
  public SearchStrategy Strategy { get; init; } = SearchStrategy.Hybrid;
}

public enum SearchStrategy
{
  VectorOnly,      // Pure semantic search
  KeywordOnly,     // Pure full-text search  
  Hybrid           // Combined (recommended for e-commerce)
}
