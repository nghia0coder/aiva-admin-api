namespace Aiva.Admin.Api.Infrastructure.Data.Config;

/// <summary>
/// Configuration specific to Azure AI Search
/// </summary>
public sealed class AzureAISearchConfiguration
{
  public const string SectionName = "AzureAISearch";

  /// <summary>
  /// Azure AI Search endpoint
  /// </summary>
  public string Endpoint { get; set; } = string.Empty;

  /// <summary>
  /// Admin API Key
  /// </summary>
  public string? ApiKey { get; set; }

  /// <summary>
  /// Use Managed Identity instead of API Key
  /// </summary>
  public bool UseManagedIdentity { get; set; } = false;

  /// <summary>
  /// Semantic configuration name for hybrid search
  /// </summary>
  public string SemanticConfigurationName { get; set; } = "product-semantic-config";
}
