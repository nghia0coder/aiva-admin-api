namespace Aiva.Admin.Api.Infrastructure.TextExtraction;

public sealed class TextExtractionConfiguration
{
  public const string SectionName = "TextExtraction";

  /// <summary>
  /// Use Azure Document Intelligence for extraction (recommended for production)
  /// </summary>
  public bool UseAzureDocumentIntelligence { get; set; } = false;

  /// <summary>
  /// Azure Document Intelligence endpoint
  /// </summary>
  public string? AzureDocumentIntelligenceEndpoint { get; set; }

  /// <summary>
  /// Azure Document Intelligence API Key
  /// </summary>
  public string? AzureDocumentIntelligenceKey { get; set; }

  /// <summary>
  /// Maximum file size for text extraction (in bytes)
  /// </summary>
  public long MaxFileSizeBytes { get; set; } = 50 * 1024 * 1024; // 50MB

  /// <summary>
  /// Maximum extracted text length (characters)
  /// </summary>
  public int MaxExtractedTextLength { get; set; } = 1_000_000; // 1M chars

  /// <summary>
  /// Timeout for extraction operations (seconds)
  /// </summary>
  public int TimeoutSeconds { get; set; } = 300; // 5 minutes
}
