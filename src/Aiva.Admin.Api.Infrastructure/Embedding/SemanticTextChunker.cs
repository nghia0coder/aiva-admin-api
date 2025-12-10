using System.Text.RegularExpressions;

namespace Aiva.Admin.Api.Infrastructure.Embedding;

using Aiva.Admin.Api.Core.Commons.Models;
using Aiva.Admin.Api.Core.Interfaces;

/// <summary>
/// Semantic-aware text chunking service
/// Preserves sentence and paragraph boundaries where possible
/// </summary>
public sealed partial class SemanticTextChunker : IChunkingService
{
  private readonly ILogger<SemanticTextChunker> _logger;
  private static readonly ChunkingOptions DefaultOptions = new();

  public SemanticTextChunker(ILogger<SemanticTextChunker> logger)
  {
    _logger = logger;
  }

  public IReadOnlyList<TextChunk> ChunkText(
      string text,
      ChunkingOptions? options = null)
  {
    options ??= DefaultOptions;

    if (string.IsNullOrWhiteSpace(text))
    {
      return [];
    }

    // Normalize whitespace
    text = NormalizeWhitespace(text);

    var chunks = new List<TextChunk>();
    var currentPosition = 0;
    var chunkIndex = 0;

    while (currentPosition < text.Length)
    {
      var (chunkText, chunkEnd) = ExtractChunk(
          text,
          currentPosition,
          options.MaxChunkSize,
          options.Separators);

      if (!string.IsNullOrWhiteSpace(chunkText))
      {
        chunks.Add(new TextChunk
        {
          Content = chunkText.Trim(),
          Index = chunkIndex++,
          CharacterOffset = currentPosition,
          Length = chunkText.Length
        });
      }

      // Move position with overlap
      var nextPosition = chunkEnd - options.ChunkOverlap;
      if (nextPosition <= currentPosition)
      {
        nextPosition = chunkEnd; // Prevent infinite loop
      }
      currentPosition = nextPosition;
    }

    _logger.LogDebug(
        "Chunked text of length {Length} into {ChunkCount} chunks",
        text.Length, chunks.Count);

    return chunks;
  }

  private static (string Text, int EndPosition) ExtractChunk(
      string text,
      int startPosition,
      int maxChunkSize,
      IReadOnlyList<string> separators)
  {
    var endPosition = Math.Min(startPosition + maxChunkSize, text.Length);
    var chunkText = text[startPosition..endPosition];

    // If we're at the end, return as is
    if (endPosition >= text.Length)
    {
      return (chunkText, endPosition);
    }

    // Try to find a good break point using separators
    foreach (var separator in separators)
    {
      if (string.IsNullOrEmpty(separator))
      {
        continue;
      }

      var lastIndex = chunkText.LastIndexOf(separator, StringComparison.Ordinal);
      if (lastIndex > maxChunkSize / 4) // Don't break too early
      {
        var breakPosition = lastIndex + separator.Length;
        return (chunkText[..breakPosition], startPosition + breakPosition);
      }
    }

    // No good break point found, return full chunk
    return (chunkText, endPosition);
  }

  private static string NormalizeWhitespace(string text)
  {
    // Replace multiple whitespace with single space
    text = MultipleWhitespaceRegex().Replace(text, " ");
    // Preserve paragraph breaks
    text = text.Replace("\n ", "\n").Replace(" \n", "\n");
    return text.Trim();
  }

  [GeneratedRegex(@"[^\S\n]+")]
  private static partial Regex MultipleWhitespaceRegex();
}
