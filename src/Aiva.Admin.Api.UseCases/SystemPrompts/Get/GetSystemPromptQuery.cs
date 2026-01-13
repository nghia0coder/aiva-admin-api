namespace Aiva.Admin.Api.UseCases.SystemPrompts.Get;

using Ardalis.Result;
using Core.SystemPromptAggregate;

public sealed record GetSystemPromptQuery(SystemPromptId Id)
  : IQuery<Result<SystemPromptDto>>;

