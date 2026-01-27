namespace Aiva.Admin.Api.Core.Commons.Results;

using Models;

public sealed record RetrievalResult
{
  public IReadOnlyList<VectorSearchResult> Results { get; init; } = [];
  public string FormattedContext { get; init; } = string.Empty;
  public int TokenCount { get; init; }

  /// <summary>
  /// Gets the highest relevance score among all results.
  /// Returns 0 if no results are available.
  /// </summary>
  public double TopScore => Results.Count > 0
      ? Results.Max(r => r.Score)
      : 0;

  /// <summary>
  /// Gets the average relevance score across all results.
  /// Returns 0 if no results are available.
  /// </summary>
  public double AverageScore => Results.Count > 0
      ? Results.Average(r => r.Score)
      : 0;

  /// <summary>
  /// Determines whether the retrieval result has sufficient context to proceed with LLM generation.
  /// Used for out-of-scope detection in grounded RAG.
  /// </summary>
  /// <param name="minScore">Minimum required score for the top result</param>
  /// <param name="minResults">Minimum number of results required</param>
  /// <returns>True if context is sufficient; otherwise, false</returns>
  public bool HasSufficientContext(double minScore = 0.7, int minResults = 1) =>
      Results.Count >= minResults && TopScore >= minScore;
}
