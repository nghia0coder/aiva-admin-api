using System.Text.Json;
using Aiva.Admin.Api.Core.Commons.Models;
using Aiva.Admin.Api.Core.ConversationAggregate.DTOs;
using Ardalis.GuardClauses;

namespace Aiva.Admin.Api.Core.ConversationAggregate;

public class ChatMessage : EntityBase<ChatMessage, MessageId>
{
  public ChatRole Role { get; private set; } = null!;
  public string Content { get; private set; } = string.Empty;
  public DateTime CreatedAt { get; private set; }
  public ConversationId ConversationId { get; private set; }
  public ChatResponseType ResponseType { get; private set; } = ChatResponseType.Text;
  public string? StructuredDataJson { get; private set; }

  private ChatMessage() { } // EF Core

  public ChatMessage(ChatRole role, string content, ConversationId conversationId)
  {
    Id = MessageId.New();
    Role = Guard.Against.Null(role);
    Content = Guard.Against.NullOrWhiteSpace(content);
    ConversationId = conversationId;
    CreatedAt = DateTime.UtcNow;
  }

  /// <summary>
  /// Sets the message as a structured table response
  /// </summary>
  public void SetStructuredResponse(TableData tableData)
  {
    Guard.Against.Null(tableData);
    ResponseType = ChatResponseType.StructuredTable;
    StructuredDataJson = JsonSerializer.Serialize(tableData, new JsonSerializerOptions
    {
      PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
      WriteIndented = false
    });
  }

  /// <summary>
  /// Deserializes structured data if present
  /// </summary>
  public TableData? GetStructuredData()
  {
    if (string.IsNullOrWhiteSpace(StructuredDataJson))
      return null;

    try
    {
      return JsonSerializer.Deserialize<TableData>(StructuredDataJson, new JsonSerializerOptions
      {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
      });
    }
    catch
    {
      return null;
    }
  }

  /// <summary>
  /// Sets the message as a chart response with all chart data
  /// </summary>
  public void SetChartResponse(ChartMessageData chartData)
  {
    Guard.Against.Null(chartData);
    ResponseType = ChatResponseType.Chart;
    StructuredDataJson = JsonSerializer.Serialize(chartData, new JsonSerializerOptions
    {
      PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
      WriteIndented = false
    });
  }

  /// <summary>
  /// Deserializes chart data if present
  /// </summary>
  public ChartMessageData? GetChartData()
  {
    if (string.IsNullOrWhiteSpace(StructuredDataJson) || ResponseType != ChatResponseType.Chart)
      return null;

    try
    {
      return JsonSerializer.Deserialize<ChartMessageData>(StructuredDataJson, new JsonSerializerOptions
      {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
      });
    }
    catch
    {
      return null;
    }
  }
}
