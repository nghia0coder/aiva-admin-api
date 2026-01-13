namespace Aiva.Admin.Api.Web.SystemPrompts;

public sealed record SystemPromptRecord(
  int Id,
  string Key,
  string Name,
  string Content,
  string? Description,
  int Version,
  bool IsActive);

