using System.Text.Json;
using Aiva.Admin.Api.Core.ConversationAggregate;
using Aiva.Admin.Api.Core.ConversationAggregate.DTOs;
using Aiva.Admin.Api.Core.Interfaces;

namespace Aiva.Admin.Api.Infrastructure.Services;

public class ChatHistoryService : IChatHistoryService
{
    public Task<string> SerializeChatHistoryAsync(List<ChatMessage> messages)
    {
        var chatHistory = messages.Select(m => new ChatHistoryDto
        {
            Role = m.Role,
            Content = m.Content
        });

        return Task.FromResult(JsonSerializer.Serialize(chatHistory));
    }
}
