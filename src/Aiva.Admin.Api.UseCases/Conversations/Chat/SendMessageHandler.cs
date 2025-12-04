namespace Aiva.Admin.Api.UseCases.Conversations.Chat;

using Core.ConversationAggregate;
using Core.ConversationAggregate.Specifications;
using Core.Interfaces;

public class SendMessageHandler(
    IRepository<Conversation> repository,
    IChatCompletionService chatService)
    : ICommandHandler<SendMessageCommand, Result<ChatMessageDTO>>
{
  public async ValueTask<Result<ChatMessageDTO>> Handle(
      SendMessageCommand command,
      CancellationToken cancellationToken)
  {
    var spec = new ConversationByIdWithMessagesSpec(command.ConversationId);
    var conversation = await repository.FirstOrDefaultAsync(spec, cancellationToken);

    if (conversation is null)
    {
      return Result.NotFound("Conversation not found.");
    }

    // Add user message
    conversation.AddMessage(ChatRole.User, command.UserMessage);

    // Get AI response
    var completionResult = await chatService.GetCompletionAsync(
        conversation.Messages,
        cancellationToken);

    if (!completionResult.IsSuccess)
    {
      return Result.Error(completionResult.Errors.ToString());
    }

    // Add assistant response
    var assistantMessage = conversation.AddMessage(
        ChatRole.Assistant,
        completionResult.Value);

    await repository.UpdateAsync(conversation, cancellationToken);

    return Result.Success(new ChatMessageDTO(
        assistantMessage.Id.Value,
        assistantMessage.Role.Name,
        assistantMessage.Content,
        assistantMessage.CreatedAt));
  }
}
