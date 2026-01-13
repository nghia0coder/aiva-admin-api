namespace Aiva.Admin.Api.UseCases.SystemPrompts.Delete;

using Ardalis.Result;
using Core.SystemPromptAggregate;

public sealed record DeleteSystemPromptCommand(SystemPromptId Id)
  : ICommand<Result>;

