namespace Aiva.Admin.Api.Web.SystemPrompts.Create;

public sealed class CreateSystemPromptRequest
{
  public const string Route = "/SystemPrompts";

  public string Key { get; set; } = string.Empty;
  public string Name { get; set; } = string.Empty;
  public string Content { get; set; } = string.Empty;
  public string? Category { get; set; }
  public string? Description { get; set; }
}

