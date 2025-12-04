namespace Aiva.Admin.Api.Infrastructure.AzureAI;

public sealed class AzureAIConfiguration
{
  public const string SectionName = "AzureAI";

  /// <summary>
  /// Azure OpenAI endpoint (e.g., https://your-resource.openai.azure.com/)
  /// </summary>
  public string Endpoint { get; set; } = string.Empty;

  /// <summary>
  /// API Key for Azure OpenAI (optional if using Managed Identity)
  /// </summary>
  public string? ApiKey { get; set; }

  /// <summary>
  /// Deployment name for the chat model (e.g., gpt-4o, gpt-4o-mini)
  /// </summary>
  public string DeploymentName { get; set; } = "gpt-4o-mini";

  /// <summary>
  /// Use Azure Managed Identity instead of API Key
  /// </summary>
  public bool UseManagedIdentity { get; set; } = false;

  /// <summary>
  /// Maximum tokens for completion response
  /// </summary>
  public int MaxTokens { get; set; } = 2048;

  /// <summary>
  /// Temperature for response creativity (0.0 - 2.0)
  /// </summary>
  public float Temperature { get; set; } = 0.7f;

  /// <summary>
  /// Default system prompt for conversations
  /// </summary>
  public string DefaultSystemPrompt { get; set; } =
      "You are a helpful AI assistant. Be concise, accurate, and friendly.";
}
