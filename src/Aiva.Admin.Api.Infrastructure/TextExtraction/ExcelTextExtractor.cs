using System.Text;
using ClosedXML.Excel;

namespace Aiva.Admin.Api.Infrastructure.TextExtraction.Extractors;

using Aiva.Admin.Api.Core.Commons.Results;

public sealed class ExcelTextExtractor : ITextExtractor
{
  public IReadOnlyCollection<string> SupportedExtensions => [".xlsx", ".xls"];

  public Task<TextExtractionResult> ExtractAsync(
      Stream fileStream,
      string fileName,
      CancellationToken cancellationToken = default)
  {
    try
    {
      var extension = Path.GetExtension(fileName).ToLowerInvariant();

      if (extension == ".xls")
      {
        return Task.FromResult(TextExtractionResult.Failure(
            "Legacy .xls format requires conversion. Please use .xlsx format."));
      }

      using var memoryStream = new MemoryStream();
      fileStream.CopyTo(memoryStream);
      memoryStream.Position = 0;

      using var workbook = new XLWorkbook(memoryStream);
      var textBuilder = new StringBuilder();
      var sheetCount = 0;

      foreach (var worksheet in workbook.Worksheets)
      {
        cancellationToken.ThrowIfCancellationRequested();
        sheetCount++;
        textBuilder.AppendLine($"=== Sheet: {worksheet.Name} ===");

        var usedRange = worksheet.RangeUsed();
        if (usedRange == null) continue;

        foreach (var row in usedRange.Rows())
        {
          var rowTexts = row.Cells().Select(c => c.GetString()).Where(s => !string.IsNullOrWhiteSpace(s));
          textBuilder.AppendLine(string.Join("\t", rowTexts));
        }
        textBuilder.AppendLine();
      }

      var extractedText = textBuilder.ToString().Trim();

      return Task.FromResult(TextExtractionResult.Success(
          extractedText,
          pageCount: sheetCount,
          additionalMetadata: new Dictionary<string, object>
          {
            ["SheetCount"] = sheetCount,
            ["DocumentType"] = "Excel"
          }));
    }
    catch (Exception ex)
    {
      return Task.FromResult(TextExtractionResult.Failure($"Excel extraction failed: {ex.Message}"));
    }
  }
}
