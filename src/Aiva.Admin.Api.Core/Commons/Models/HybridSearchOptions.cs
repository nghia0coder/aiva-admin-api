namespace Aiva.Admin.Api.Core.Commons.Models;

public sealed record HybridSearchOptions
{
  public int TopK { get; init; } = 10;
  public double? MinScore { get; init; }
  public bool UseSemanticRanking { get; init; } = true;

  // Filters
  public Guid? StorageId { get; init; }
  public Guid? FolderId { get; init; }
  public IReadOnlyList<string>? DocumentIds { get; init; }

  // Product filters (for e-commerce)
  public string? Category { get; init; }
  public string? Brand { get; init; }
  public double? MinPrice { get; init; }
  public double? MaxPrice { get; init; }
}
