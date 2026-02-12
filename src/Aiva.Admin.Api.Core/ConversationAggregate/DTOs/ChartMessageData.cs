using System.Text.Json.Serialization;

namespace Aiva.Admin.Api.Core.ConversationAggregate.DTOs;

public class ChartMessageData
{
  [JsonPropertyName("textResponse")]
  public string TextResponse { get; set; } = string.Empty;
  
  [JsonPropertyName("markdownTable")]
  public string? MarkdownTable { get; set; }
  
  [JsonPropertyName("hasChart")]
  public bool HasChart { get; set; }
  
  [JsonPropertyName("chartConfig")]
  public string? ChartConfig { get; set; }
  
  [JsonPropertyName("chartType")]
  public string? ChartType { get; set; }
}
