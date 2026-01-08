namespace Aiva.Admin.Api.Core.Interfaces;

using ConversationAggregate;
using UserAggregate;

public interface IRealtimeNotificationService
{
  Task NotifyTitleUpdatedAsync(
      ConversationId conversationId,
      UserId userId,
      string newTitle,
      CancellationToken cancellationToken = default);
}
