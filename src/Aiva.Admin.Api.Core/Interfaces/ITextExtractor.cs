namespace Aiva.Admin.Api.Infrastructure.TextExtraction.Extractors;

using Core.Commons.Results;

/// <summary>
/// Strategy interface for file-type specific text extractors
/// </summary>
public interface ITextExtractor
{
  /// <summary>
  /// File extensions this extractor supports
  /// </summary>
  IReadOnlyCollection<string> SupportedExtensions { get; }

  /// <summary>
  /// Extracts text from the file stream
  /// </summary>
  Task<TextExtractionResult> ExtractAsync(
      Stream fileStream,
      string fileName,
      CancellationToken cancellationToken = default);
}
