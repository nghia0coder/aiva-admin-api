using Aiva.Admin.Api.Core.ConversationAggregate;

namespace Aiva.Admin.Api.UseCases.Conversations.UpdateTitle;

public record UpdateTitleCommand(ConversationId ConversationId, string NewTitle) : ICommand<Result<ConversationDTO>>;

public record UpdateTitleResult(
  ConversationId ConversationId,
  string Title);
