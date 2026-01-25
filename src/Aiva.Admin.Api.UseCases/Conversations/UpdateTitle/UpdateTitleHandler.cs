using Aiva.Admin.Api.Core.ConversationAggregate;

namespace Aiva.Admin.Api.UseCases.Conversations.UpdateTitle;

public class UpdateTitleHandler(IRepository<Conversation> repository)
    : ICommandHandler<UpdateTitleCommand, Result<ConversationDTO>>
{
  public async ValueTask<Result<ConversationDTO>> Handle(
      UpdateTitleCommand command,
      CancellationToken cancellationToken)
  {
    var conversation = await repository.GetByIdAsync(command.ConversationId, cancellationToken);

    if (conversation is null)
    {
      return Result.NotFound($"Conversation with ID {command.ConversationId} not found.");
    }

    conversation.UpdateTitle(command.NewTitle);
    await repository.UpdateAsync(conversation, cancellationToken);

    var result = new ConversationDTO(conversation.Id, conversation.Title, conversation.CreatedAt, conversation.LastMessageAt, conversation.Messages.Count);
    return Result.Success(result);
  }
}
