using System.Text;

namespace Aiva.Admin.Api.Infrastructure.TextExtraction.Extractors;

using Aiva.Admin.Api.Core.Commons.Results;

public sealed class PlainTextExtractor : ITextExtractor
{
  public IReadOnlyCollection<string> SupportedExtensions => [".txt", ".md", ".json", ".xml", ".csv"];

  public async Task<TextExtractionResult> ExtractAsync(
      Stream fileStream,
      string fileName,
      CancellationToken cancellationToken = default)
  {
    try
    {
      using var reader = new StreamReader(fileStream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
      var extractedText = await reader.ReadToEndAsync(cancellationToken);

      return TextExtractionResult.Success(
          extractedText.Trim(),
          additionalMetadata: new Dictionary<string, object>
          {
            ["DocumentType"] = "PlainText",
            ["Encoding"] = reader.CurrentEncoding.EncodingName
          });
    }
    catch (Exception ex)
    {
      return TextExtractionResult.Failure($"Text extraction failed: {ex.Message}");
    }
  }
}
