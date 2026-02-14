using System.Text.Json.Serialization;

namespace Aiva.Admin.Api.Core.ConversationAggregate.DTOs;

public class StandaloneQuestionDto
{
    [JsonPropertyName("queryString")]
    public string? QueryString { get; set; }

    [JsonPropertyName("keywords")]
    public List<string>? KeyWords { get; set; }

    [JsonPropertyName("standaloneQuestion")]
    public string? StandaloneQuestion { get; set; }

    [JsonPropertyName("summary_column")]
    public List<string>? SummaryColumn { get; set; }
}
