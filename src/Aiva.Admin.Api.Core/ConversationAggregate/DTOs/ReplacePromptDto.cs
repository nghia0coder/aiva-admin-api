namespace Aiva.Admin.Api.Core.ConversationAggregate.DTOs;

public class ReplacePromptDto
{
    public string? FullName { get; set; }
    public string? ChatInput { get; set; }
    public string? ChatHistory { get; set; }
    public string? SqlQuery { get; set; }
    public string? ResultData { get; set; }
    public string? StandaloneQuestion { get; set; }
    public int RowCount { get; set; }
    public string? ColumnNames { get; set; }
}
