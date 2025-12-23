namespace Aiva.Admin.Api.UseCases.Conversations.ListByUser;

using Core.UserAggregate;

public record ListUserConversationsQuery(
    UserId UserId,
    int Page = 1,
    int PerPage = Constants.DEFAULT_PAGE_SIZE)
    : IQuery<Result<PagedResult<ConversationDTO>>>;
