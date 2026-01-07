namespace Aiva.Admin.Api.Core.ConversationAggregate.Specifications;

using UserAggregate;

public sealed class ConversationByIdAndUserWithMessagesSpec : Specification<Conversation>, ISingleResultSpecification<Conversation>
{
  public ConversationByIdAndUserWithMessagesSpec(ConversationId conversationId, UserId userId)
  {
    Query
        .Where(c => c.Id == conversationId && c.UserId == userId)
        .Include(c => c.Messages);
  }
}
