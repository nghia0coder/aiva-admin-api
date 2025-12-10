namespace Aiva.Admin.Api.Core.Interfaces;

using Commons.Models;

/// <summary>
/// Service for splitting documents into chunks for embedding
/// </summary>
public interface IChunkingService
{
  /// <summary>
  /// Splits text into chunks suitable for embedding
  /// </summary>
  IReadOnlyList<TextChunk> ChunkText(
      string text,
      ChunkingOptions? options = null);
}
