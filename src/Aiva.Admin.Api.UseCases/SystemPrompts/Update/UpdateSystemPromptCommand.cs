namespace Aiva.Admin.Api.UseCases.SystemPrompts.Update;

using Ardalis.Result;
using Core.SystemPromptAggregate;

public sealed record UpdateSystemPromptCommand(
  SystemPromptId Id,
  string Name,
  string Content,
  string? Description,
  bool IsActive)
  : ICommand<Result<SystemPromptDto>>;

