namespace Aiva.Admin.Api.UseCases.Conversations.Stream;

public record StreamShoppingChatCommand(
    Guid ConversationId,
    string Message) : IRequest<Result<StreamShoppingChatResponse>>;
