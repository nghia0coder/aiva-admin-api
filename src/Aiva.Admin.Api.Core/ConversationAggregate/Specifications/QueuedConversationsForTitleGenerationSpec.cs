namespace Aiva.Admin.Api.Core.ConversationAggregate.Specifications;

public sealed class QueuedConversationsForTitleGenerationSpec : Specification<Conversation>
{
  public QueuedConversationsForTitleGenerationSpec(int batchSize = 10)
  {
    Query
        .Where(c => c.TitleStatus == TitleGenerationStatus.Queued)
        .Include(c => c.Messages)
        .OrderBy(c => c.CreatedAt)
        .Take(batchSize);
  }
}
