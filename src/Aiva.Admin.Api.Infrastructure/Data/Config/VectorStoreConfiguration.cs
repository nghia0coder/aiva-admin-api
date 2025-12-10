namespace Aiva.Admin.Api.Infrastructure.VectorStore;

/// <summary>
/// Configuration for vector store services
/// </summary>
public sealed class VectorStoreConfiguration
{
  public const string SectionName = "VectorStore";

  /// <summary>
  /// The provider to use: "Qdrant" or "AzureAISearch"
  /// </summary>
  public string Provider { get; set; } = "Qdrant";

  /// <summary>
  /// Default collection/index name
  /// </summary>
  public string DefaultCollectionName { get; set; } = "aiva-documents";
}
