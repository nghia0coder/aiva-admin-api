using Aiva.Admin.Api.Core.ConversationAggregate;

namespace Aiva.Admin.Api.UseCases.Conversations.Stream;

public record StreamDataChatCommand(
    Guid ConversationId,
    string Message) : IRequest<Result<StreamDataChatResponse>>;
