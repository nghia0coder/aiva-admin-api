namespace Aiva.Admin.Api.UseCases.Conversations.Stream;

public record StreamShoppingChatCommand(
    Guid ConversationId,
    string UserName,
    string Message,
    string? AdditionalUserData = null) : IRequest<Result<StreamShoppingChatResponse>>;
