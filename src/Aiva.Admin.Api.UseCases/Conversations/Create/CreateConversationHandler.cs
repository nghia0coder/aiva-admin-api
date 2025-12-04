namespace Aiva.Admin.Api.UseCases.Conversations.Create;

using Core.ConversationAggregate;

public class CreateConversationHandler(IRepository<Conversation> repository)
    : ICommandHandler<CreateConversationCommand, Result<ConversationId>>
{
  public async ValueTask<Result<ConversationId>> Handle(
      CreateConversationCommand command,
      CancellationToken cancellationToken)
  {
    var conversation = new Conversation(command.Title, command.SystemPrompt);

    await repository.AddAsync(conversation, cancellationToken);

    return Result.Success(conversation.Id);
  }
}
