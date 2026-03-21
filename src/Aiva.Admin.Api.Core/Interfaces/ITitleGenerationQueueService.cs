namespace Aiva.Admin.Api.Core.Interfaces;

using ConversationAggregate;

/// <summary>
/// Service for queuing title generation requests
/// </summary>
public interface ITitleGenerationQueueService
{
  /// <summary>
  /// Queues a conversation for title generation if it's ready
  /// </summary>
  Task QueueTitleGenerationIfReadyAsync(
      Conversation conversation,
      CancellationToken cancellationToken = default);
}
