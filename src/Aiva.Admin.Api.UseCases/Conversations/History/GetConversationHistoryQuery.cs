namespace Aiva.Admin.Api.UseCases.Conversations.History;

using Core.ConversationAggregate;
using Core.UserAggregate;

public record GetConversationHistoryQuery(ConversationId ConversationId, UserId UserId)
    : IQuery<Result<ConversationDetailDTO>>;
