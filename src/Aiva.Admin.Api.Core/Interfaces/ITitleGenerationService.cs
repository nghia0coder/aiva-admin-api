namespace Aiva.Admin.Api.Core.Interfaces;

using ConversationAggregate;

/// <summary>
/// Service for generating conversation titles using AI
/// </summary>
public interface ITitleGenerationService
{
  /// <summary>
  /// Generates a concise, descriptive title based on conversation content
  /// </summary>
  Task<Result<string>> GenerateTitleAsync(
      IReadOnlyList<ChatMessage> messages,
      CancellationToken cancellationToken = default);
}
