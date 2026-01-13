namespace Aiva.Admin.Api.UseCases.SystemPrompts.Get;

using Ardalis.Result;
using Core.SystemPromptAggregate;

public sealed class GetSystemPromptHandler(IReadRepository<SystemPrompt> repository)
  : IQueryHandler<GetSystemPromptQuery, Result<SystemPromptDto>>
{
  public async ValueTask<Result<SystemPromptDto>> Handle(
    GetSystemPromptQuery request,
    CancellationToken cancellationToken)
  {
    var entity = await repository.GetByIdAsync(request.Id, cancellationToken);
    if (entity is null)
    {
      return Result.NotFound();
    }

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

