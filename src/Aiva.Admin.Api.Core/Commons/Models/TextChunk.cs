namespace Aiva.Admin.Api.Core.Commons.Models;

/// <summary>
/// Represents a text chunk before embedding
/// </summary>
public sealed record TextChunk
{
  /// <summary>
  /// The text content
  /// </summary>
  public required string Content { get; init; }

  /// <summary>
  /// Zero-based index of this chunk
  /// </summary>
  public required int Index { get; init; }

  /// <summary>
  /// Character offset in original text
  /// </summary>
  public required int CharacterOffset { get; init; }

  /// <summary>
  /// Length in characters
  /// </summary>
  public required int Length { get; init; }
}
