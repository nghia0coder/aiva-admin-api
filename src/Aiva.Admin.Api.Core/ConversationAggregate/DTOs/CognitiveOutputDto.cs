using System.Text.Json.Serialization;

namespace Aiva.Admin.Api.Core.ConversationAggregate.DTOs;

public class CognitiveOutputDto
{
    [JsonPropertyName("answer")]
    public string? Answer { get; set; }
    
    [JsonPropertyName("chart_type")]
    public string? ChartType { get; set; }
    
    [JsonPropertyName("chart_x_axis")]
    public string? ChartXAxis { get; set; }
    
    [JsonPropertyName("chart_y_axis")]
    public string? ChartYAxis { get; set; }
    
    [JsonPropertyName("chart_series")]
    public string? ChartSeries { get; set; }
}
