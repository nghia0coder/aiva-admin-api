namespace Aiva.Admin.Api.Web.SystemPrompts.List;

using Aiva.Admin.Api.Web.SystemPrompts;

public sealed record ListSystemPromptsResponse(
  IReadOnlyList<SystemPromptRecord> Items);

