namespace Aiva.Admin.Api.UseCases.SystemPrompts.List;

using Ardalis.Result;
using Core.SystemPromptAggregate;

public sealed class ListSystemPromptsHandler(IReadRepository<SystemPrompt> repository)
  : IQueryHandler<ListSystemPromptsQuery, Result<IReadOnlyList<SystemPromptDto>>>
{
  public async ValueTask<Result<IReadOnlyList<SystemPromptDto>>> Handle(
    ListSystemPromptsQuery request,
    CancellationToken cancellationToken)
  {
    var entities = await repository.ListAsync(cancellationToken);

    var dtos = entities
      .Select(sp => new SystemPromptDto(
        sp.Id,
        sp.Key,
        sp.Name,
        sp.Content,
        sp.Description,
        sp.Version,
        sp.IsActive))
      .ToList()
      .AsReadOnly();

    return Result.Success((IReadOnlyList<SystemPromptDto>)dtos);
  }
}

