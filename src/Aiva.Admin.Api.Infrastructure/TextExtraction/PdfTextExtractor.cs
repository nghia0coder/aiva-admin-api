using System.Text;
using System.Text.RegularExpressions;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.DocumentLayoutAnalysis.PageSegmenter;
using UglyToad.PdfPig.DocumentLayoutAnalysis.WordExtractor;

namespace Aiva.Admin.Api.Infrastructure.TextExtraction.Extractors;

using Aiva.Admin.Api.Core.Commons.Results;

public sealed class PdfTextExtractor : ITextExtractor
{
  public IReadOnlyCollection<string> SupportedExtensions => [".pdf"];

  // Threshold for detecting headings based on font size ratio
  private const double HeadingFontSizeRatio = 1.2;

  public Task<TextExtractionResult> ExtractAsync(
      Stream fileStream,
      string fileName,
      CancellationToken cancellationToken = default)
  {
    try
    {
      using var memoryStream = new MemoryStream();
      fileStream.CopyTo(memoryStream);
      memoryStream.Position = 0;

      using var document = PdfDocument.Open(memoryStream);
      var textBuilder = new StringBuilder();
      var metadata = new Dictionary<string, object> { ["DocumentType"] = "PDF" };
      var sections = new List<PdfSection>();

      // 1. Extract document metadata
      ExtractDocumentMetadata(document, textBuilder, metadata);

      // 2. Extract bookmarks (Table of Contents)
      ExtractBookmarks(document, textBuilder, metadata);

      // 3. Process each page with advanced extraction
      var pageCount = document.NumberOfPages;
      var allFontSizes = new List<double>();

      // First pass: collect font statistics
      foreach (var page in document.GetPages())
      {
        cancellationToken.ThrowIfCancellationRequested();
        var letters = page.Letters;
        allFontSizes.AddRange(letters.Select(l => l.FontSize));
      }

      var baseFontSize = allFontSizes.Count > 0
          ? allFontSizes.GroupBy(x => Math.Round(x, 1))
              .OrderByDescending(g => g.Count())
              .First().Key
          : 12.0;

      // Second pass: extract content with structure
      var pageNumber = 0;
      foreach (var page in document.GetPages())
      {
        cancellationToken.ThrowIfCancellationRequested();
        pageNumber++;

        // Add page marker for RAG citation
        textBuilder.AppendLine();
        textBuilder.AppendLine($"[Page {pageNumber}]");
        textBuilder.AppendLine();

        // Extract page content with layout analysis
        ExtractPageContent(page, textBuilder, sections, baseFontSize, pageNumber, cancellationToken);

        // Extract annotations (comments, highlights)
        ExtractAnnotations(page, textBuilder);

        // Extract hyperlinks
        ExtractHyperlinks(page, textBuilder);
      }

      var extractedText = NormalizeText(textBuilder.ToString());

      // Check if PDF might be scanned (very little text)
      var wordCount = CountWords(extractedText);
      var isLikelyScanned = pageCount > 0 && (wordCount / pageCount) < 50;

      // Metadata for RAG
      metadata["PageCount"] = pageCount;
      metadata["WordCount"] = wordCount;
      metadata["SectionCount"] = sections.Count;
      metadata["IsLikelyScanned"] = isLikelyScanned;
      metadata["BaseFontSize"] = baseFontSize;

      if (isLikelyScanned)
      {
        metadata["Warning"] = "PDF appears to be scanned. OCR may be required for better extraction.";
      }

      return Task.FromResult(TextExtractionResult.Success(
          extractedText,
          pageCount: pageCount,
          additionalMetadata: metadata));
    }
    catch (Exception ex)
    {
      return Task.FromResult(TextExtractionResult.Failure($"PDF extraction failed: {ex.Message}"));
    }
  }

  private record PdfSection(string Title, int PageNumber, int Level);

  private static void ExtractDocumentMetadata(
      PdfDocument document,
      StringBuilder textBuilder,
      Dictionary<string, object> metadata)
  {
    var info = document.Information;

    metadata["PdfVersion"] = document.Version.ToString();

    if (!string.IsNullOrWhiteSpace(info.Title))
    {
      metadata["Title"] = info.Title;
      textBuilder.AppendLine($"# {info.Title}");
      textBuilder.AppendLine();
    }

    if (!string.IsNullOrWhiteSpace(info.Author))
      metadata["Author"] = info.Author;

    if (!string.IsNullOrWhiteSpace(info.Subject))
      metadata["Subject"] = info.Subject;

    if (!string.IsNullOrWhiteSpace(info.Keywords))
      metadata["Keywords"] = info.Keywords;

    if (!string.IsNullOrWhiteSpace(info.Creator))
      metadata["Creator"] = info.Creator;

    if (info.CreationDate != null)
      metadata["CreatedDate"] = info.CreationDate;

    if (info.ModifiedDate != null)
      metadata["ModifiedDate"] = info.ModifiedDate;
  }

  private static void ExtractBookmarks(
      PdfDocument document,
      StringBuilder textBuilder,
      Dictionary<string, object> metadata)
  {
    try
    {
      if (!document.TryGetBookmarks(out var bookmarks)) return;
      if (bookmarks?.Roots == null || !bookmarks.Roots.Any()) return;

      var tocItems = new List<object>();
      textBuilder.AppendLine("## Table of Contents");
      textBuilder.AppendLine();

      void ProcessBookmark(UglyToad.PdfPig.Outline.BookmarkNode node, int level)
      {
        var indent = new string(' ', level * 2);
        var prefix = level == 0 ? "-" : "-";
        textBuilder.AppendLine($"{indent}{prefix} {node.Title}");

        tocItems.Add(new { node.Title, Level = level });

        foreach (var child in node.Children)
        {
          ProcessBookmark(child, level + 1);
        }
      }

      foreach (var root in bookmarks.Roots)
      {
        ProcessBookmark(root, 0);
      }

      textBuilder.AppendLine();
      metadata["TableOfContents"] = tocItems;
    }
    catch
    {
      // Bookmarks not available or error - continue without them
    }
  }

  private static void ExtractPageContent(
      Page page,
      StringBuilder textBuilder,
      List<PdfSection> sections,
      double baseFontSize,
      int pageNumber,
      CancellationToken cancellationToken)
  {
    // Use PdfPig's layout analysis for better text extraction
    var words = page.GetWords(NearestNeighbourWordExtractor.Instance).ToList();

    if (words.Count == 0)
    {
      // Fallback to basic text extraction
      var basicText = page.Text;
      if (!string.IsNullOrWhiteSpace(basicText))
      {
        textBuilder.AppendLine(basicText);
      }
      return;
    }

    // Get text blocks using page segmenter for better layout handling
    var blocks = DocstrumBoundingBoxes.Instance.GetBlocks(words).ToList();

    // Sort blocks by reading order (top-to-bottom, left-to-right)
    var orderedBlocks = blocks
        .OrderBy(b => b.BoundingBox.Top)
        .ThenBy(b => b.BoundingBox.Left)
        .ToList();

    foreach (var block in orderedBlocks)
    {
      cancellationToken.ThrowIfCancellationRequested();

      var blockText = block.Text.Trim();
      if (string.IsNullOrWhiteSpace(blockText)) continue;

      // Analyze if this block is a heading based on font characteristics
      var blockWords = block.TextLines
          .SelectMany(line => line.Words)
          .ToList();

      var avgFontSize = blockWords.Count > 0
          ? blockWords.Average(w => w.Letters.Average(l => l.FontSize))
          : baseFontSize;

      var isHeading = avgFontSize > baseFontSize * HeadingFontSizeRatio;
      var isBold = blockWords.Any(w => w.Letters.Any(l =>
          l.FontName?.Contains("Bold", StringComparison.OrdinalIgnoreCase) == true));

      // Check if it looks like a table (multiple aligned columns)
      var isLikelyTable = IsLikelyTable(block);

      if (isLikelyTable)
      {
        textBuilder.AppendLine();
        ExtractTableFromBlock(block, textBuilder);
        textBuilder.AppendLine();
      }
      else if (isHeading || isBold)
      {
        // Determine heading level based on font size ratio
        var level = avgFontSize > baseFontSize * 1.5 ? 1 :
                   avgFontSize > baseFontSize * 1.3 ? 2 : 3;

        var prefix = new string('#', level);
        textBuilder.AppendLine();
        textBuilder.AppendLine($"{prefix} {blockText}");
        textBuilder.AppendLine();

        sections.Add(new PdfSection(blockText, pageNumber, level));
      }
      else
      {
        // Regular paragraph
        textBuilder.AppendLine(blockText);
        textBuilder.AppendLine();
      }
    }
  }

  private static bool IsLikelyTable(UglyToad.PdfPig.DocumentLayoutAnalysis.TextBlock block)
  {
    // Simple heuristic: if there are multiple lines with similar horizontal positions
    // it might be a table
    var lines = block.TextLines.ToList();
    if (lines.Count < 2) return false;

    // Check if lines have multiple "columns" (words with consistent x-positions)
    var wordPositions = lines
        .SelectMany(l => l.Words)
        .Select(w => Math.Round(w.BoundingBox.Left, 0))
        .GroupBy(x => x)
        .Where(g => g.Count() >= lines.Count * 0.5) // At least half the lines have words at this position
        .ToList();

    return wordPositions.Count >= 3; // At least 3 columns
  }

  private static void ExtractTableFromBlock(
      UglyToad.PdfPig.DocumentLayoutAnalysis.TextBlock block,
      StringBuilder textBuilder)
  {
    var lines = block.TextLines.ToList();

    // Get all unique x-positions to determine columns
    var allWords = lines.SelectMany(l => l.Words).ToList();
    var xPositions = allWords
        .Select(w => Math.Round(w.BoundingBox.Left, 0))
        .Distinct()
        .OrderBy(x => x)
        .ToList();

    // Group nearby x-positions into columns (within 20 units)
    var columnBoundaries = new List<double> { xPositions.First() };
    foreach (var x in xPositions.Skip(1))
    {
      if (x - columnBoundaries.Last() > 50)
      {
        columnBoundaries.Add(x);
      }
    }

    if (columnBoundaries.Count < 2)
    {
      // Not really a table, just output as text
      textBuilder.AppendLine(block.Text);
      return;
    }

    // Extract table rows
    var isFirstRow = true;
    foreach (var line in lines)
    {
      var lineWords = line.Words.OrderBy(w => w.BoundingBox.Left).ToList();
      var cells = new string[columnBoundaries.Count];

      for (var i = 0; i < cells.Length; i++)
        cells[i] = "";

      foreach (var word in lineWords)
      {
        var wordX = word.BoundingBox.Left;
        var columnIndex = 0;
        for (var i = columnBoundaries.Count - 1; i >= 0; i--)
        {
          if (wordX >= columnBoundaries[i] - 25)
          {
            columnIndex = i;
            break;
          }
        }

        cells[columnIndex] += (cells[columnIndex].Length > 0 ? " " : "") + word.Text;
      }

      textBuilder.AppendLine($"| {string.Join(" | ", cells)} |");

      if (isFirstRow)
      {
        textBuilder.AppendLine($"| {string.Join(" | ", Enumerable.Repeat("---", columnBoundaries.Count))} |");
        isFirstRow = false;
      }
    }
  }

  private static void ExtractAnnotations(Page page, StringBuilder textBuilder)
  {
    try
    {
      var annotations = page.ExperimentalAccess.GetAnnotations().ToList();

      var textAnnotations = annotations
          .Where(a => !string.IsNullOrWhiteSpace(a.Content))
          .ToList();

      if (textAnnotations.Count == 0) return;

      textBuilder.AppendLine();
      textBuilder.AppendLine("[Annotations]");

      foreach (var annotation in textAnnotations)
      {
        var type = annotation.Type.ToString();
        textBuilder.AppendLine($"- [{type}]: {annotation.Content}");
      }
    }
    catch
    {
      // Annotations not available - continue
    }
  }

  private static void ExtractHyperlinks(Page page, StringBuilder textBuilder)
  {
    try
    {
      var annotations = page.ExperimentalAccess.GetAnnotations()
          .Where(a => a.Type == UglyToad.PdfPig.Annotations.AnnotationType.Link)
          .ToList();

      if (annotations.Count == 0) return;

      var links = new List<string>();
      foreach (var link in annotations)
      {
        // Try to get the URI from the action
        var action = link.Action;
        if (action != null)
        {
          // Extract URI if available
          var uri = action.GetType().GetProperty("Uri")?.GetValue(action)?.ToString();
          if (!string.IsNullOrEmpty(uri))
          {
            links.Add(uri);
          }
        }
      }

      if (links.Count > 0)
      {
        textBuilder.AppendLine();
        textBuilder.AppendLine("[Links]");
        foreach (var uri in links.Distinct())
        {
          textBuilder.AppendLine($"- {uri}");
        }
      }
    }
    catch
    {
      // Links extraction failed - continue
    }
  }

  private static string NormalizeText(string text)
  {
    if (string.IsNullOrEmpty(text)) return text;

    // Fix common PDF extraction issues

    // 1. Fix broken words (hy- phenation)
    text = Regex.Replace(text, @"(\w+)-\s*\n\s*(\w+)", "$1$2");

    // 2. Normalize line endings
    text = text.Replace("\r\n", "\n").Replace("\r", "\n");

    // 3. Fix multiple spaces
    text = Regex.Replace(text, @"[^\S\n]+", " ");

    // 4. Remove excessive blank lines
    text = Regex.Replace(text, @"\n{3,}", "\n\n");

    // 5. Fix common character encoding issues
    text = text
        .Replace("ﬁ", "fi")
        .Replace("ﬂ", "fl")
        .Replace("ﬀ", "ff")
        .Replace("ﬃ", "ffi")
        .Replace("ﬄ", "ffl")
        .Replace("–", "-")
        .Replace("—", "-")
        .Replace("\"", "\\\"")
        .Replace("\"", "\"")
        .Replace("'", "'")
        .Replace("'", "'")
        .Replace("…", "...");

    // 6. Normalize Unicode
    text = text.Normalize(NormalizationForm.FormC);

    // 7. Remove zero-width characters
    text = Regex.Replace(text, @"[\u200B-\u200D\uFEFF]", "");

    return text.Trim();
  }

  private static int CountWords(string text)
  {
    if (string.IsNullOrWhiteSpace(text)) return 0;
    return Regex.Matches(text, @"[\w]+").Count;
  }
}
