using Aiva.Admin.Api.Core.ConversationAggregate;

namespace Aiva.Admin.Api.Core.Interfaces;

/// <summary>
/// Service for detecting user query intent in e-commerce conversations
/// </summary>
public interface IIntentDetectionService
{
  /// <summary>
  /// Detects the intent of a user query and extracts relevant entities
  /// </summary>
  /// <param name="query">The user's query text</param>
  /// <param name="conversationHistory">Previous messages for context</param>
  /// <param name="cancellationToken">Cancellation token</param>
  /// <returns>Result containing intent classification and extracted entities</returns>
  Task<Result<IntentDetectionResult>> DetectIntentAsync(
      string query,
      IReadOnlyList<ChatMessage> conversationHistory,
      CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of intent detection including classification and extracted entities
/// </summary>
public sealed record IntentDetectionResult(
    QueryIntent Intent,
    double Confidence,
    bool RequiresStructuredResponse,
    IReadOnlyDictionary<string, string>? ExtractedEntities = null)
{
  /// <summary>
  /// Brief explanation of why this intent was classified
  /// </summary>
  public string? Reasoning { get; init; }
}
