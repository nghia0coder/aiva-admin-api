namespace Aiva.Admin.Api.UseCases.Conversations.Create;

using Aiva.Admin.Api.Core.ConversationAggregate;

public record CreateConversationCommand(
    string Title,
    string? SystemPrompt = null) : ICommand<Result<ConversationId>>;
