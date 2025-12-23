namespace Aiva.Admin.Api.UseCases.Conversations.Create;

using Core.ConversationAggregate;
using Core.UserAggregate;

public record CreateConversationCommand(
    UserId? UserId,
    string Title,
    string? SystemPrompt = null) : ICommand<Result<ConversationId>>;
