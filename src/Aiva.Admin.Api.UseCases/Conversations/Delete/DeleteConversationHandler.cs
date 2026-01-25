using Aiva.Admin.Api.Core.ConversationAggregate;

namespace Aiva.Admin.Api.UseCases.Conversations.Delete;

public class DeleteConversationHandler(IRepository<Conversation> repository)
    : ICommandHandler<DeleteConversationCommand, Result>
{
  public async ValueTask<Result> Handle(
      DeleteConversationCommand command,
      CancellationToken cancellationToken)
  {
    var conversation = await repository.GetByIdAsync(command.ConversationId, cancellationToken);

    if (conversation is null)
    {
      return Result.NotFound($"Conversation with ID {command.ConversationId} not found.");
    }

    conversation.Delete();
    await repository.UpdateAsync(conversation, cancellationToken);

    return Result.Success();
  }
}
