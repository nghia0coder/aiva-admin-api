namespace Aiva.Admin.Api.UseCases.SystemPrompts.Create;

using Core.SystemPromptAggregate;

public class CreateSystemPromptHandler(
    IRepository<SystemPrompt> repository)
    : ICommandHandler<CreateSystemPromptCommand, Result<SystemPromptId>>
{
  public async ValueTask<Result<SystemPromptId>> Handle(
      CreateSystemPromptCommand command,
      CancellationToken cancellationToken)
  {
    var key = SystemPromptKey.From(command.Key);

    var systemPrompt = SystemPrompt.Create(
        key,
        command.Name,
        command.Content,
        command.UserId,
        command.Description);

    await repository.AddAsync(systemPrompt, cancellationToken);

    return Result.Success(systemPrompt.Id);
  }
}
