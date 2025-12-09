namespace Aiva.Admin.Api.Core.Commons.Results;

/// <summary>
/// Result of text extraction operation
/// </summary>
public sealed record TextExtractionResult
{
  public bool IsSuccess { get; init; }
  public string? ExtractedText { get; init; }
  public int? PageCount { get; init; }
  public int? WordCount { get; init; }
  public string? DetectedLanguage { get; init; }
  public string? ContentHash { get; init; }
  public string? ErrorMessage { get; init; }
  public Dictionary<string, object>? AdditionalMetadata { get; init; }

  public static TextExtractionResult Success(
      string extractedText,
      int? pageCount = null,
      string? detectedLanguage = null,
      Dictionary<string, object>? additionalMetadata = null)
  {
    var wordCount = string.IsNullOrWhiteSpace(extractedText)
        ? 0
        : extractedText.Split([' ', '\n', '\r', '\t'], StringSplitOptions.RemoveEmptyEntries).Length;

    return new TextExtractionResult
    {
      IsSuccess = true,
      ExtractedText = extractedText,
      PageCount = pageCount,
      WordCount = wordCount,
      DetectedLanguage = detectedLanguage,
      ContentHash = ComputeHash(extractedText),
      AdditionalMetadata = additionalMetadata
    };
  }

  public static TextExtractionResult Failure(string errorMessage) => new()
  {
    IsSuccess = false,
    ErrorMessage = errorMessage
  };

  private static string ComputeHash(string content)
  {
    using var sha256 = System.Security.Cryptography.SHA256.Create();
    var bytes = System.Text.Encoding.UTF8.GetBytes(content);
    var hash = sha256.ComputeHash(bytes);
    return Convert.ToHexString(hash);
  }
}
