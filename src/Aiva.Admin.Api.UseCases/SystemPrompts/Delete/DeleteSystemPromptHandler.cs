namespace Aiva.Admin.Api.UseCases.SystemPrompts.Delete;

using Ardalis.Result;
using Core.SystemPromptAggregate;

public sealed class DeleteSystemPromptHandler(IRepository<SystemPrompt> repository)
  : ICommandHandler<DeleteSystemPromptCommand, Result>
{
  public async ValueTask<Result> Handle(
    DeleteSystemPromptCommand command,
    CancellationToken cancellationToken)
  {
    var entity = await repository.GetByIdAsync(command.Id, cancellationToken);
    if (entity is null)
    {
      return Result.NotFound();
    }

    await repository.DeleteAsync(entity, cancellationToken);
    return Result.Success();
  }
}

