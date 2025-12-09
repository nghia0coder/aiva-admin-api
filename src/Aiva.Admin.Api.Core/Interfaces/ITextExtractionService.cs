namespace Aiva.Admin.Api.Core.Interfaces;

using Core.Commons.Results;

/// <summary>
/// Service for extracting text content from various file types
/// </summary>
public interface ITextExtractionService
{
  /// <summary>
  /// Extracts text content from a file stream
  /// </summary>
  /// <param name="fileStream">The file content stream</param>
  /// <param name="fileName">Original file name with extension</param>
  /// <param name="contentType">MIME type of the file</param>
  /// <param name="cancellationToken">Cancellation token</param>
  /// <returns>Extraction result containing text and metadata</returns>
  Task<TextExtractionResult> ExtractTextAsync(
      Stream fileStream,
      string fileName,
      string contentType,
      CancellationToken cancellationToken = default);

  /// <summary>
  /// Checks if the file type is supported for text extraction
  /// </summary>
  bool IsSupported(string extension);
}
