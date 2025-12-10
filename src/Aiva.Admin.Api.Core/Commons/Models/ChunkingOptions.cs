namespace Aiva.Admin.Api.Core.Commons.Models;

/// <summary>
/// Options for text chunking
/// </summary>
public sealed record ChunkingOptions
{
  /// <summary>
  /// Maximum size of each chunk in characters
  /// </summary>
  public int MaxChunkSize { get; init; } = 1000;

  /// <summary>
  /// Number of characters to overlap between chunks
  /// </summary>
  public int ChunkOverlap { get; init; } = 200;

  /// <summary>
  /// Separators to use for splitting (in order of priority)
  /// </summary>
  public IReadOnlyList<string> Separators { get; init; } = [
      "\n\n",     // Paragraph breaks
      "\n",       // Line breaks
      ". ",       // Sentence ends
      "? ",       // Question ends
      "! ",       // Exclamation ends
      "; ",       // Semicolons
      ", ",       // Commas
      " ",        // Words
      ""          // Characters
  ];
}
