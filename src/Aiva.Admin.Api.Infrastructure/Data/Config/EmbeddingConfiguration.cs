namespace Aiva.Admin.Api.Infrastructure.Embedding;

/// <summary>
/// Configuration for embedding generation
/// </summary>
public sealed class EmbeddingConfiguration
{
  public const string SectionName = "Embedding";

  /// <summary>
  /// Azure OpenAI endpoint for embeddings
  /// </summary>
  public string Endpoint { get; set; } = string.Empty;

  /// <summary>
  /// API Key (optional if using Managed Identity)
  /// </summary>
  public string? ApiKey { get; set; }

  /// <summary>
  /// Deployment name for embedding model (e.g., text-embedding-3-small)
  /// </summary>
  public string DeploymentName { get; set; } = "text-embedding-3-small";

  /// <summary>
  /// Use Managed Identity instead of API Key
  /// </summary>
  public bool UseManagedIdentity { get; set; } = false;

  /// <summary>
  /// Embedding vector dimension (text-embedding-3-small = 1536)
  /// </summary>
  public int Dimension { get; set; } = 1536;

  /// <summary>
  /// Maximum batch size for embedding requests
  /// </summary>
  public int MaxBatchSize { get; set; } = 16;

  /// <summary>
  /// Maximum input tokens per request
  /// </summary>
  public int MaxInputTokens { get; set; } = 8191;
}
