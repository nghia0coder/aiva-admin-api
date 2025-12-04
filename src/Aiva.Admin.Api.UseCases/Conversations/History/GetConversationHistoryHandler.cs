namespace Aiva.Admin.Api.UseCases.Conversations.History;

using Core.ConversationAggregate;
using Core.ConversationAggregate.Specifications;

public class GetConversationHistoryHandler(IReadRepository<Conversation> repository)
    : IQueryHandler<GetConversationHistoryQuery, Result<ConversationDetailDTO>>
{
  public async ValueTask<Result<ConversationDetailDTO>> Handle(
      GetConversationHistoryQuery query,
      CancellationToken cancellationToken)
  {
    var spec = new ConversationByIdWithMessagesSpec(query.ConversationId);
    var conversation = await repository.FirstOrDefaultAsync(spec, cancellationToken);

    if (conversation is null)
    {
      return Result.NotFound("Conversation not found.");
    }

    var dto = new ConversationDetailDTO(
        conversation.Id.Value,
        conversation.Title,
        conversation.SystemPrompt,
        conversation.CreatedAt,
        conversation.Messages
            .Select(m => new ChatMessageDTO(
                m.Id.Value,
                m.Role.Name,
                m.Content,
                m.CreatedAt))
            .ToList());

    return Result.Success(dto);
  }
}
