using Aiva.Admin.Api.Core.ConversationAggregate;
using Aiva.Admin.Api.Core.Interfaces;
using Aiva.Admin.Api.UseCases.Conversations;

namespace Aiva.Admin.Api.Infrastructure.Services;

/// <summary>
/// Service Bus-based implementation for queuing title generation requests
/// </summary>
public class TitleGenerationQueueService(
    IServiceBusPublisher serviceBusPublisher) : ITitleGenerationQueueService
{
  public async Task QueueTitleGenerationIfReadyAsync(
      Conversation conversation,
      CancellationToken cancellationToken = default)
  {
    if (!conversation.IsReadyForTitleGeneration())
      return;

    // Publish message to Service Bus instead of using domain events
    var titleMessage = new TitleGenerationMessage(
        conversation.Id.Value,
        DateTime.UtcNow);

    await serviceBusPublisher.PublishAsync(
        titleMessage,
        "title-generation-queue",
        cancellationToken);
  }
}
