namespace Aiva.Admin.Api.UseCases.Conversations.Chat;

using Core.ConversationAggregate;

public record SendMessageCommand(
    ConversationId ConversationId,
    string UserMessage) : ICommand<Result<ChatMessageDTO>>;
