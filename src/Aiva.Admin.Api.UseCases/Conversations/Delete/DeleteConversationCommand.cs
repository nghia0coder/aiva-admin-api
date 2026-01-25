using Aiva.Admin.Api.Core.ConversationAggregate;

namespace Aiva.Admin.Api.UseCases.Conversations.Delete;

public record DeleteConversationCommand(ConversationId ConversationId) : ICommand<Result>;
