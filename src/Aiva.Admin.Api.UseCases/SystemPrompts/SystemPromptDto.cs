namespace Aiva.Admin.Api.UseCases.SystemPrompts;

using Core.SystemPromptAggregate;

public sealed record SystemPromptDto(
  SystemPromptId Id,
  SystemPromptKey Key,
  string Name,
  string Content,
  string? Description,
  int Version,
  bool IsActive);

