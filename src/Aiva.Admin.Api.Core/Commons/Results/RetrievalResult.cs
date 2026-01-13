namespace Aiva.Admin.Api.Core.Commons.Results;

using Models;

public sealed record RetrievalResult
{
  public IReadOnlyList<VectorSearchResult> Results { get; init; } = [];
  public string FormattedContext { get; init; } = string.Empty;
  public int TokenCount { get; init; }
}
