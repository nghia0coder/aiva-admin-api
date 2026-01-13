namespace Aiva.Admin.Api.Web.SystemPrompts.List;

using Aiva.Admin.Api.UseCases.SystemPrompts;
using Aiva.Admin.Api.Web.SystemPrompts;

public sealed class ListSystemPromptsMapper
  : Mapper<ListSystemPromptsRequest, ListSystemPromptsResponse, IReadOnlyList<SystemPromptDto>>
{
  public override ListSystemPromptsResponse FromEntity(IReadOnlyList<SystemPromptDto> e)
  {
    var records = e
      .Select(sp => new SystemPromptRecord(
        sp.Id.Value,
        sp.Key.Value,
        sp.Name,
        sp.Content,
        sp.Description,
        sp.Version,
        sp.IsActive))
      .ToList()
      .AsReadOnly();

    return new ListSystemPromptsResponse(records);
  }
}

