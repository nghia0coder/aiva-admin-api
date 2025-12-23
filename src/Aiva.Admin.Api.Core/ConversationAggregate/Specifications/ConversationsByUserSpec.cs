namespace Aiva.Admin.Api.Core.ConversationAggregate.Specifications;

using UserAggregate;

public sealed class ConversationsByUserSpec : Specification<Conversation>
{
  public ConversationsByUserSpec(UserId userId, int skip, int take)
  {
    Query
        .Where(c => c.UserId == userId)
        .OrderByDescending(c => c.LastMessageAt ?? c.CreatedAt)
        .Skip(skip)
        .Take(take);
  }
}

public sealed class ConversationsByUserCountSpec : Specification<Conversation>
{
  public ConversationsByUserCountSpec(UserId userId)
  {
    Query.Where(c => c.UserId == userId);
  }
}
