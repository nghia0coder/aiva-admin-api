namespace Aiva.Admin.Api.Core.ConversationAggregate.DTOs;

public class ChatCompletionResult
{
  public string? TextResponse { get; set; }
  public List<ToolCall> ToolCalls { get; set; } = new();
  public bool HasToolCalls => ToolCalls.Any();
  public string? FinishReason { get; set; }
}

public class ToolCall
{
  public string Id { get; set; } = string.Empty;
  public string Type { get; set; } = "function";
  public FunctionCall Function { get; set; } = new();
}

public class FunctionCall
{
  public string Name { get; set; } = string.Empty;
  public string Arguments { get; set; } = string.Empty;
}

public class ToolDefinition
{
  public string Type { get; set; } = "function";
  public FunctionDefinition Function { get; set; } = new();
}

public class FunctionDefinition
{
  public string Name { get; set; } = string.Empty;
  public string Description { get; set; } = string.Empty;
  public object Parameters { get; set; } = new();
}
