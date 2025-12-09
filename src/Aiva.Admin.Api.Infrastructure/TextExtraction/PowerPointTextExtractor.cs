using System.Text;
using DocumentFormat.OpenXml.Packaging;

namespace Aiva.Admin.Api.Infrastructure.TextExtraction.Extractors;

using Core.Commons.Results;
using DocumentFormat.OpenXml.Presentation;

public sealed class PowerPointTextExtractor : ITextExtractor
{
  public IReadOnlyCollection<string> SupportedExtensions => [".pptx", ".ppt"];

  public Task<TextExtractionResult> ExtractAsync(
      Stream fileStream,
      string fileName,
      CancellationToken cancellationToken = default)
  {
    try
    {
      var extension = Path.GetExtension(fileName).ToLowerInvariant();

      if (extension == ".ppt")
      {
        return Task.FromResult(TextExtractionResult.Failure(
            "Legacy .ppt format requires conversion. Please use .pptx format."));
      }

      using var memoryStream = new MemoryStream();
      fileStream.CopyTo(memoryStream);
      memoryStream.Position = 0;

      using var presentation = PresentationDocument.Open(memoryStream, false);
      var textBuilder = new StringBuilder();
      var slideCount = 0;

      var presentationPart = presentation.PresentationPart;
      if (presentationPart?.Presentation?.SlideIdList == null)
        return Task.FromResult(TextExtractionResult.Failure("Unable to read presentation"));

      foreach (var slideId in presentationPart.Presentation.SlideIdList.Elements<SlideId>())
      {
        cancellationToken.ThrowIfCancellationRequested();
        slideCount++;

        var slidePart = (SlidePart?)presentationPart.GetPartById(slideId.RelationshipId!);
        if (slidePart?.Slide == null) continue;

        textBuilder.AppendLine($"=== Slide {slideCount} ===");

        var texts = slidePart.Slide.Descendants<DocumentFormat.OpenXml.Drawing.Text>()
            .Select(t => t.Text)
            .Where(t => !string.IsNullOrWhiteSpace(t));

        textBuilder.AppendLine(string.Join(" ", texts));
        textBuilder.AppendLine();
      }

      var extractedText = textBuilder.ToString().Trim();

      return Task.FromResult(TextExtractionResult.Success(
          extractedText,
          pageCount: slideCount,
          additionalMetadata: new Dictionary<string, object>
          {
            ["SlideCount"] = slideCount,
            ["DocumentType"] = "PowerPoint"
          }));
    }
    catch (Exception ex)
    {
      return Task.FromResult(TextExtractionResult.Failure($"PowerPoint extraction failed: {ex.Message}"));
    }
  }
}
