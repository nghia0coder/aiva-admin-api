namespace Aiva.Admin.Api.UseCases.SystemPrompts.Update;

using Ardalis.Result;
using Core.SystemPromptAggregate;

public sealed class UpdateSystemPromptHandler(IRepository<SystemPrompt> repository)
  : ICommandHandler<UpdateSystemPromptCommand, Result<SystemPromptDto>>
{
  public async ValueTask<Result<SystemPromptDto>> Handle(
    UpdateSystemPromptCommand command,
    CancellationToken cancellationToken)
  {
    var entity = await repository.GetByIdAsync(command.Id, cancellationToken);
    if (entity is null)
    {
      return Result.NotFound();
    }

    entity.UpdateMetadata(command.Name, command.Description, null);
    entity.UpdateContent(command.Content, null);

    if (command.IsActive)
    {
      entity.Activate();
    }
    else
    {
      entity.Deactivate();
    }

    await repository.UpdateAsync(entity, cancellationToken);

    var dto = new SystemPromptDto(
      entity.Id,
      entity.Key,
      entity.Name,
      entity.Content,
      entity.Description,
      entity.Version,
      entity.IsActive);

    return Result.Success(dto);
  }
}

