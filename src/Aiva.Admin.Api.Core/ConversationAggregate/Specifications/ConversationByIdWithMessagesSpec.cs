namespace Aiva.Admin.Api.Core.ConversationAggregate.Specifications;

public sealed class ConversationByIdWithMessagesSpec : Specification<Conversation>, ISingleResultSpecification<Conversation>
{
  public ConversationByIdWithMessagesSpec(ConversationId conversationId)
  {
    Query
        .Where(c => c.Id == conversationId)
        .Include(c => c.Messages)
        .OrderBy(c => c.CreatedAt);
  }
}
