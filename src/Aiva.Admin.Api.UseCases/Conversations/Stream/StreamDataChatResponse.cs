namespace Aiva.Admin.Api.UseCases.Conversations.Stream;

public class StreamDataChatResponse
{
  public string TextResponse { get; set; } = string.Empty;
  public string? ChartUrl { get; set; }
  public string? MarkdownTable { get; set; }
  public bool HasChart { get; set; }
  public string? ChartConfig { get; set; } // Chart.js config JSON
  public string? ChartType { get; set; } // Chart type (bar, line, pie, etc.)
}
