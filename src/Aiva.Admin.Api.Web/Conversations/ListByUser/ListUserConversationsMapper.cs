namespace Aiva.Admin.Api.Web.Conversations.ListByUser;

using UseCases;
using UseCases.Conversations;

public class ListUserConversationsMapper
    : Mapper<ListUserConversationsRequest, ListUserConversationsResponse, PagedResult<ConversationDTO>>
{
  public override ListUserConversationsResponse FromEntity(PagedResult<ConversationDTO> entity)
  {
    return new ListUserConversationsResponse(
        entity.Items.Select(c => new ConversationRecord(
            c.Id,
            c.Title,
            c.CreatedAt,
            c.LastMessageAt,
            c.MessageCount)).ToList(),
        entity.Page,
        entity.PerPage,
        entity.TotalCount,
        entity.TotalPages);
  }
}
