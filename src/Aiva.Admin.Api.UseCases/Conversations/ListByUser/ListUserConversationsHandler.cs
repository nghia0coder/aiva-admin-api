namespace Aiva.Admin.Api.UseCases.Conversations.ListByUser;

using Core.ConversationAggregate;
using Core.ConversationAggregate.Specifications;

public class ListUserConversationsHandler(IReadRepository<Conversation> repository)
    : IQueryHandler<ListUserConversationsQuery, Result<PagedResult<ConversationDTO>>>
{
  public async ValueTask<Result<PagedResult<ConversationDTO>>> Handle(
      ListUserConversationsQuery query,
      CancellationToken cancellationToken)
  {
    var page = query.Page < 1 ? 1 : query.Page;
    var perPage = Math.Clamp(query.PerPage, 1, Constants.MAX_PAGE_SIZE);
    var skip = (page - 1) * perPage;

    // Get total count
    var countSpec = new ConversationsByUserCountSpec(query.UserId);
    var totalCount = await repository.CountAsync(countSpec, cancellationToken);

    if (totalCount == 0)
    {
      return Result.Success(new PagedResult<ConversationDTO>(
          [], page, perPage, 0, 0));
    }

    // Get paginated conversations
    var spec = new ConversationsByUserSpec(query.UserId, skip, perPage);
    var conversations = await repository.ListAsync(spec, cancellationToken);

    var items = conversations
        .Select(c => new ConversationDTO(
            c.Id.Value,
            c.Title,
            c.CreatedAt,
            c.LastMessageAt,
            c.Messages.Count))
        .ToList();

    var totalPages = (int)Math.Ceiling((double)totalCount / perPage);

    return Result.Success(new PagedResult<ConversationDTO>(
        items, page, perPage, totalCount, totalPages));
  }
}
