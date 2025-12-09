using System.Text;
using System.Text.RegularExpressions;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using DW = DocumentFormat.OpenXml.Drawing.Wordprocessing;

namespace Aiva.Admin.Api.Infrastructure.TextExtraction.Extractors;

using Aiva.Admin.Api.Core.Commons.Results;

public sealed class WordTextExtractor : ITextExtractor
{
  public IReadOnlyCollection<string> SupportedExtensions => [".docx", ".doc"];

  public Task<TextExtractionResult> ExtractAsync(
      Stream fileStream,
      string fileName,
      CancellationToken cancellationToken = default)
  {
    try
    {
      var extension = Path.GetExtension(fileName).ToLowerInvariant();

      if (extension == ".doc")
      {
        return Task.FromResult(TextExtractionResult.Failure(
            "Legacy .doc format requires conversion. Please use .docx format."));
      }

      using var memoryStream = new MemoryStream();
      fileStream.CopyTo(memoryStream);
      memoryStream.Position = 0;

      using var document = WordprocessingDocument.Open(memoryStream, false);
      var textBuilder = new StringBuilder();
      var metadata = new Dictionary<string, object> { ["DocumentType"] = "Word" };
      var sections = new List<DocumentSection>(); // For RAG chunking

      // 1. Document Properties
      ExtractDocumentProperties(document, textBuilder, metadata);

      // 2. Headers (deduplicated)
      ExtractHeaders(document, textBuilder, cancellationToken);

      // 3. Main Content with structure
      var body = document.MainDocumentPart?.Document?.Body;
      if (body != null)
      {
        ExtractBodyContentAdvanced(document, body, textBuilder, sections, cancellationToken);
      }

      // 4. Footers
      ExtractFooters(document, textBuilder, cancellationToken);

      // 5. Footnotes & Endnotes
      ExtractFootnotes(document, textBuilder, cancellationToken);
      ExtractEndnotes(document, textBuilder, cancellationToken);

      // 6. Comments
      ExtractComments(document, textBuilder, cancellationToken);

      var extractedText = NormalizeText(textBuilder.ToString());

      // Metadata for RAG
      metadata["WordCount"] = CountWords(extractedText);
      metadata["SectionCount"] = sections.Count;
      metadata["HasTables"] = sections.Any(s => s.Type == "Table");
      metadata["HasImages"] = sections.Any(s => s.Type == "Image");
      metadata["Sections"] = sections.Select(s => new { s.Title, s.Type, s.Level }).ToList();

      return Task.FromResult(TextExtractionResult.Success(extractedText, additionalMetadata: metadata));
    }
    catch (Exception ex)
    {
      return Task.FromResult(TextExtractionResult.Failure($"Word extraction failed: {ex.Message}"));
    }
  }

  private record DocumentSection(string Title, string Type, int Level, int StartPosition);

  private static void ExtractDocumentProperties(
      WordprocessingDocument document,
      StringBuilder textBuilder,
      Dictionary<string, object> metadata)
  {
    var coreProps = document.PackageProperties;

    if (!string.IsNullOrWhiteSpace(coreProps.Title))
    {
      metadata["Title"] = coreProps.Title;
      textBuilder.AppendLine($"# {coreProps.Title}");
      textBuilder.AppendLine();
    }

    if (!string.IsNullOrWhiteSpace(coreProps.Subject))
      metadata["Subject"] = coreProps.Subject;

    if (!string.IsNullOrWhiteSpace(coreProps.Keywords))
      metadata["Keywords"] = coreProps.Keywords;

    if (!string.IsNullOrWhiteSpace(coreProps.Creator))
      metadata["Author"] = coreProps.Creator;

    if (!string.IsNullOrWhiteSpace(coreProps.Description))
    {
      metadata["Description"] = coreProps.Description;
      textBuilder.AppendLine($"> {coreProps.Description}");
      textBuilder.AppendLine();
    }

    if (coreProps.Created.HasValue)
      metadata["CreatedDate"] = coreProps.Created.Value;

    if (coreProps.Modified.HasValue)
      metadata["ModifiedDate"] = coreProps.Modified.Value;
  }

  private static void ExtractHeaders(
      WordprocessingDocument document,
      StringBuilder textBuilder,
      CancellationToken cancellationToken)
  {
    var mainPart = document.MainDocumentPart;
    if (mainPart == null) return;

    var headerTexts = new HashSet<string>();

    foreach (var headerPart in mainPart.HeaderParts)
    {
      cancellationToken.ThrowIfCancellationRequested();
      var headerText = GetAllText(headerPart.Header);
      if (!string.IsNullOrWhiteSpace(headerText) && headerTexts.Add(headerText))
      {
        textBuilder.AppendLine($"[Header: {headerText}]");
      }
    }

    if (headerTexts.Count > 0)
      textBuilder.AppendLine();
  }

  private static void ExtractFooters(
      WordprocessingDocument document,
      StringBuilder textBuilder,
      CancellationToken cancellationToken)
  {
    var mainPart = document.MainDocumentPart;
    if (mainPart == null) return;

    var footerTexts = new HashSet<string>();

    foreach (var footerPart in mainPart.FooterParts)
    {
      cancellationToken.ThrowIfCancellationRequested();
      var footerText = GetAllText(footerPart.Footer);
      if (!string.IsNullOrWhiteSpace(footerText) && footerTexts.Add(footerText))
      {
        textBuilder.AppendLine($"[Footer: {footerText}]");
      }
    }
  }

  private static void ExtractBodyContentAdvanced(
      WordprocessingDocument document,
      Body body,
      StringBuilder textBuilder,
      List<DocumentSection> sections,
      CancellationToken cancellationToken)
  {
    var currentListLevel = -1;
    var numberedListCounters = new Dictionary<int, int>();

    foreach (var element in body.ChildElements)
    {
      cancellationToken.ThrowIfCancellationRequested();

      switch (element)
      {
        case Paragraph paragraph:
          ExtractParagraphAdvanced(document, paragraph, textBuilder, sections,
              ref currentListLevel, numberedListCounters);
          break;

        case Table table:
          sections.Add(new DocumentSection("Table", "Table", 0, textBuilder.Length));
          ExtractTableAdvanced(table, textBuilder, cancellationToken);
          currentListLevel = -1;
          break;

        case SdtBlock sdtBlock:
          var sdtText = GetAllText(sdtBlock);
          if (!string.IsNullOrWhiteSpace(sdtText))
          {
            textBuilder.AppendLine(sdtText);
          }
          break;
      }
    }
  }

  private static void ExtractParagraphAdvanced(
      WordprocessingDocument document,
      Paragraph paragraph,
      StringBuilder textBuilder,
      List<DocumentSection> sections,
      ref int currentListLevel,
      Dictionary<int, int> numberedListCounters)
  {
    var props = paragraph.ParagraphProperties;
    var text = GetParagraphText(document, paragraph);
    var styleId = props?.ParagraphStyleId?.Val?.Value ?? "";

    // Check for heading styles
    if (IsHeading(styleId, out var level))
    {
      var prefix = new string('#', level);
      textBuilder.AppendLine();
      textBuilder.AppendLine($"{prefix} {text}");
      textBuilder.AppendLine();
      sections.Add(new DocumentSection(text, "Heading", level, textBuilder.Length));
      currentListLevel = -1;
      return;
    }

    // Check for list items
    var numProps = props?.NumberingProperties;
    if (numProps != null)
    {
      var ilvl = numProps.NumberingLevelReference?.Val?.Value ?? 0;
      var numId = numProps.NumberingId?.Val?.Value ?? 0;

      var indent = new string(' ', ilvl * 2);

      // Detect if numbered or bulleted
      var isNumbered = IsNumberedList(document, numId, ilvl);

      if (isNumbered)
      {
        if (!numberedListCounters.ContainsKey(ilvl) || currentListLevel != ilvl)
          numberedListCounters[ilvl] = 0;

        numberedListCounters[ilvl]++;
        textBuilder.AppendLine($"{indent}{numberedListCounters[ilvl]}. {text}");
      }
      else
      {
        textBuilder.AppendLine($"{indent}- {text}");
      }

      currentListLevel = ilvl;
      return;
    }

    // Regular paragraph
    currentListLevel = -1;

    if (!string.IsNullOrWhiteSpace(text))
    {
      textBuilder.AppendLine(text);
    }
    else
    {
      textBuilder.AppendLine();
    }
  }

  private static string GetParagraphText(WordprocessingDocument document, Paragraph paragraph)
  {
    var sb = new StringBuilder();

    foreach (var child in paragraph.ChildElements)
    {
      switch (child)
      {
        case Run run:
          sb.Append(run.InnerText);
          break;

        case Hyperlink hyperlink:
          var linkText = hyperlink.InnerText;
          var url = GetHyperlinkUrl(document, hyperlink);
          if (!string.IsNullOrEmpty(url) && url != linkText)
          {
            sb.Append($"[{linkText}]({url})");
          }
          else
          {
            sb.Append(linkText);
          }
          break;

        case BookmarkStart bookmark:
          // Could track bookmarks for cross-references
          break;
      }
    }

    // Extract image alt text/descriptions
    foreach (var drawing in paragraph.Descendants<Drawing>())
    {
      var altText = GetImageDescription(drawing);
      if (!string.IsNullOrWhiteSpace(altText))
      {
        sb.Append($" [Image: {altText}]");
      }
    }

    return sb.ToString();
  }

  private static string? GetHyperlinkUrl(WordprocessingDocument document, Hyperlink hyperlink)
  {
    var rId = hyperlink.Id?.Value;
    if (string.IsNullOrEmpty(rId)) return null;

    var mainPart = document.MainDocumentPart;
    if (mainPart == null) return null;

    try
    {
      var rel = mainPart.HyperlinkRelationships.FirstOrDefault(r => r.Id == rId);
      return rel?.Uri?.ToString();
    }
    catch
    {
      return null;
    }
  }

  private static string? GetImageDescription(Drawing drawing)
  {
    // Try to get alt text from DocProperties
    var docProps = drawing.Descendants<DW.DocProperties>().FirstOrDefault();
    if (docProps != null)
    {
      var description = docProps.Description?.Value;
      var title = docProps.Title?.Value;
      var name = docProps.Name?.Value;

      return !string.IsNullOrWhiteSpace(description) ? description :
             !string.IsNullOrWhiteSpace(title) ? title :
             !string.IsNullOrWhiteSpace(name) ? name : null;
    }

    return null;
  }

  private static bool IsHeading(string styleId, out int level)
  {
    level = 0;
    if (string.IsNullOrEmpty(styleId)) return false;

    // Common heading style patterns
    if (styleId.StartsWith("Heading", StringComparison.OrdinalIgnoreCase))
    {
      var levelStr = styleId.Replace("Heading", "");
      if (int.TryParse(levelStr, out level) && level >= 1 && level <= 9)
        return true;
    }

    // Vietnamese/other language heading styles
    if (styleId.Contains("Heading") || styleId.Contains("heading") ||
        styleId.StartsWith("H", StringComparison.OrdinalIgnoreCase))
    {
      // Try to extract number
      var match = Regex.Match(styleId, @"\d+");
      if (match.Success && int.TryParse(match.Value, out level) && level >= 1 && level <= 9)
        return true;
    }

    // Title style = H1
    if (styleId.Equals("Title", StringComparison.OrdinalIgnoreCase))
    {
      level = 1;
      return true;
    }

    // Subtitle = H2
    if (styleId.Equals("Subtitle", StringComparison.OrdinalIgnoreCase))
    {
      level = 2;
      return true;
    }

    return false;
  }

  private static bool IsNumberedList(WordprocessingDocument document, int numId, int ilvl)
  {
    try
    {
      var numberingPart = document.MainDocumentPart?.NumberingDefinitionsPart;
      if (numberingPart?.Numbering == null) return false;

      var numInstance = numberingPart.Numbering.Elements<NumberingInstance>()
          .FirstOrDefault(n => n.NumberID?.Value == numId);

      if (numInstance?.AbstractNumId?.Val?.Value == null) return false;

      var abstractNum = numberingPart.Numbering.Elements<AbstractNum>()
          .FirstOrDefault(a => a.AbstractNumberId?.Value == numInstance.AbstractNumId.Val.Value);

      var level = abstractNum?.Elements<Level>()
          .FirstOrDefault(l => l.LevelIndex?.Value == ilvl);

      var numFmt = level?.NumberingFormat?.Val?.Value;

      return numFmt != NumberFormatValues.Bullet;
    }
    catch
    {
      return false;
    }
  }

  private static void ExtractTableAdvanced(
      Table table,
      StringBuilder textBuilder,
      CancellationToken cancellationToken)
  {
    textBuilder.AppendLine();

    var rows = table.Elements<TableRow>().ToList();
    if (rows.Count == 0) return;

    // Determine column count from first row
    var columnCount = rows[0].Elements<TableCell>().Count();
    var isFirstRow = true;

    foreach (var row in rows)
    {
      cancellationToken.ThrowIfCancellationRequested();

      var cellTexts = new List<string>();
      foreach (var cell in row.Elements<TableCell>())
      {
        var cellContent = new StringBuilder();
        foreach (var para in cell.Elements<Paragraph>())
        {
          if (cellContent.Length > 0)
            cellContent.Append(' ');
          cellContent.Append(para.InnerText.Trim());
        }

        // Clean cell text - remove newlines, normalize whitespace
        var cleanedText = Regex.Replace(cellContent.ToString(), @"\s+", " ").Trim();
        cellTexts.Add(cleanedText);
      }

      // Pad to consistent column count
      while (cellTexts.Count < columnCount)
        cellTexts.Add("");

      textBuilder.AppendLine($"| {string.Join(" | ", cellTexts)} |");

      // Add markdown table header separator after first row
      if (isFirstRow)
      {
        textBuilder.AppendLine($"| {string.Join(" | ", Enumerable.Repeat("---", columnCount))} |");
        isFirstRow = false;
      }
    }

    textBuilder.AppendLine();
  }

  private static void ExtractFootnotes(
      WordprocessingDocument document,
      StringBuilder textBuilder,
      CancellationToken cancellationToken)
  {
    var footnotesPart = document.MainDocumentPart?.FootnotesPart;
    if (footnotesPart?.Footnotes == null) return;

    var footnotes = footnotesPart.Footnotes
        .Elements<Footnote>()
        .Where(f => f.Type == null || f.Type == FootnoteEndnoteValues.Normal)
        .ToList();

    if (footnotes.Count == 0) return;

    textBuilder.AppendLine();
    textBuilder.AppendLine("---");
    textBuilder.AppendLine("**Footnotes:**");

    foreach (var footnote in footnotes)
    {
      cancellationToken.ThrowIfCancellationRequested();
      var id = footnote.Id?.Value;
      var text = GetAllText(footnote);
      if (!string.IsNullOrWhiteSpace(text))
      {
        textBuilder.AppendLine($"[^{id}]: {text}");
      }
    }
  }

  private static void ExtractEndnotes(
      WordprocessingDocument document,
      StringBuilder textBuilder,
      CancellationToken cancellationToken)
  {
    var endnotesPart = document.MainDocumentPart?.EndnotesPart;
    if (endnotesPart?.Endnotes == null) return;

    var endnotes = endnotesPart.Endnotes
        .Elements<Endnote>()
        .Where(e => e.Type == null || e.Type == FootnoteEndnoteValues.Normal)
        .ToList();

    if (endnotes.Count == 0) return;

    textBuilder.AppendLine();
    textBuilder.AppendLine("---");
    textBuilder.AppendLine("**Endnotes:**");

    foreach (var endnote in endnotes)
    {
      cancellationToken.ThrowIfCancellationRequested();
      var id = endnote.Id?.Value;
      var text = GetAllText(endnote);
      if (!string.IsNullOrWhiteSpace(text))
      {
        textBuilder.AppendLine($"[^e{id}]: {text}");
      }
    }
  }

  private static void ExtractComments(
      WordprocessingDocument document,
      StringBuilder textBuilder,
      CancellationToken cancellationToken)
  {
    var commentsPart = document.MainDocumentPart?.WordprocessingCommentsPart;
    if (commentsPart?.Comments == null) return;

    var comments = commentsPart.Comments.Elements<Comment>().ToList();
    if (comments.Count == 0) return;

    textBuilder.AppendLine();
    textBuilder.AppendLine("---");
    textBuilder.AppendLine("**Document Comments:**");

    foreach (var comment in comments)
    {
      cancellationToken.ThrowIfCancellationRequested();
      var author = comment.Author?.Value ?? "Unknown";
      var date = comment.Date?.Value;
      var text = GetAllText(comment);

      if (!string.IsNullOrWhiteSpace(text))
      {
        var dateStr = date.HasValue ? $" ({date.Value:yyyy-MM-dd})" : "";
        textBuilder.AppendLine($"- **{author}**{dateStr}: {text}");
      }
    }
  }

  private static string GetAllText(OpenXmlElement? element)
  {
    if (element == null) return string.Empty;
    return string.Join(" ", element.Descendants<Text>().Select(t => t.Text)).Trim();
  }

  private static string NormalizeText(string text)
  {
    if (string.IsNullOrEmpty(text)) return text;

    // Normalize line endings
    text = text.Replace("\r\n", "\n").Replace("\r", "\n");

    // Remove excessive blank lines (more than 2 consecutive)
    text = Regex.Replace(text, @"\n{3,}", "\n\n");

    // Normalize Unicode (NFC normalization)
    text = text.Normalize(NormalizationForm.FormC);

    // Remove zero-width characters that can interfere with RAG
    text = Regex.Replace(text, @"[\u200B-\u200D\uFEFF]", "");

    return text.Trim();
  }

  private static int CountWords(string text)
  {
    if (string.IsNullOrWhiteSpace(text)) return 0;
    return Regex.Matches(text, @"[\w]+").Count;
  }
}
