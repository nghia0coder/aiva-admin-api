using Aiva.Admin.Api.Core.Commons.Models;
using Aiva.Admin.Api.Core.ConversationAggregate;

namespace Aiva.Admin.Api.Core.Interfaces;

/// <summary>
/// Service for formatting retrieval results into structured table responses
/// </summary>
public interface IResponseFormatterService
{
  /// <summary>
  /// Formats RAG retrieval results into structured table data with action metadata
  /// </summary>
  /// <param name="intent">The detected user intent</param>
  /// <param name="retrievalResults">Results from RAG retrieval</param>
  /// <param name="userQuery">The original user query for context</param>
  /// <param name="cancellationToken">Cancellation token</param>
  /// <returns>Result containing chatbot suggestion text and structured table data</returns>
  Task<Result<StructuredResponsePackage>> FormatStructuredResponseAsync(
      QueryIntent intent,
      IReadOnlyList<VectorSearchResult> retrievalResults,
      string userQuery,
      CancellationToken cancellationToken = default);
}

/// <summary>
/// Package containing both chatbot suggestion text and structured table data
/// </summary>
public sealed record StructuredResponsePackage(
    string ChatbotSuggestionText,
    TableData TableData)
{
  /// <summary>
  /// Optional metadata about the formatting process
  /// </summary>
  public Dictionary<string, object>? Metadata { get; init; }
}
