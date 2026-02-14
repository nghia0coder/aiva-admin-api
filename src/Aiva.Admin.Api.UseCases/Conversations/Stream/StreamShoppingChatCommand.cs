namespace Aiva.Admin.Api.UseCases.Conversations.Stream;

public record StreamShoppingChatCommand(
    Guid ConversationId,
    string UserName,
    string Message) : IRequest<Result<StreamShoppingChatResponse>>;
