namespace Aiva.Admin.Api.Infrastructure.Data.Config;

/// <summary>
/// Configuration specific to Qdrant
/// </summary>
public sealed class QdrantConfiguration
{
  public const string SectionName = "Qdrant";

  /// <summary>
  /// Qdrant endpoint URL
  /// </summary>
  public string Endpoint { get; set; } = "http://localhost:6334";

  /// <summary>
  /// API Key for Qdrant (optional for local development)
  /// </summary>
  public string? ApiKey { get; set; }

  /// <summary>
  /// Use HTTPS for connection
  /// </summary>
  public bool UseHttps { get; set; } = false;
}
