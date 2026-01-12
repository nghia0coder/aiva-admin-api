namespace Aiva.Admin.Api.Infrastructure.Configuration;

public class AppSettings
{
  public const string SectionName = "ApplicationSettings";

  public ConnectionStringsSettings ConnectionStrings { get; set; } = new();
  public DatabaseSettings Database { get; set; } = new();
  public SerilogSettings Serilog { get; set; } = new();
  public AzureAdSettings AzureAd { get; set; } = new();
  public AzureBlobStorageSettings AzureBlobStorage { get; set; } = new();
  public CorsSettings Cors { get; set; } = new();
  public AzureAISettings AzureAI { get; set; } = new();
  public EmbeddingSettings Embedding { get; set; } = new();
  public VectorStoreSettings VectorStore { get; set; } = new();
  public AzureAISearchSettings AzureAISearch { get; set; } = new();
  public MailserverSettings Mailserver { get; set; } = new();
  public WorkerSettings Worker { get; set; } = new();
  public TitleGenerationSettings TitleGeneration { get; set; } = new();
}

public class ConnectionStringsSettings
{
  public string AivaChatbotDb { get; set; } = "aiva-chatbot-db";
  public string DefaultConnection { get; set; } = string.Empty;
  public string SqliteConnection { get; set; } = string.Empty;
}

public class DatabaseSettings
{
  public bool ApplyMigrationsOnStartup { get; set; }
}

public class AzureAdSettings
{
  public string Instance { get; set; } = string.Empty;
  public string TenantId { get; set; } = string.Empty;
  public string ClientId { get; set; } = string.Empty;
  public string Audience { get; set; } = string.Empty;
  public string ScalarClientId { get; set; } = string.Empty;
}

public class AzureBlobStorageSettings
{
  public string ServiceUri { get; set; } = string.Empty;
  public bool UseAzureIdentity { get; set; }
  public string DefaultAccessTier { get; set; } = "Hot";
  public string AccountName { get; set; } = string.Empty;
  public string AccountKey { get; set; } = string.Empty;
}

public class CorsSettings
{
  public string[] AllowedOrigins { get; set; } = Array.Empty<string>();
  public string[] AllowedMethods { get; set; } = Array.Empty<string>();
  public string[] AllowedHeaders { get; set; } = Array.Empty<string>();
  public string[] ExposedHeaders { get; set; } = Array.Empty<string>();
  public bool AllowCredentials { get; set; }
  public int MaxAgeSeconds { get; set; }
}

public class AzureAISettings
{
  public string Endpoint { get; set; } = string.Empty;
  public string ApiKey { get; set; } = string.Empty;
  public string DeploymentName { get; set; } = "gpt-4o-mini";
  public bool UseManagedIdentity { get; set; }
  public int MaxTokens { get; set; } = 2048;
  public float? Temperature { get; set; } = 0.7f;
  public string DefaultSystemPrompt { get; set; } = string.Empty;
}

public class EmbeddingSettings
{
  public string Endpoint { get; set; } = string.Empty;
  public string ApiKey { get; set; } = string.Empty;
  public string DeploymentName { get; set; } = "text-embedding-3-small";
  public bool UseManagedIdentity { get; set; }
  public int Dimension { get; set; } = 1536;
  public int MaxBatchSize { get; set; } = 16;
  public int MaxInputTokens { get; set; } = 8191;
}

public class VectorStoreSettings
{
  public string DefaultCollectionName { get; set; } = "products-index";
}

public class AzureAISearchSettings
{
  public string Endpoint { get; set; } = string.Empty;
  public string ApiKey { get; set; } = string.Empty;
  public bool UseManagedIdentity { get; set; }
  public string SemanticConfigurationName { get; set; } = "product-semantic-config";
}

public class MailserverSettings
{
  public string Server { get; set; } = "localhost";
  public int Port { get; set; } = 25;
}

public class SerilogSettings
{
  public MinimumLevelSettings MinimumLevel { get; set; } = new();
  public List<WriteToSettings> WriteTo { get; set; } = new();
}

public class MinimumLevelSettings
{
  public string Default { get; set; } = "Information";
}

public class WriteToSettings
{
  public string Name { get; set; } = string.Empty;
  public Dictionary<string, object>? Args { get; set; }
}

public sealed class WorkerSettings
{
  public const string SectionName = "Worker";
  public int PollingIntervalSeconds { get; set; } = 10;
  public int BatchSize { get; set; } = 5;
  public int MaxConcurrency { get; set; } = 3;
  public bool Enabled { get; set; } = true;
}

public sealed class TitleGenerationSettings
{
  public const string SectionName = "TitleGeneration";
  public bool Enabled { get; set; } = true;
  public int PollingIntervalSeconds { get; set; } = 5;
  public int BatchSize { get; set; } = 10;
  public string? DeploymentName { get; set; }
  public int MaxTitleLength { get; set; } = 50;
}
