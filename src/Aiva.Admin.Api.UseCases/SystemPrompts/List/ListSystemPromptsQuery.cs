namespace Aiva.Admin.Api.UseCases.SystemPrompts.List;

using Ardalis.Result;

public sealed record ListSystemPromptsQuery()
  : IQuery<Result<IReadOnlyList<SystemPromptDto>>>;

