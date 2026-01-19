namespace Aiva.Admin.Api.Core.ConversationAggregate.Specifications;

using UserAggregate;

/// <summary>
/// Specification to get conversation without messages for pagination scenarios
/// </summary>
public sealed class ConversationByIdAndUserSpec : Specification<Conversation>, ISingleResultSpecification<Conversation>
{
  public ConversationByIdAndUserSpec(ConversationId conversationId, UserId userId)
  {
    Query
        .Where(c => c.Id == conversationId && c.UserId == userId)
        .AsNoTracking();
  }
}
