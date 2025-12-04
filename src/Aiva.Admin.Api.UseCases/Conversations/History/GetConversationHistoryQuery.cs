namespace Aiva.Admin.Api.UseCases.Conversations.History;

using Core.ConversationAggregate;

public record GetConversationHistoryQuery(ConversationId ConversationId)
    : IQuery<Result<ConversationDetailDTO>>;
