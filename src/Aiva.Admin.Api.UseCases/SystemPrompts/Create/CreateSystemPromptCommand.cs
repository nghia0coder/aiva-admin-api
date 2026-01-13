namespace Aiva.Admin.Api.UseCases.SystemPrompts.Create;

using Core.UserAggregate;
using Core.SystemPromptAggregate;

public record CreateSystemPromptCommand(
    string Key,
    string Name,
    string Content,
    UserId? UserId,
    string Category,
    string? Description = null) : ICommand<Result<SystemPromptId>>;
