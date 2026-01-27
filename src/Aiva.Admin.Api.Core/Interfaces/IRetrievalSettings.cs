namespace Aiva.Admin.Api.Core.Interfaces;

/// <summary>
/// Settings interface for RAG retrieval and out-of-scope detection.
/// Implemented by infrastructure configuration.
/// </summary>
public interface IRetrievalSettings
{
  /// <summary>
  /// Minimum relevance score threshold (0.0-1.0). Documents below this score are filtered out.
  /// Used for vector-only search (typically 0.7-0.8).
  /// </summary>
  double MinScoreThreshold { get; }

  /// <summary>
  /// Minimum relevance score threshold for hybrid search (0.0-1.0).
  /// Hybrid search with semantic ranking typically produces lower scores (0.01-0.1),
  /// so this threshold should be lower than MinScoreThreshold.
  /// If not set, falls back to MinScoreThreshold.
  /// </summary>
  double? HybridSearchMinScoreThreshold { get; }

  /// <summary>
  /// Minimum number of results required to proceed with LLM generation.
  /// </summary>
  int MinResultCount { get; }

  /// <summary>
  /// Enable or disable out-of-scope detection.
  /// </summary>
  bool EnableOutOfScopeDetection { get; }

  /// <summary>
  /// Number of top documents to retrieve.
  /// </summary>
  int TopK { get; }
}
