namespace Aiva.Admin.Api.Infrastructure.TextExtraction;

using Core.Commons.Results;
using Core.Interfaces;
using Extractors;

public sealed class TextExtractionService : ITextExtractionService
{
  private readonly IEnumerable<ITextExtractor> _extractors;
  private readonly TextExtractionConfiguration _config;
  private readonly ILogger<TextExtractionService> _logger;
  private readonly Dictionary<string, ITextExtractor> _extractorMap;

  public TextExtractionService(
      IEnumerable<ITextExtractor> extractors,
      IOptions<TextExtractionConfiguration> config,
      ILogger<TextExtractionService> logger)
  {
    _extractors = extractors;
    _config = config.Value;
    _logger = logger;

    // Build lookup map for O(1) extractor resolution
    _extractorMap = new Dictionary<string, ITextExtractor>(StringComparer.OrdinalIgnoreCase);
    foreach (var extractor in extractors)
    {
      foreach (var ext in extractor.SupportedExtensions)
      {
        _extractorMap[ext] = extractor;
      }
    }
  }

  public bool IsSupported(string extension)
  {
    var normalizedExt = extension.StartsWith('.') ? extension : $".{extension}";
    return _extractorMap.ContainsKey(normalizedExt);
  }

  public async Task<TextExtractionResult> ExtractTextAsync(
      Stream fileStream,
      string fileName,
      string contentType,
      CancellationToken cancellationToken = default)
  {
    var extension = Path.GetExtension(fileName).ToLowerInvariant();

    _logger.LogInformation(
        "Starting text extraction for file {FileName} with extension {Extension}",
        fileName, extension);

    // Validate file size
    if (fileStream.Length > _config.MaxFileSizeBytes)
    {
      return TextExtractionResult.Failure(
          $"File size ({fileStream.Length:N0} bytes) exceeds maximum allowed ({_config.MaxFileSizeBytes:N0} bytes)");
    }

    // Find appropriate extractor
    if (!_extractorMap.TryGetValue(extension, out var extractor))
    {
      return TextExtractionResult.Failure($"No extractor available for extension '{extension}'");
    }

    try
    {
      using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
      cts.CancelAfter(TimeSpan.FromSeconds(_config.TimeoutSeconds));

      var result = await extractor.ExtractAsync(fileStream, fileName, cts.Token);

      // Truncate if too long
      if (result.IsSuccess &&
          result.ExtractedText?.Length > _config.MaxExtractedTextLength)
      {
        var truncatedText = result.ExtractedText[.._config.MaxExtractedTextLength];
        result = result with
        {
          ExtractedText = truncatedText,
          AdditionalMetadata = new Dictionary<string, object>(result.AdditionalMetadata ?? [])
          {
            ["Truncated"] = true,
            ["OriginalLength"] = result.ExtractedText.Length
          }
        };
      }

      _logger.LogInformation(
          "Text extraction completed for {FileName}: Success={Success}, WordCount={WordCount}",
          fileName, result.IsSuccess, result.WordCount);

      return result;
    }
    catch (OperationCanceledException)
    {
      return TextExtractionResult.Failure($"Extraction timed out after {_config.TimeoutSeconds} seconds");
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Text extraction failed for {FileName}", fileName);
      return TextExtractionResult.Failure($"Extraction failed: {ex.Message}");
    }
  }
}
