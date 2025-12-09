namespace Aiva.Admin.Api.Infrastructure.TextExtraction.Extractors;

using Aiva.Admin.Api.Core.Commons.Results;

/// <summary>
/// Image text extractor using OCR
/// For production: Use Azure Computer Vision or Document Intelligence
/// For local dev: Use Tesseract OCR
/// </summary>
public sealed class ImageTextExtractor : ITextExtractor
{
  private readonly ILogger<ImageTextExtractor> _logger;
  private readonly TextExtractionConfiguration _config;

  public ImageTextExtractor(
      ILogger<ImageTextExtractor> logger,
      IOptions<TextExtractionConfiguration> config)
  {
    _logger = logger;
    _config = config.Value;
  }

  public IReadOnlyCollection<string> SupportedExtensions => [".jpg", ".jpeg", ".png"];

  public async Task<TextExtractionResult> ExtractAsync(
      Stream fileStream,
      string fileName,
      CancellationToken cancellationToken = default)
  {
    try
    {
      // Option 1: Use Azure Computer Vision (recommended for production)
      if (_config.UseAzureDocumentIntelligence)
      {
        return await ExtractWithAzureAsync(fileStream, cancellationToken);
      }

      // Option 2: For images without OCR, return empty but successful
      _logger.LogWarning(
          "OCR not configured for image {FileName}. Consider enabling Azure Document Intelligence.",
          fileName);

      return TextExtractionResult.Success(
          string.Empty,
          additionalMetadata: new Dictionary<string, object>
          {
            ["DocumentType"] = "Image",
            ["OcrEnabled"] = false,
            ["Note"] = "OCR not configured. Enable Azure Document Intelligence for text extraction from images."
          });
    }
    catch (Exception ex)
    {
      return TextExtractionResult.Failure($"Image extraction failed: {ex.Message}");
    }
  }

  private async Task<TextExtractionResult> ExtractWithAzureAsync(
      Stream fileStream,
      CancellationToken cancellationToken)
  {
    // Implement Azure Computer Vision OCR here
    // This is a placeholder - actual implementation would use Azure.AI.Vision.ImageAnalysis
    await Task.CompletedTask;

    return TextExtractionResult.Failure("Azure OCR not implemented yet");
  }
}
