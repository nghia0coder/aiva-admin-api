using Aiva.Admin.Api.Core.ConversationAggregate;

namespace Aiva.Admin.Api.UseCases.Conversations.Stream;

public record StreamChatCommand(
    Guid ConversationId,
    string Message) : IRequest<Result<StreamChatResponse>>;
