namespace Aiva.Admin.Api.Core.SystemPromptAggregate.Specifications;

public sealed class ActiveSystemPromptByKeySpec
  : Specification<SystemPrompt>, ISingleResultSpecification<SystemPrompt>
{
  public ActiveSystemPromptByKeySpec(SystemPromptKey key)
  {
    Query
      .Where(sp => sp.Key == key && sp.IsActive)
      .OrderByDescending(sp => sp.Version);
  }
}
