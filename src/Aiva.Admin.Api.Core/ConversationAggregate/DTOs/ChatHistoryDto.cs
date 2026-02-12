using System.Text.Json.Serialization;

namespace Aiva.Admin.Api.Core.ConversationAggregate.DTOs;

public class ChatHistoryDto
{
    [JsonPropertyName("role")]
    public ChatRole? Role { get; set; }
    
    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;
}
